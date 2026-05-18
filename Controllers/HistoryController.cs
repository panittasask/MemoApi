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

                // One-time backfill: ensure every existing task of this user has a SortOrder
                // so that reordering / pagination produces a stable ordering across pages.
                var hasNullOrder = await _context.Tasks
                    .AnyAsync(x => x.UserID == id && x.SortOrder == null);
                if (hasNullOrder)
                {
                    var maxOrder = await _context.Tasks
                        .Where(x => x.UserID == id && x.SortOrder != null)
                        .MaxAsync(x => (int?)x.SortOrder) ?? -1;
                    var nullTasks = await _context.Tasks
                        .Where(x => x.UserID == id && x.SortOrder == null)
                        .OrderByDescending(x => x.CreatedDate)
                        .ToListAsync();
                    var next = maxOrder + 1;
                    foreach (var t in nullTasks)
                    {
                        t.SortOrder = next++;
                    }
                    await _context.SaveChangesAsync();
                }

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
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(x => x.Status == request.Status);
                }

                query = query.OrderBy(x => x.SortOrder ?? int.MaxValue)
                             .ThenByDescending(x => x.CreatedDate);

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
                    StartTime = t.StartTime,
                    Hyperlink = t.Hyperlink,
                    TaskGroupId = t.TaskGroupId ?? t.Id,
                    SortOrder = t.SortOrder
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
                // งานที่สร้างใหม่ต้องอยู่ลำดับแรกเสมอ (SortOrder = 1) โดยเลื่อนงานเดิมของ user คนนี้ลงไป 1 ลำดับ
                await _context.Tasks
                    .Where(x => x.UserID == userId && x.SortOrder != null)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.SortOrder, x => x.SortOrder + 1));
                var sortOrder = 1;
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
                    StartTime = request.StartTime,
                    UserID = userId,
                    Hyperlink = request.Hyperlink,
                    TaskGroupId = taskGroupId,
                    SortOrder = sortOrder
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
                    StartTime = newTask.StartTime,
                    Hyperlink = newTask.Hyperlink,
                    TaskGroupId = newTask.TaskGroupId,
                    SortOrder = newTask.SortOrder
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
                task.StartTime = dto.StartTime;
                task.Hyperlink = dto.Hyperlink;
                if (dto.SortOrder.HasValue)
                {
                    task.SortOrder = dto.SortOrder;
                }
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
                    StartTime = t.StartTime,
                    Hyperlink = t.Hyperlink,
                    TaskGroupId = t.TaskGroupId ?? t.Id,
                    SortOrder = t.SortOrder
                }).ToList();

                return Ok(taskDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost("Reorder")]
        public async Task<IActionResult> ReorderTasks([FromBody] TaskReorderRequest request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                return BadRequest("No items to reorder");
            }

            try
            {
                var userId = _userService.GetMyId();
                var ids = request.Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                    .Select(i => i.Id!.Trim())
                    .Distinct()
                    .ToList();

                if (ids.Count == 0)
                {
                    return BadRequest("No valid ids");
                }

                var tasks = await _context.Tasks
                    .Where(t => t.UserID == userId && t.Id != null && ids.Contains(t.Id))
                    .ToListAsync();

                var orderMap = request.Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                    .GroupBy(i => i.Id!.Trim())
                    .ToDictionary(g => g.Key, g => g.Last().SortOrder);

                foreach (var t in tasks)
                {
                    if (t.Id != null && orderMap.TryGetValue(t.Id, out var order))
                    {
                        t.SortOrder = order;
                        t.UpdateDate = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Reordered", count = tasks.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Reorder failed: {ex.Message}");
            }
        }
    }
}
