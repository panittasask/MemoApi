using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MemmoApi.DTOs
{
    public class TaskDTO
    {
        public string? Id { get; set; }
        public Double? Duration { get; set; }
        public string? NameType { get; set; }
        public string? ProjectName { get; set; }
        public string? TaskName { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
    }
    public class TaskRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateTime? FilterDate { get; set; } = DateTime.Now;
        public bool IsAllFilter { get; set; } = false;
        public string? NameType { get; set; }
    }
}
