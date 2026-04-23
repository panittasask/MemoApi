using System.Collections.Generic;

namespace MemmoApi.Models
{
    public class Workflow : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<WorkflowNode> Nodes { get; set; }
        public ICollection<WorkflowEdge> Edges { get; set; }
    }

    public class WorkflowNode : BaseEntity
    {
        public int Id { get; set; }
        public string NodeType { get; set; } // "Task" or "Custom"
        public int? TaskId { get; set; } // ถ้าเป็น Task จะมีค่า ถ้าเป็น Custom จะเป็น null
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string CustomName { get; set; } // ถ้าเป็น Custom จะมีค่า
        public int WorkflowId { get; set; }
        public Workflow Workflow { get; set; }

        // Navigation property สำหรับ edge ที่ node นี้เป็น parent (child node ทั้งหมด)
        public ICollection<WorkflowEdge> OutgoingEdges { get; set; }
        // Navigation property สำหรับ edge ที่ node นี้เป็น child (parent node ทั้งหมด)
        public ICollection<WorkflowEdge> IncomingEdges { get; set; }
    }

    public class WorkflowEdge : BaseEntity
    {
        public int Id { get; set; }
        public int FromNodeId { get; set; }
        public WorkflowNode FromNode { get; set; }
        public int ToNodeId { get; set; }
        public WorkflowNode ToNode { get; set; }
        public int WorkflowId { get; set; }
        public Workflow Workflow { get; set; }
    }
}
