using MemmoApi.Data;
using MemmoApi.DTOs;
using MemmoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MemmoApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private sealed class CustomNodePayload
        {
            public string? Title { get; set; }
            public string? Note { get; set; }
        }

        public WorkflowController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string BuildCustomNodePayload(string? title, string? note)
        {
            var normalizedTitle = (title ?? string.Empty).Trim();
            var normalizedNote = (note ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedTitle))
            {
                normalizedTitle = "Custom Box";
            }

            if (string.IsNullOrWhiteSpace(normalizedNote))
            {
                return normalizedTitle;
            }

            return JsonSerializer.Serialize(new CustomNodePayload
            {
                Title = normalizedTitle,
                Note = normalizedNote
            });
        }

        private static (string Title, string Note) ParseCustomNodePayload(string? rawValue)
        {
            var raw = (rawValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ("Custom Box", string.Empty);
            }

            if (!raw.StartsWith("{", StringComparison.Ordinal))
            {
                return (raw, string.Empty);
            }

            try
            {
                var payload = JsonSerializer.Deserialize<CustomNodePayload>(raw);
                var title = (payload?.Title ?? string.Empty).Trim();
                var note = (payload?.Note ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(note))
                {
                    return (raw, string.Empty);
                }

                return (string.IsNullOrWhiteSpace(title) ? "Custom Box" : title, note);
            }
            catch
            {
                return (raw, string.Empty);
            }
        }

        private static int? ParseClientNodeIdAsDbId(string clientNodeId, string prefix)
        {
            if (string.IsNullOrWhiteSpace(clientNodeId))
            {
                return null;
            }

            if (!clientNodeId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var suffix = clientNodeId.Substring(prefix.Length).Trim();
            return int.TryParse(suffix, out var dbNodeId) ? dbNodeId : null;
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

            return Ok(BuildWorkflowDetailDto(workflow));
        }

        [HttpGet("by-task/{taskId}")]
        public async Task<ActionResult<WorkflowDetailDTO>> GetWorkflowByTask(string taskId)
        {
            var normalizedTaskId = (taskId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedTaskId))
            {
                return Ok(new
                {
                    found = false,
                    message = "taskId is required",
                    workflow = (WorkflowDTO?)null,
                    nodes = new List<WorkflowNodeDTO>(),
                    edges = new List<WorkflowEdgeDTO>()
                });
            }

            var parsedNumericTaskId = int.TryParse(normalizedTaskId, out var numericTaskId)
                ? numericTaskId
                : (int?)null;

            // หา TaskGroupId ของ task นี้ และเก็บทุก task ID ที่อยู่ในกลุ่มเดียวกัน
            // เพื่อให้ task ที่ถูก clone จากวันอื่น เห็น workflow เดียวกัน
            var groupTaskIds = new List<string> { normalizedTaskId };
            var sourceTask = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == normalizedTaskId);
            string? groupKey = sourceTask?.TaskGroupId ?? sourceTask?.Id;

            if (!string.IsNullOrWhiteSpace(groupKey))
            {
                var siblingIds = await _context.Tasks
                    .Where(t => (t.TaskGroupId == groupKey) || (t.Id == groupKey))
                    .Select(t => t.Id!)
                    .ToListAsync();

                groupTaskIds = siblingIds
                    .Concat(new[] { normalizedTaskId, groupKey })
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var workflow = await _context.Workflows
                .Include(w => w.Nodes)
                    .ThenInclude(n => n.OutgoingEdges)
                .Include(w => w.Edges)
                .Where(w => w.Nodes.Any(n =>
                    n.NodeType == "Task" &&
                    (
                        (parsedNumericTaskId.HasValue && n.TaskId == parsedNumericTaskId.Value) ||
                        (!n.TaskId.HasValue && n.CustomName != null && groupTaskIds.Contains(n.CustomName))
                    )))
                .OrderByDescending(w => w.UpdateDate)
                .FirstOrDefaultAsync();

            if (workflow == null)
            {
                var mockNode = new WorkflowNodeDTO
                {
                    Id = 0,
                    NodeType = "Task",
                    TaskId = parsedNumericTaskId,
                    PositionX = 80,
                    PositionY = 60,
                    CustomName = null,
                    CustomNote = null,
                    ExternalTaskKey = parsedNumericTaskId.HasValue ? null : normalizedTaskId,
                    ChildNodeIds = new List<int>()
                };

                return Ok(new
                {
                    found = false,
                    isMock = true,
                    message = "Workflow for task not found, returned mock node",
                    workflow = (WorkflowDTO?)null,
                    nodes = new List<WorkflowNodeDTO> { mockNode },
                    edges = new List<WorkflowEdgeDTO>()
                });
            }

            var dto = BuildWorkflowDetailDto(workflow);
            return Ok(new
            {
                found = true,
                isMock = false,
                message = "Workflow found",
                workflow = dto.Workflow,
                nodes = dto.Nodes,
                edges = dto.Edges
            });
        }

        private WorkflowDetailDTO BuildWorkflowDetailDto(Workflow workflow)
        {

            var nodeIdToChildIds = workflow.Nodes.ToDictionary(
                n => n.Id,
                n => n.OutgoingEdges?.Select(e => e.ToNodeId).ToList() ?? new List<int>()
            );

            return new WorkflowDetailDTO
            {
                Workflow = new WorkflowDTO
                {
                    Id = workflow.Id,
                    Name = workflow.Name,
                    Description = workflow.Description
                },
                Nodes = workflow.Nodes.Select(n =>
                {
                    var isCustomNode = string.Equals(n.NodeType, "Custom", StringComparison.OrdinalIgnoreCase);
                    var customPayload = ParseCustomNodePayload(n.CustomName);

                    return new WorkflowNodeDTO
                    {
                        Id = n.Id,
                        NodeType = n.NodeType,
                        TaskId = n.TaskId,
                        PositionX = n.PositionX,
                        PositionY = n.PositionY,
                        CustomName = isCustomNode ? customPayload.Title : null,
                        CustomNote = isCustomNode ? customPayload.Note : null,
                        ExternalTaskKey = string.Equals(n.NodeType, "Task", StringComparison.OrdinalIgnoreCase) && !n.TaskId.HasValue ? n.CustomName : null,
                        ChildNodeIds = nodeIdToChildIds.ContainsKey(n.Id) ? nodeIdToChildIds[n.Id] : new List<int>()
                    };
                }).ToList(),
                Edges = workflow.Edges.Select(e => new WorkflowEdgeDTO
                {
                    Id = e.Id,
                    FromNodeId = e.FromNodeId,
                    ToNodeId = e.ToNodeId
                }).ToList()
            };
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
                PositionX = dto.PositionX,
                PositionY = dto.PositionY,
                CustomName = isTaskNode
                    ? (dto.ExternalTaskKey ?? dto.CustomName)
                    : BuildCustomNodePayload(dto.CustomName, dto.CustomNote),
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
            var workingEdges = workflow.Edges.ToList();
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
                    var customTitle = (node.CustomName ?? string.Empty).Trim();
                    var customNote = (node.CustomNote ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(customTitle))
                    {
                        customTitle = $"Custom-{clientNodeId}";
                    }

                    var customPayload = BuildCustomNodePayload(customTitle, customNote);
                    var linkedDbNodeId = ParseClientNodeIdAsDbId(clientNodeId, "custom-");
                    if (linkedDbNodeId.HasValue)
                    {
                        var linkedNode = workflow.Nodes.FirstOrDefault(n =>
                            n.Id == linkedDbNodeId.Value &&
                            string.Equals(n.NodeType, "Custom", StringComparison.OrdinalIgnoreCase));
                        if (linkedNode != null)
                        {
                            linkedNode.CustomName = customPayload;
                            linkedNode.PositionX = node.PositionX;
                            linkedNode.PositionY = node.PositionY;
                            clientNodeIdToDbNodeId[clientNodeId] = linkedNode.Id;
                            existingCustomNodeMap[customPayload] = linkedNode.Id;
                            continue;
                        }
                    }

                    if (!existingCustomNodeMap.TryGetValue(customPayload, out var dbCustomNodeId))
                    {
                        var createdCustomNode = new WorkflowNode
                        {
                            NodeType = "Custom",
                            TaskId = null,
                            PositionX = node.PositionX,
                            PositionY = node.PositionY,
                            CustomName = customPayload,
                            WorkflowId = workflowId
                        };
                        _context.WorkflowNodes.Add(createdCustomNode);
                        await _context.SaveChangesAsync();
                        dbCustomNodeId = createdCustomNode.Id;
                        existingCustomNodeMap[customPayload] = dbCustomNodeId;
                        result.CreatedNodes++;
                    }

                    var existingCustomNode = workflow.Nodes.FirstOrDefault(n => n.Id == dbCustomNodeId);
                    if (existingCustomNode != null)
                    {
                        existingCustomNode.PositionX = node.PositionX;
                        existingCustomNode.PositionY = node.PositionY;
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
                        PositionX = node.PositionX,
                        PositionY = node.PositionY,
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

                var existingNode = workflow.Nodes.FirstOrDefault(n => n.Id == dbNodeId);
                if (existingNode != null)
                {
                    existingNode.PositionX = node.PositionX;
                    existingNode.PositionY = node.PositionY;
                }

                clientNodeIdToDbNodeId[clientNodeId] = dbNodeId;
            }

            var requestedNodeIds = clientNodeIdToDbNodeId.Values.ToHashSet();
            var nodesToDelete = workflow.Nodes
                .Where(n => !requestedNodeIds.Contains(n.Id))
                .ToList();

            if (nodesToDelete.Count > 0)
            {
                var deletedNodeIds = nodesToDelete.Select(n => n.Id).ToHashSet();
                var edgesLinkedToDeletedNodes = workingEdges
                    .Where(e => deletedNodeIds.Contains(e.FromNodeId) || deletedNodeIds.Contains(e.ToNodeId))
                    .ToList();

                if (edgesLinkedToDeletedNodes.Count > 0)
                {
                    _context.WorkflowEdges.RemoveRange(edgesLinkedToDeletedNodes);
                    result.DeletedEdges += edgesLinkedToDeletedNodes.Count;
                    workingEdges = workingEdges
                        .Where(e => !deletedNodeIds.Contains(e.FromNodeId) && !deletedNodeIds.Contains(e.ToNodeId))
                        .ToList();
                }

                _context.WorkflowNodes.RemoveRange(nodesToDelete);
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
                var edgesToDelete = workingEdges
                    .Where(e => !requestedEdgeKeys.Contains($"{e.FromNodeId}-{e.ToNodeId}"))
                    .ToList();

                if (edgesToDelete.Count > 0)
                {
                    _context.WorkflowEdges.RemoveRange(edgesToDelete);
                    result.DeletedEdges += edgesToDelete.Count;
                    var removedEdgeIds = edgesToDelete.Select(e => e.Id).ToHashSet();
                    workingEdges = workingEdges
                        .Where(e => !removedEdgeIds.Contains(e.Id))
                        .ToList();
                }
            }

            var existingEdgeKeys = workingEdges
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
            node.PositionX = dto.PositionX;
            node.PositionY = dto.PositionY;
            node.CustomName = isTaskNode
                ? (dto.ExternalTaskKey ?? dto.CustomName)
                : BuildCustomNodePayload(dto.CustomName, dto.CustomNote);
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
