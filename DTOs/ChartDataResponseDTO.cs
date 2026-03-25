namespace MemmoApi.DTOs
{
    public class ChartDataResponseDTO
    {
        public List<ProjectData> Projects { get; set; } = new List<ProjectData>();
        public List<StatusData> Statuses { get; set; } = new List<StatusData>();
        public List<TaskSummary> TasksSummary { get; set; } = new List<TaskSummary>();
    }

    public class ProjectData
    {
        public string? ProjectName { get; set; }
        public int TaskCount { get; set; }
        public double TotalDuration { get; set; }
    }

    public class StatusData
    {
        public string? Status { get; set; }
        public int TaskCount { get; set; }
    }

    public class TaskSummary
    {
        public string? ProjectName { get; set; }
        public string? Status { get; set; }
        public int Count { get; set; }
        public double TotalDuration { get; set; }
    }
}
