using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataVisualizationAPI.Models
{
    public class Dataset
    {
        [Key]
        public int DatasetId { get; set; }

        [Required]
        [StringLength(255)]
        public string DatasetName { get; set; }

        [Required]
        [StringLength(50)]
        public string SourceType { get; set; }

        [StringLength(255)]
        public string SourceName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int SchemaVersion { get; set; } = 1;

        public int TotalRows { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; }

        public virtual ICollection<DatasetSchema> Schemas { get; set; }
        public virtual ICollection<DatasetData> Data { get; set; }
    }
} 