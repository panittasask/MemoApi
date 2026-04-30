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
    public class WorkNoteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public WorkNoteController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = _userService.GetMyId();
                var notes = await _context.WorkNotes
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.UpdateDate)
                    .Select(x => new WorkNoteDTO
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Detail = x.Detail,
                        CreatedDate = x.CreatedDate,
                        UpdateDate = x.UpdateDate,
                    })
                    .ToListAsync();
                return Ok(notes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var userId = _userService.GetMyId();
                var note = await _context.WorkNotes
                    .Where(x => x.UserId == userId && x.Id == id)
                    .Select(x => new WorkNoteDTO
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Detail = x.Detail,
                        CreatedDate = x.CreatedDate,
                        UpdateDate = x.UpdateDate,
                    })
                    .FirstOrDefaultAsync();
                if (note == null)
                {
                    return NotFound();
                }
                return Ok(note);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(WorkNoteDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required");
            }
            try
            {
                var userId = _userService.GetMyId();
                var note = new WorkNote
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Title = request.Title,
                    Detail = request.Detail,
                };
                _context.WorkNotes.Add(note);
                await _context.SaveChangesAsync();
                request.Id = note.Id;
                request.CreatedDate = note.CreatedDate;
                request.UpdateDate = note.UpdateDate;
                return Ok(request);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, WorkNoteDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required");
            }
            try
            {
                var userId = _userService.GetMyId();
                var note = await _context.WorkNotes
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id);
                if (note == null)
                {
                    return NotFound();
                }
                note.Title = request.Title;
                note.Detail = request.Detail;
                await _context.SaveChangesAsync();
                request.Id = note.Id;
                request.CreatedDate = note.CreatedDate;
                request.UpdateDate = note.UpdateDate;
                return Ok(request);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var userId = _userService.GetMyId();
                var note = await _context.WorkNotes
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id);
                if (note == null)
                {
                    return NotFound();
                }
                _context.WorkNotes.Remove(note);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
