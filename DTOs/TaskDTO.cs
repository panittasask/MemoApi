namespace MemmoApi.DTOs
{
    public class TaskDTO
    {
        public string? Id { get; set; }
        public Double? Duration { get; set; }
        public string? ProjectName { get; set; }
        public string? TaskName { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
    }
}
