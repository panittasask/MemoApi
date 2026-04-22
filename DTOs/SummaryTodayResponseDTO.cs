namespace MemmoApi.DTOs
{
    public class SummaryTodayResponseDTO
    {
        public DateTime Date { get; set; }
        public int TotalTasks { get; set; }
        public double TotalHours { get; set; }
        public List<TodayTaskItemDTO> Tasks { get; set; } = new List<TodayTaskItemDTO>();
    }

    public class TodayTaskItemDTO
    {
        public string? TaskId { get; set; }
        public string? ProjectName { get; set; }
        public string? TaskName { get; set; }
        public string? Status { get; set; }
        public double Duration { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Hyperlink { get; set; }
    }
}