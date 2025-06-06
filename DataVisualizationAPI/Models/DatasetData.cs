using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataVisualizationAPI.Models
{
    public class DatasetData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DatasetId { get; set; }

        [Required]
        public string RowData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("DatasetId")]
        public virtual Dataset Dataset { get; set; }
    }
} 