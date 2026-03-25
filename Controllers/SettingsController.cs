using MemmoApi.DTOs;
using MemmoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemmoApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var response = new SettingsResponse
            {
                Parents = await _context.SettingParents
                    .OrderBy(x => x.Name)
                    .Select(x => new DropdownParentItem
                    {
                        Id = x.Id,
                        Key = x.Key,
                        Name = x.Name
                    })
                    .ToListAsync(),
                Children = await _context.SettingChildren
                    .OrderBy(x => x.Name)
                    .Select(x => new DropdownChildItem
                    {
                        Id = x.Id,
                        ParentId = x.ParentId,
                        Key = x.Key,
                        Name = x.Name
                    })
                    .ToListAsync()
            };

            return Ok(response);
        }

        [HttpPost]
        [Route("settings/parent")]
        public async Task<IActionResult> CreateParentSetting(CreateParentSettingRequest request)
        {
            var entity = new SettingParent
            {
                Id = Guid.NewGuid().ToString("N"),
                Key = request.Key?.Trim().ToLowerInvariant() ?? string.Empty,
                Name = request.Name?.Trim() ?? string.Empty
            };

            _context.SettingParents.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new DropdownParentItem
            {
                Id = entity.Id,
                Key = entity.Key,
                Name = entity.Name
            });
        }

        [HttpPost]
        [Route("settings/child")]
        public async Task<IActionResult> CreateChildSetting(CreateChildSettingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ParentId))
            {
                return BadRequest("Parent id is required");
            }

            var parentExists = await _context.SettingParents.AnyAsync(x => x.Id == request.ParentId);
            if (!parentExists)
            {
                return BadRequest("Parent id not found");
            }

            var entity = new SettingChild
            {
                Id = Guid.NewGuid().ToString("N"),
                ParentId = request.ParentId,
                Key = request.Key?.Trim().ToLowerInvariant() ?? string.Empty,
                Name = request.Name?.Trim() ?? string.Empty
            };

            _context.SettingChildren.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new DropdownChildItem
            {
                Id = entity.Id,
                ParentId = entity.ParentId,
                Key = entity.Key,
                Name = entity.Name
            });
        }

        [HttpPost]
        [Route("settings/parent/update")]
        public async Task<IActionResult> UpdateParentSetting(UpdateParentSettingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest("Parent id is required");
            }

            var entity = await _context.SettingParents.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (entity == null)
            {
                return NotFound("Parent setting not found");
            }

            entity.Key = request.Key?.Trim().ToLowerInvariant() ?? string.Empty;
            entity.Name = request.Name?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            return Ok(new DropdownParentItem
            {
                Id = entity.Id,
                Key = entity.Key,
                Name = entity.Name
            });
        }

        [HttpPost]
        [Route("settings/child/update")]
        public async Task<IActionResult> UpdateChildSetting(UpdateChildSettingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest("Child id is required");
            }

            if (string.IsNullOrWhiteSpace(request.ParentId))
            {
                return BadRequest("Parent id is required");
            }

            var parentExists = await _context.SettingParents.AnyAsync(x => x.Id == request.ParentId);
            if (!parentExists)
            {
                return BadRequest("Parent id not found");
            }

            var entity = await _context.SettingChildren.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (entity == null)
            {
                return NotFound("Child setting not found");
            }

            entity.ParentId = request.ParentId;
            entity.Key = request.Key?.Trim().ToLowerInvariant() ?? string.Empty;
            entity.Name = request.Name?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            return Ok(new DropdownChildItem
            {
                Id = entity.Id,
                ParentId = entity.ParentId,
                Key = entity.Key,
                Name = entity.Name
            });
        }

        [HttpPost]
        [Route("settings/parent/delete/{id}")]
        public async Task<IActionResult> DeleteParentSetting(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Parent id is required");
            }

            var entity = await _context.SettingParents.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound("Parent setting not found");
            }

            _context.SettingParents.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Parent setting deleted successfully",
                id = entity.Id
            });
        }

        [HttpPost]
        [Route("settings/child/delete/{id}")]
        public async Task<IActionResult> DeleteChildSetting(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Child id is required");
            }

            var entity = await _context.SettingChildren.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound("Child setting not found");
            }

            _context.SettingChildren.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Child setting deleted successfully",
                id = entity.Id
            });
        }
    }
}
