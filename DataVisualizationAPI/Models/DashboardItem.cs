using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.Models
{
    public class DashboardItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DashboardId { get; set; }

        [ForeignKey("DashboardId")]
        public Dashboard Dashboard { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(50)]
        public string DataSourceId { get; set; }

        public string ChartOptions { get; set; } // Lưu dưới dạng JSON
        //public string BackgroundColor { get; set; }
        //public string BorderColor { get; set; }
    }
}
