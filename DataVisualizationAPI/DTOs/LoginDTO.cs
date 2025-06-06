using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class LoginDTO
    {
        [Required]
        [StringLength(30)]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
