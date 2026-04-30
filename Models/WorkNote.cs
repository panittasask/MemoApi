using System.ComponentModel.DataAnnotations;

namespace MemmoApi.Models
{
    public class WorkNote : BaseEntity
    {
        [Key]
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}
