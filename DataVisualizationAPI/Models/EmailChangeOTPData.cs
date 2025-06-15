namespace DataVisualizationAPI.Models
{
    public class EmailChangeOTPData
    {
        public int UserId { get; set; }
        public string CurrentEmail { get; set; }
        public string NewEmail { get; set; }
        public string OTP { get; set; }
        public DateTime Expiry { get; set; }
    }
} 