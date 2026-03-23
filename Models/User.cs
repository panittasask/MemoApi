using System.ComponentModel.DataAnnotations;

namespace MemmoApi.Models
{
    public class User: BaseEntity
    {
        [Key]
        public string? Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string? Name { get; set; }
        [Required]
        [MaxLength(100)]
        public string? UserName { get; set; }
        [Required]
        public string? Password { get; set; }
        [Required]
        public string? Email { get; set; }
    }
}
