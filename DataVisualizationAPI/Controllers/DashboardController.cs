using DataVisualizationAPI.Data;
using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public DashboardController(AppDbContext context)
        {
            _context = context;
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
    }
} 