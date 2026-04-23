namespace MemmoApi.DTOs
{
    public class WorkflowDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class WorkflowNodeDTO
    {
        public int Id { get; set; }
        public string NodeType { get; set; } // "Task" or "Custom"
        public int? TaskId { get; set; }
        public string CustomName { get; set; }
        public string ExternalTaskKey { get; set; }
        public List<int> ChildNodeIds { get; set; } // สำหรับ response: node ลูกทั้งหมด
    }

    public class WorkflowEdgeDTO
    {
        public int Id { get; set; }
        public int FromNodeId { get; set; }
        public int ToNodeId { get; set; }
    }

    public class WorkflowDetailDTO
    {
        public WorkflowDTO Workflow { get; set; }
        public List<WorkflowNodeDTO> Nodes { get; set; }
        public List<WorkflowEdgeDTO> Edges { get; set; }
    }

    public class WorkflowSyncNodeDTO
    {
        public string ClientNodeId { get; set; }
        public string NodeType { get; set; } // "Task" or "Custom"
        public int? TaskId { get; set; }
        public string CustomName { get; set; }
        public string ExternalTaskKey { get; set; }
    }

    public class WorkflowSyncEdgeDTO
    {
        public string FromClientNodeId { get; set; }
        public string ToClientNodeId { get; set; }
    }

    public class WorkflowSyncRequestDTO
    {
        public List<WorkflowSyncNodeDTO> Nodes { get; set; } = new();
        public List<WorkflowSyncEdgeDTO> Edges { get; set; } = new();
        public bool ReplaceExistingEdges { get; set; } = true;
    }

    public class WorkflowSyncResultDTO
    {
        public int CreatedNodes { get; set; }
        public int CreatedEdges { get; set; }
        public int DeletedEdges { get; set; }
        public int SkippedEdges { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
