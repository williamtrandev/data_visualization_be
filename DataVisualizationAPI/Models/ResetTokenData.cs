namespace DataVisualizationAPI.Models
{
    public class ResetTokenData
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public DateTime Expiry { get; set; }
    }
} 