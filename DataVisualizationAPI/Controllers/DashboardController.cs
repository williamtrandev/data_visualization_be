using DataVisualizationAPI.Data;
using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataVisualizationAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDashboard(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "User not authenticated" });

            var dashboard = await _context.Set<Dashboard>()
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (dashboard == null)
                return NotFound(new { message = "Dashboard not found" });

            var response = new DashboardResponse
            {
                Id = dashboard.Id,
                Title = dashboard.Title,
                UserId = dashboard.UserId,
                CreatedAt = dashboard.CreatedAt,
                UpdatedAt = dashboard.UpdatedAt,
                Items = dashboard.Items.Select(item => new DashboardItemDTO
                {
                    Id = item.Id,
                    Type = item.Type,
                    X = item.X,
                    Y = item.Y,
                    Width = item.Width,
                    Height = item.Height,
                    Title = item.Title,
                    DataSourceId = item.DataSourceId,
                    ChartOptions = item.ChartOptions,
                    //BackgroundColor = item.BackgroundColor,
                    //BorderColor = item.BorderColor
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboards()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "User not authenticated" });

            var dashboards = await _context.Set<Dashboard>()
                .Include(d => d.Items)
                .Where(d => d.UserId == userId)
                .Select(d => new DashboardListItemResponse
                {
                    Id = d.Id,
                    Title = d.Title,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    ChartCount = d.Items.Count,
                    FirstChartType = d.Items.OrderBy(i => i.Id).Select(i => i.Type).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(dashboards);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDashboard([FromBody] CreateDashboardRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "User not authenticated" });

            var dashboard = new Dashboard
            {
                Title = request.Title,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = request.Items?.Select(item => new DashboardItem
                {
                    Type = item.Type,
                    X = item.X,
                    Y = item.Y,
                    Width = item.Width,
                    Height = item.Height,
                    Title = item.Title,
                    DataSourceId = item.DataSourceId,
                    ChartOptions = item.ChartOptions,
                    //BackgroundColor = item.BackgroundColor,
                    //BorderColor = item.BorderColor
                }).ToList() ?? new List<DashboardItem>()
            };

            _context.Add(dashboard);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Dashboard created successfully", dashboardId = dashboard.Id });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateDashboard([FromBody] UpdateDashboardRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "User not authenticated" });

            var dashboard = await _context.Set<Dashboard>()
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == request.Id && d.UserId == userId);
            if (dashboard == null)
                return NotFound(new { message = "Dashboard not found" });

            dashboard.Title = request.Title;
            dashboard.UpdatedAt = DateTime.UtcNow;

            // Xóa các item cũ không còn trong request
            var itemIds = request.Items?.Where(i => i.Id.HasValue).Select(i => i.Id.Value).ToList() ?? new List<int>();
            var itemsToRemove = dashboard.Items.Where(i => !itemIds.Contains(i.Id)).ToList();
            foreach (var item in itemsToRemove)
                _context.Remove(item);

            // Thêm hoặc cập nhật các item mới
            foreach (var itemDto in request.Items)
            {
                if (itemDto.Id.HasValue)
                {
                    // Update
                    var item = dashboard.Items.FirstOrDefault(i => i.Id == itemDto.Id.Value);
                    if (item != null)
                    {
                        item.Type = itemDto.Type;
                        item.X = itemDto.X;
                        item.Y = itemDto.Y;
                        item.Width = itemDto.Width;
                        item.Height = itemDto.Height;
                        item.Title = itemDto.Title;
                        item.DataSourceId = itemDto.DataSourceId;
                        item.ChartOptions = itemDto.ChartOptions;
                        //item.BackgroundColor = itemDto.BackgroundColor;
                        //item.BorderColor = itemDto.BorderColor;
                    }
                }
                else
                {
                    // Add new
                    dashboard.Items.Add(new DashboardItem
                    {
                        Type = itemDto.Type,
                        X = itemDto.X,
                        Y = itemDto.Y,
                        Width = itemDto.Width,
                        Height = itemDto.Height,
                        Title = itemDto.Title,
                        DataSourceId = itemDto.DataSourceId,
                        ChartOptions = itemDto.ChartOptions,
                        //BackgroundColor = itemDto.BackgroundColor,
                        //BorderColor = itemDto.BorderColor
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Dashboard updated successfully" });
        }

        /// <summary>
        /// Deletes a dashboard by ID
        /// </summary>
        /// <param name="id">The ID of the dashboard to delete</param>
        /// <returns>Success message with deleted dashboard ID</returns>
        /// <response code="200">Dashboard deleted successfully</response>
        /// <response code="400">Invalid dashboard ID</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">Dashboard not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDashboard(int id)
        {
            try
            {
                // Validate dashboard ID
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid dashboard ID provided: {DashboardId}", id);
                    return BadRequest(new { message = "Invalid dashboard ID" });
                }

                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId))
                {
                    _logger.LogWarning("Unauthorized dashboard deletion attempt for dashboard ID: {DashboardId}", id);
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var dashboard = await _context.Set<Dashboard>()
                    .Include(d => d.Items)
                    .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

                if (dashboard == null)
                {
                    _logger.LogWarning("Dashboard not found for deletion. Dashboard ID: {DashboardId}, User ID: {UserId}", id, userId);
                    return NotFound(new { message = "Dashboard not found" });
                }

                _logger.LogInformation("Deleting dashboard. Dashboard ID: {DashboardId}, Title: {Title}, User ID: {UserId}, Item Count: {ItemCount}", 
                    id, dashboard.Title, userId, dashboard.Items.Count);

                // Xóa dashboard và tất cả các items liên quan (cascade delete)
                _context.Remove(dashboard);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dashboard deleted successfully. Dashboard ID: {DashboardId}, User ID: {UserId}", id, userId);

                var response = new DeleteDashboardResponse
                {
                    Message = "Dashboard deleted successfully",
                    DeletedDashboardId = id
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dashboard. Dashboard ID: {DashboardId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the dashboard" });
            }
        }
    }
} 