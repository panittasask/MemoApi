namespace MemmoApi.Models
{
    public abstract class BaseEntity
    {
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; } 
        public DateTime UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
