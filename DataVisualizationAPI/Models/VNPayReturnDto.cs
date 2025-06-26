namespace DataVisualizationAPI.Models
{
    public class VNPayReturnDto
    {
        public string vnp_TxnRef { get; set; }
        public string vnp_TransactionNo { get; set; }
        public string vnp_ResponseCode { get; set; }
        public string vnp_SecureHash { get; set; }
        public Dictionary<string, string> allParams { get; set; }
    }
} 