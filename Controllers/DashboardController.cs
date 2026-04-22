using MemmoApi.Data;
using MemmoApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemmoApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public DashboardController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [HttpPost]
        [Route("getChartData")]
        public async Task<IActionResult> GetChartData(ChartDataRequestDTO request)
        {
            try
            {
                var userId = _userService.GetMyId();

                // Query tasks for the current user within the date range
                var query = _context.Tasks.Where(x => x.UserID == userId);

                if (request.StartDate.HasValue)
                {
                    query = query.Where(x => x.StartDate.HasValue && x.StartDate.Value.Date >= request.StartDate.Value.Date);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(x => x.StartDate.HasValue && x.StartDate.Value.Date <= request.EndDate.Value.Date);
                }

                var tasks = await query.ToListAsync();

                // Prepare response data
                var response = new ChartDataResponseDTO();

                // Group by ProjectName
                var projectsGrouped = tasks
                    .GroupBy(x => x.ProjectName)
                    .Select(g => new ProjectData
                    {
                        ProjectName = g.Key,
                        TaskCount = g.Count(),
                        TotalDuration = g.Sum(x => x.Duration.HasValue ? x.Duration.Value : 0)
                    })
                    .ToList();

                response.Projects = projectsGrouped;

                // Group by Status
                var statusesGrouped = tasks
                    .GroupBy(x => x.Status)
                    .Select(g => new StatusData
                    {
                        Status = g.Key,
                        TaskCount = g.Count()
                    })
                    .ToList();

                response.Statuses = statusesGrouped;

                // Group by ProjectName and Status
                var tasksSummary = tasks
                    .GroupBy(x => new { x.ProjectName, x.Status })
                    .Select(g => new TaskSummary
                    {
                        ProjectName = g.Key.ProjectName,
                        Status = g.Key.Status,
                        Count = g.Count(),
                        TotalDuration = g.Sum(x => x.Duration.HasValue ? x.Duration.Value : 0)
                    })
                    .ToList();

                response.TasksSummary = tasksSummary;

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("summarytoday")]
        public async Task<IActionResult> SummaryToday(SummaryTodayResponseDTO item)
        {
            try
            {
                var userId = _userService.GetMyId();
                var today = item.Date;

                var tasks = await _context.Tasks
                    .Where(x => x.UserID == userId && x.StartDate.HasValue && x.StartDate.Value.Date == today)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();

                var response = new SummaryTodayResponseDTO
                {
                    Date = today,
                    TotalTasks = tasks.Count,
                    TotalHours = tasks.Sum(x => x.Duration ?? 0),
                    Tasks = tasks.Select(x => new TodayTaskItemDTO
                    {
                        TaskId = x.Id,
                        ProjectName = x.ProjectName,
                        TaskName = x.TaskName,
                        Status = x.Status,
                        Duration = x.Duration ?? 0,
                        CreatedAt = x.StartDate,
                        Hyperlink = x.Hyperlink
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
