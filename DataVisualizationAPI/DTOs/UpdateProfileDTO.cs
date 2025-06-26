using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class UpdateProfileDTO
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }
    }
} 