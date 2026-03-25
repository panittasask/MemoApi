using MemmoApi.Data;
using MemmoApi.DTOs;
using MemmoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

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
                if (request.IsAllFilter == true)
                {
                    var query = _context.Tasks
                            .Where(x => x.UserID == id)
                            .OrderByDescending(x => x.CreatedDate);

                    int totalItems = await query.CountAsync();

                    int totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                    var tasks = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    return Ok(new PaginatedList<Models.Task>
                    {
                        Items = tasks,
                        TotalItems = totalItems,
                        TotalPages = totalPages,
                        PageIndex = request.Page
                    });
                }
                else
                {
                    var query = _context.Tasks
                        .Where(x => x.UserID == id && x.StartDate.HasValue && request.FilterDate.HasValue && x.StartDate.Value.Date == request.FilterDate.Value.Date)
                        .OrderByDescending(x => x.CreatedDate);
                    int totalItems = await query.CountAsync();

                    int totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                    var tasks = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    return Ok(new PaginatedList<Models.Task>
                    {
                        Items = tasks,
                        TotalItems = totalItems,
                        TotalPages = totalPages,
                        PageIndex = request.Page
                    });
                }


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
                var newTask = new Models.Task
                {
                    Id = id,
                    Description = request.Description,
                    Duration = request.Duration,
                    ProjectName = request.ProjectName,
                    Status = request.Status,
                    TaskName = request.TaskName,
                    StartDate = DateTime.Now,
                    UserID = userId
                };
                _context.Tasks.Add(newTask);
                await _context.SaveChangesAsync();
                var response = new TaskDTO
                {
                    Id = id,
                    Description = newTask.Description,
                    Duration = newTask.Duration,
                    ProjectName = newTask.ProjectName,
                    Status = newTask.Status,
                    TaskName = newTask.TaskName,
                    StartDate = DateTime.Now,
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
                task.TaskName = dto.TaskName;
                task.Status = dto.Status;
                task.StartDate = dto.StartDate;
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

        [HttpDelete("history")]
        public async Task<IActionResult> DeleteHistory([FromQuery] DateTime? filterDate = null)
        {
            try
            {
                var userId = _userService.GetMyId();
                var query = _context.Tasks.Where(x => x.UserID == userId);

                if (filterDate.HasValue)
                {
                    query = query.Where(x => x.StartDate.HasValue && x.StartDate.Value.Date == filterDate.Value.Date);
                }

                var tasks = await query.ToListAsync();
                if (tasks.Count == 0)
                {
                    return NotFound("No history found");
                }

                _context.Tasks.RemoveRange(tasks);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "History deleted successfully",
                    deletedCount = tasks.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"ลบ History ไม่สำเร็จ: {ex.Message}");
            }
        }
    }
}
