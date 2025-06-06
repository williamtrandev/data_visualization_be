using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataVisualizationAPI.Models
{
    public class DatasetSchema
    {
        [Key]
        public int SchemaId { get; set; }

        [Required]
        public int DatasetId { get; set; }

        [Required]
        [StringLength(255)]
        public string ColumnName { get; set; }

        [Required]
        [StringLength(50)]
        public string DataType { get; set; }

        public bool IsRequired { get; set; }

        public string Description { get; set; }

        [StringLength(255)]
        public string DisplayName { get; set; }

        public int ColumnOrder { get; set; }

        [ForeignKey("DatasetId")]
        public virtual Dataset Dataset { get; set; }
    }
} 