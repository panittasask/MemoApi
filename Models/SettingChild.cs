using System.ComponentModel.DataAnnotations;

namespace MemmoApi.Models
{
    public class SettingChild : BaseEntity
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;


        public SettingParent? Parent { get; set; }
    }
}