using MemmoApi.Data;
using MemmoApi.DTOs;
using MemmoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllTask()
        {
            try
            {
                var task = await _context.Tasks.OrderByDescending(x => x.CreatedDate).ToListAsync();
                return Ok(task);
            }catch(Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost]
        [Route("AddNew")]
        public async Task<IActionResult>CreateTask(TaskDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var id = Guid.NewGuid().ToString();
                var newTask = new Models.Task
                {
                    Id = id,
                    Description = request.Description,
                    Duration = request.Duration,
                    ProjectName = request.ProjectName,
                    Status = request.Status,
                    TaskName = request.TaskName,
                    StartDate = DateTime.Now,
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
                task.UpdateDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"อัปเดตไม่สำเร็จ: {ex.Message}");
            }
        }
    }
}
