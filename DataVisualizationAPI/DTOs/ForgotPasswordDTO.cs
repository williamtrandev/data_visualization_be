using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
} 