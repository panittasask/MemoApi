using MemmoApi.Data;
using MemmoApi.DTOs;
using MemmoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WorkflowController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkflowDTO>>> GetWorkflows()
        {
            var workflows = await _context.Workflows
                .Select(w => new WorkflowDTO
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description
                }).ToListAsync();
            return Ok(workflows);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WorkflowDetailDTO>> GetWorkflow(int id)
        {
            var workflow = await _context.Workflows
                .Include(w => w.Nodes)
                    .ThenInclude(n => n.OutgoingEdges)
                .Include(w => w.Edges)
                .FirstOrDefaultAsync(w => w.Id == id);
            if (workflow == null) return NotFound();

            var nodeIdToChildIds = workflow.Nodes.ToDictionary(
                n => n.Id,
                n => n.OutgoingEdges?.Select(e => e.ToNodeId).ToList() ?? new List<int>()
            );

            var dto = new WorkflowDetailDTO
            {
                Workflow = new WorkflowDTO
                {
                    Id = workflow.Id,
                    Name = workflow.Name,
                    Description = workflow.Description
                },
                Nodes = workflow.Nodes.Select(n => new WorkflowNodeDTO
                {
                    Id = n.Id,
                    NodeType = n.NodeType,
                    TaskId = n.TaskId,
                    CustomName = string.Equals(n.NodeType, "Custom", StringComparison.OrdinalIgnoreCase) ? n.CustomName : null,
                    ExternalTaskKey = string.Equals(n.NodeType, "Task", StringComparison.OrdinalIgnoreCase) && !n.TaskId.HasValue ? n.CustomName : null,
                    ChildNodeIds = nodeIdToChildIds.ContainsKey(n.Id) ? nodeIdToChildIds[n.Id] : new List<int>()
                }).ToList(),
                Edges = workflow.Edges.Select(e => new WorkflowEdgeDTO
                {
                    Id = e.Id,
                    FromNodeId = e.FromNodeId,
                    ToNodeId = e.ToNodeId
                }).ToList()
            };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<WorkflowDTO>> CreateWorkflow([FromBody] WorkflowDTO dto)
        {
            var workflow = new Workflow
            {
                Name = dto.Name,
                Description = dto.Description
            };
            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync();
            dto.Id = workflow.Id;
            return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, dto);
        }

        [HttpPost("{workflowId}/nodes")]
        public async Task<ActionResult<WorkflowNodeDTO>> AddNode(int workflowId, [FromBody] WorkflowNodeDTO dto)
        {
            var isTaskNode = string.Equals(dto.NodeType, "Task", StringComparison.OrdinalIgnoreCase);
            var node = new WorkflowNode
            {
                NodeType = dto.NodeType,
                TaskId = dto.TaskId,
                CustomName = isTaskNode ? (dto.ExternalTaskKey ?? dto.CustomName) : dto.CustomName,
                WorkflowId = workflowId
            };
            _context.WorkflowNodes.Add(node);
            await _context.SaveChangesAsync();
            dto.Id = node.Id;
            return Created($"api/workflow/{workflowId}/nodes/{node.Id}", dto);
        }

        [HttpPost("{workflowId}/edges")]
        public async Task<ActionResult<WorkflowEdgeDTO>> AddEdge(int workflowId, [FromBody] WorkflowEdgeDTO dto)
        {
            var edge = new WorkflowEdge
            {
                FromNodeId = dto.FromNodeId,
                ToNodeId = dto.ToNodeId,
                WorkflowId = workflowId
            };
            _context.WorkflowEdges.Add(edge);
            await _context.SaveChangesAsync();
            dto.Id = edge.Id;
            return Created($"api/workflow/{workflowId}/edges/{edge.Id}", dto);
        }

        [HttpPost("{workflowId}/sync")]
        public async Task<ActionResult<WorkflowSyncResultDTO>> SyncWorkflowGraph(int workflowId, [FromBody] WorkflowSyncRequestDTO dto)
        {
            var workflow = await _context.Workflows
                .Include(w => w.Nodes)
                .Include(w => w.Edges)
                .FirstOrDefaultAsync(w => w.Id == workflowId);

            if (workflow == null)
            {
                return NotFound(new { message = "Workflow not found" });
            }

            var result = new WorkflowSyncResultDTO();
            var clientNodeIdToDbNodeId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var existingTaskNodeMap = workflow.Nodes
                .Where(n => string.Equals(n.NodeType, "Task", StringComparison.OrdinalIgnoreCase) && n.TaskId.HasValue)
                .GroupBy(n => n.TaskId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Id);
            var existingTaskNodeByExternalIdMap = workflow.Nodes
                .Where(n =>
                    string.Equals(n.NodeType, "Task", StringComparison.OrdinalIgnoreCase) &&
                    !n.TaskId.HasValue &&
                    !string.IsNullOrWhiteSpace(n.CustomName))
                .GroupBy(n => n.CustomName!.Trim())
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            var existingCustomNodeMap = workflow.Nodes
                .Where(n =>
                    string.Equals(n.NodeType, "Custom", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(n.CustomName))
                .GroupBy(n => n.CustomName!.Trim())
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            foreach (var node in dto.Nodes ?? new List<WorkflowSyncNodeDTO>())
            {
                var clientNodeId = (node.ClientNodeId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(clientNodeId))
                {
                    result.Warnings.Add("Skipped node: clientNodeId is required.");
                    continue;
                }

                var nodeType = (node.NodeType ?? string.Empty).Trim();
                var isTaskNode = string.Equals(nodeType, "Task", StringComparison.OrdinalIgnoreCase);
                var isCustomNode = string.Equals(nodeType, "Custom", StringComparison.OrdinalIgnoreCase);

                if (!isTaskNode && !isCustomNode)
                {
                    result.Warnings.Add($"Skipped node '{clientNodeId}': unsupported node type '{nodeType}'.");
                    continue;
                }

                if (isCustomNode)
                {
                    var customName = (node.CustomName ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(customName))
                    {
                        customName = $"Custom-{clientNodeId}";
                    }

                    if (!existingCustomNodeMap.TryGetValue(customName, out var dbCustomNodeId))
                    {
                        var createdCustomNode = new WorkflowNode
                        {
                            NodeType = "Custom",
                            TaskId = null,
                            CustomName = customName,
                            WorkflowId = workflowId
                        };
                        _context.WorkflowNodes.Add(createdCustomNode);
                        await _context.SaveChangesAsync();
                        dbCustomNodeId = createdCustomNode.Id;
                        existingCustomNodeMap[customName] = dbCustomNodeId;
                        result.CreatedNodes++;
                    }

                    clientNodeIdToDbNodeId[clientNodeId] = dbCustomNodeId;
                    continue;
                }

                var externalTaskKey = (node.ExternalTaskKey ?? node.CustomName ?? string.Empty).Trim();

                if (!node.TaskId.HasValue && string.IsNullOrWhiteSpace(externalTaskKey))
                {
                    result.Warnings.Add($"Skipped node '{clientNodeId}': taskId or externalTaskKey is required for Task node.");
                    continue;
                }

                var hasNumericTaskId = node.TaskId.HasValue;
                var externalTaskId = externalTaskKey;

                var dbNodeId = 0;
                var foundExistingNode = false;
                if (hasNumericTaskId)
                {
                    foundExistingNode = existingTaskNodeMap.TryGetValue(node.TaskId!.Value, out dbNodeId);
                }
                else
                {
                    foundExistingNode = existingTaskNodeByExternalIdMap.TryGetValue(externalTaskId, out dbNodeId);
                }

                if (!foundExistingNode)
                {
                    var createdNode = new WorkflowNode
                    {
                        NodeType = "Task",
                        TaskId = hasNumericTaskId ? node.TaskId : null,
                        CustomName = hasNumericTaskId ? null : externalTaskId,
                        WorkflowId = workflowId
                    };
                    _context.WorkflowNodes.Add(createdNode);
                    await _context.SaveChangesAsync();
                    dbNodeId = createdNode.Id;
                    if (hasNumericTaskId)
                    {
                        existingTaskNodeMap[node.TaskId!.Value] = dbNodeId;
                    }
                    else
                    {
                        existingTaskNodeByExternalIdMap[externalTaskId] = dbNodeId;
                    }
                    result.CreatedNodes++;
                }

                clientNodeIdToDbNodeId[clientNodeId] = dbNodeId;
            }

            var requestedEdgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var edge in dto.Edges ?? new List<WorkflowSyncEdgeDTO>())
            {
                var fromClientNodeId = (edge.FromClientNodeId ?? string.Empty).Trim();
                var toClientNodeId = (edge.ToClientNodeId ?? string.Empty).Trim();

                if (!clientNodeIdToDbNodeId.TryGetValue(fromClientNodeId, out var fromDbNodeId) ||
                    !clientNodeIdToDbNodeId.TryGetValue(toClientNodeId, out var toDbNodeId))
                {
                    result.SkippedEdges++;
                    continue;
                }

                if (fromDbNodeId == toDbNodeId)
                {
                    result.SkippedEdges++;
                    result.Warnings.Add($"Skipped self-loop edge: {fromClientNodeId} -> {toClientNodeId}");
                    continue;
                }

                requestedEdgeKeys.Add($"{fromDbNodeId}-{toDbNodeId}");
            }

            if (dto.ReplaceExistingEdges)
            {
                var edgesToDelete = workflow.Edges
                    .Where(e => !requestedEdgeKeys.Contains($"{e.FromNodeId}-{e.ToNodeId}"))
                    .ToList();

                if (edgesToDelete.Count > 0)
                {
                    _context.WorkflowEdges.RemoveRange(edgesToDelete);
                    result.DeletedEdges = edgesToDelete.Count;
                }
            }

            var existingEdgeKeys = workflow.Edges
                .Select(e => $"{e.FromNodeId}-{e.ToNodeId}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var edgeKey in requestedEdgeKeys)
            {
                if (existingEdgeKeys.Contains(edgeKey))
                {
                    continue;
                }

                var parts = edgeKey.Split('-');
                var edge = new WorkflowEdge
                {
                    WorkflowId = workflowId,
                    FromNodeId = int.Parse(parts[0]),
                    ToNodeId = int.Parse(parts[1])
                };
                _context.WorkflowEdges.Add(edge);
                result.CreatedEdges++;
            }

            await _context.SaveChangesAsync();

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkflow(int id, [FromBody] WorkflowDTO dto)
        {
            var workflow = await _context.Workflows.FindAsync(id);
            if (workflow == null) return NotFound();
            workflow.Name = dto.Name;
            workflow.Description = dto.Description;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{workflowId}/nodes/{nodeId}")]
        public async Task<IActionResult> UpdateNode(int workflowId, int nodeId, [FromBody] WorkflowNodeDTO dto)
        {
            var node = await _context.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == nodeId && n.WorkflowId == workflowId);
            if (node == null) return NotFound();
            var isTaskNode = string.Equals(dto.NodeType, "Task", StringComparison.OrdinalIgnoreCase);
            node.NodeType = dto.NodeType;
            node.TaskId = dto.TaskId;
            node.CustomName = isTaskNode ? (dto.ExternalTaskKey ?? dto.CustomName) : dto.CustomName;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkflow(int id)
        {
            var workflow = await _context.Workflows.FindAsync(id);
            if (workflow == null) return NotFound();
            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{workflowId}/nodes/{nodeId}")]
        public async Task<IActionResult> DeleteNode(int workflowId, int nodeId)
        {
            var node = await _context.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == nodeId && n.WorkflowId == workflowId);
            if (node == null) return NotFound();
            _context.WorkflowNodes.Remove(node);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{workflowId}/edges/{edgeId}")]
        public async Task<IActionResult> DeleteEdge(int workflowId, int edgeId)
        {
            var edge = await _context.WorkflowEdges.FirstOrDefaultAsync(e => e.Id == edgeId && e.WorkflowId == workflowId);
            if (edge == null) return NotFound();
            _context.WorkflowEdges.Remove(edge);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
