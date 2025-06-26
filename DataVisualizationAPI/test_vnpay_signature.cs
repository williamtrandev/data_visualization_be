using System.Security.Cryptography;
using System.Text;

// Test VNPay signature generation
public class VNPaySignatureTest
{
    public static void Main()
    {
        // Test data
        var tmnCode = "DUJ527UO";
        var hashSecret = "73R46WE5RAGYJO8GDVDSZ6ZD4BY01CDW";
        var orderId = "ORDER_20241222104530_1";
        var amount = 99000m;
        var ipAddress = "127.0.0.1";
        var returnUrl = "http://localhost:5000/api/payment/vnpay-return";
        
        // Create test parameters
        var vnpay = new SortedDictionary<string, string>();
        var createDate = DateTime.Now;
        
        vnpay.Add("vnp_Version", "2.1.0");
        vnpay.Add("vnp_Command", "pay");
        vnpay.Add("vnp_TmnCode", tmnCode);
        vnpay.Add("vnp_Amount", (amount * 100).ToString());
        vnpay.Add("vnp_CreateDate", createDate.ToString("yyyyMMddHHmmss"));
        vnpay.Add("vnp_CurrCode", "VND");
        vnpay.Add("vnp_IpAddr", ipAddress);
        vnpay.Add("vnp_Locale", "vn");
        vnpay.Add("vnp_OrderInfo", $"Thanh toan goi Pro - {orderId}");
        vnpay.Add("vnp_OrderType", "other");
        vnpay.Add("vnp_ReturnUrl", returnUrl);
        vnpay.Add("vnp_TxnRef", orderId);
        
        // Create signature data
        var signData = string.Join("&", vnpay.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        Console.WriteLine("Signature Data:");
        Console.WriteLine(signData);
        Console.WriteLine();
        
        // Generate HMAC-SHA512 signature
        var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signData));
        var signature = Convert.ToHexString(hash).ToLower();
        
        Console.WriteLine("Generated Signature:");
        Console.WriteLine(signature);
        Console.WriteLine();
        
        // Test validation
        var testTxnRef = orderId;
        var testTransactionNo = "12345678";
        var testResponseCode = "00";
        
        var validationData = new SortedDictionary<string, string>();
        validationData.Add("vnp_TxnRef", testTxnRef);
        validationData.Add("vnp_TransactionNo", testTransactionNo);
        validationData.Add("vnp_ResponseCode", testResponseCode);
        
        var validationSignData = string.Join("&", validationData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var validationHmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret));
        var validationHash = validationHmac.ComputeHash(Encoding.UTF8.GetBytes(validationSignData));
        var validationSignature = Convert.ToHexString(validationHash).ToLower();
        
        Console.WriteLine("Validation Signature Data:");
        Console.WriteLine(validationSignData);
        Console.WriteLine();
        Console.WriteLine("Validation Signature:");
        Console.WriteLine(validationSignature);
    }
} 