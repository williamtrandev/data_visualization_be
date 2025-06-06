using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class RegisterDTO
    {
        [Required]
        [StringLength(30)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(30)]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
