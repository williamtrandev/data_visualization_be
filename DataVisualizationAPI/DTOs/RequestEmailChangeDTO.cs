using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class RequestEmailChangeDTO
    {
        [Required]
        [EmailAddress]
        [StringLength(30)]
        public string NewEmail { get; set; }
    }
} 