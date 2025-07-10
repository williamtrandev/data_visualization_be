using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class DashboardItemDTO
    {
        public int? Id { get; set; } // null khi tạo mới, có giá trị khi update
        [Required]
        public string Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Title { get; set; }
        public string DataSourceId { get; set; }
        public string ChartOptions { get; set; } // JSON string
        //public string BackgroundColor { get; set; }
        //public string BorderColor { get; set; }
    }

    public class CreateDashboardRequest
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        public List<DashboardItemDTO> Items { get; set; }
    }

    public class UpdateDashboardRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        public List<DashboardItemDTO> Items { get; set; }
    }

    public class DashboardResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<DashboardItemDTO> Items { get; set; }
    }

    public class DashboardListItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ChartCount { get; set; }
        public string FirstChartType { get; set; }
    }

    public class DeleteDashboardResponse
    {
        public string Message { get; set; }
        public int DeletedDashboardId { get; set; }
    }
} 