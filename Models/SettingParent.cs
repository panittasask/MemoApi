using System.ComponentModel.DataAnnotations;

namespace MemmoApi.Models
{
    public class SettingParent : BaseEntity
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public ICollection<SettingChild> Children { get; set; } = new List<SettingChild>();
    }
}