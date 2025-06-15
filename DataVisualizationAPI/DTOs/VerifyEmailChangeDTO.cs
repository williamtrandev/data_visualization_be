using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class VerifyEmailChangeDTO
    {
        [Required]
        [EmailAddress]
        [StringLength(30)]
        public string NewEmail { get; set; }

        [Required]
        public string OTP { get; set; }
    }
} 