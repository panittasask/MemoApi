using MemmoApi.Data;
using MemmoApi.DTOs;
using MemmoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        public HistoryController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }
        [HttpPost]
        public async Task<IActionResult> GetAllTask(TaskRequest request)
        {
            try
            {
                var id = _userService.GetMyId();

                var query = _context.Tasks
                    .Where(x => x.UserID == id);

                //แยก normal task กับ focus task ให้ชัด
                if (string.IsNullOrWhiteSpace(request.NameType))
                {
                    //query = query.Where(x => x.NameType == null || x.NameType == "");
                }
                else
                {
                    query = query.Where(x => x.NameType == request.NameType);
                }

                if (request.IsAllFilter != true)
                {
                    query = query.Where(x =>
                        x.StartDate.HasValue &&
                        request.FilterDate.HasValue &&
                        x.StartDate.Value.Date == request.FilterDate.Value.Date);
                }
                if(!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(x => x.Status == request.Status);
                }

                query = query.OrderByDescending(x => x.CreatedDate);

                int totalItems = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                var tasks = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Map Tasks to TaskDTO to include hyperlink field
                var taskDTOs = tasks.Select(t => new TaskDTO
                {
                    Id = t.Id,
                    Duration = t.Duration,
                    NameType = t.NameType,
                    ProjectName = t.ProjectName,
                    TaskName = t.TaskName,
                    Description = t.Description,
                    Status = t.Status,
                    StartDate = t.StartDate,
                    Hyperlink = t.Hyperlink,
                    TaskGroupId = t.TaskGroupId ?? t.Id
                }).ToList();

                return Ok(new PaginatedList<TaskDTO>
                {
                    Items = taskDTOs,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    PageIndex = request.Page
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost]
        [Route("AddNew")]
        public async Task<IActionResult> CreateTask(TaskDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var id = Guid.NewGuid().ToString();
                var userId = _userService.GetMyId();
                // ถ้าเป็นการ clone จากงานเดิม ให้ใช้ TaskGroupId เดิม; ถ้าเป็นงานใหม่ ใช้ id ของตัวเองเป็น TaskGroupId
                var taskGroupId = string.IsNullOrWhiteSpace(request.TaskGroupId) ? id : request.TaskGroupId!.Trim();
                var newTask = new Models.Task
                {
                    Id = id,
                    Description = request.Description,
                    Duration = request.Duration,
                    NameType = request.NameType,
                    ProjectName = request.ProjectName,
                    Status = request.Status,
                    TaskName = request.TaskName,
                    StartDate = DateTime.Now,
                    UserID = userId,
                    Hyperlink = request.Hyperlink,
                    TaskGroupId = taskGroupId
                };
                _context.Tasks.Add(newTask);
                await _context.SaveChangesAsync();
                var response = new TaskDTO
                {
                    Id = id,
                    Description = newTask.Description,
                    Duration = newTask.Duration,
                    NameType = newTask.NameType,
                    ProjectName = newTask.ProjectName,
                    Status = newTask.Status,
                    TaskName = newTask.TaskName,
                    StartDate = DateTime.Now,
                    Hyperlink = newTask.Hyperlink,
                    TaskGroupId = newTask.TaskGroupId
                };
                return Ok(response);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost("Update")]
        public async Task<IActionResult> UpdateTask(TaskDTO dto)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(dto.Id);

                if (task == null)
                {
                    return NotFound($"ไม่พบ Task ID: {dto.Id}");
                }
                task.ProjectName = dto.ProjectName;
                task.Description = dto.Description;
                task.Duration = dto.Duration;
                task.NameType = dto.NameType;
                task.TaskName = dto.TaskName;
                task.Status = dto.Status;
                task.StartDate = dto.StartDate;
                task.Hyperlink = dto.Hyperlink;
                task.UpdateDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"อัปเดตไม่สำเร็จ: {ex.Message}");
            }
        }

        [HttpDelete("task/{id}")]
        public async Task<IActionResult> DeleteTask(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Task id is required");
            }

            try
            {
                var userId = _userService.GetMyId();
                var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserID == userId);

                if (task == null)
                {
                    return NotFound("Task not found");
                }

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Task deleted successfully",
                    id = task.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"ลบ Task ไม่สำเร็จ: {ex.Message}");
            }
        }

        [HttpPost("ByIds")]
        public async Task<IActionResult> GetTasksByIds([FromBody] TaskIdsRequest request)
        {
            try
            {
                var userId = _userService.GetMyId();
                var normalizedIds = (request.TaskIds ?? new List<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (normalizedIds.Count == 0)
                {
                    return Ok(new List<TaskDTO>());
                }

                var tasks = await _context.Tasks
                    .Where(t => t.UserID == userId && t.Id != null && normalizedIds.Contains(t.Id))
                    .ToListAsync();

                var taskDTOs = tasks.Select(t => new TaskDTO
                {
                    Id = t.Id,
                    Duration = t.Duration,
                    NameType = t.NameType,
                    ProjectName = t.ProjectName,
                    TaskName = t.TaskName,
                    Description = t.Description,
                    Status = t.Status,
                    StartDate = t.StartDate,
                    Hyperlink = t.Hyperlink,
                    TaskGroupId = t.TaskGroupId ?? t.Id
                }).ToList();

                return Ok(taskDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
