namespace MemmoApi.DTOs
{
    public class WorkNoteDTO
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
