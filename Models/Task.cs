using System.ComponentModel.DataAnnotations;

namespace MemmoApi.Models
{
    public class Task:BaseEntity
    {
        [Key]
        public string? Id { get; set; }
        public string? UserID { get; set; }
        public Double? Duration { get; set; }
        public string? NameType { get; set; }
        public string? ProjectName { get; set; }
        public string? TaskName { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public string? Hyperlink { get; set; }
    }
}
