using MemmoApi.Data;
using MemmoApi.DTOs;
using MemmoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase
            // Update Workflow
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

            // Update Node
            [HttpPut("{workflowId}/nodes/{nodeId}")]
            public async Task<IActionResult> UpdateNode(int workflowId, int nodeId, [FromBody] WorkflowNodeDTO dto)
            {
                var node = await _context.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == nodeId && n.WorkflowId == workflowId);
                if (node == null) return NotFound();
                node.NodeType = dto.NodeType;
                node.TaskId = dto.TaskId;
                node.CustomName = dto.CustomName;
                await _context.SaveChangesAsync();
                return NoContent();
            }

            // Delete Node
            [HttpDelete("{workflowId}/nodes/{nodeId}")]
            public async Task<IActionResult> DeleteNode(int workflowId, int nodeId)
            {
                var node = await _context.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == nodeId && n.WorkflowId == workflowId);
                if (node == null) return NotFound();
                _context.WorkflowNodes.Remove(node);
                await _context.SaveChangesAsync();
                return NoContent();
            }

            // Delete Edge
            [HttpDelete("{workflowId}/edges/{edgeId}")]
            public async Task<IActionResult> DeleteEdge(int workflowId, int edgeId)
            {
                var edge = await _context.WorkflowEdges.FirstOrDefaultAsync(e => e.Id == edgeId && e.WorkflowId == workflowId);
                if (edge == null) return NotFound();
                _context.WorkflowEdges.Remove(edge);
                await _context.SaveChangesAsync();
                return NoContent();
            }
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
                    CustomName = n.CustomName,
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
            var node = new WorkflowNode
            {
                NodeType = dto.NodeType,
                TaskId = dto.TaskId,
                CustomName = dto.CustomName,
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkflow(int id)
        {
            var workflow = await _context.Workflows.FindAsync(id);
            if (workflow == null) return NotFound();
            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
