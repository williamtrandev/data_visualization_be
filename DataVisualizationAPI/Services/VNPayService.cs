using System.Security.Cryptography;
using System.Text;
using DataVisualizationAPI.Models;
using Microsoft.Extensions.Options;

namespace DataVisualizationAPI.Services
{
    public class VNPayService
    {
        private readonly VNPayConfig _config;

        public VNPayService(IOptions<VNPayConfig> config)
        {
            _config = config.Value;
        }

        public string CreatePaymentUrl(string orderId, decimal amount, string ipAddress)
        {
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");

            // Create parameters exactly like Node.js
            var vnpParams = new Dictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _config.TmnCode },
                { "vnp_Locale", "vn" },
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", orderId },
                { "vnp_OrderInfo", "Thanh toan cho ma GD:" + orderId },
                { "vnp_OrderType", "other" },
                { "vnp_Amount", (amount * 100).ToString() },
                { "vnp_ReturnUrl", _config.ReturnUrl },
                { "vnp_IpAddr", ipAddress },
                { "vnp_CreateDate", createDate }
            };

            // Sort parameters alphabetically (like sortObject in Node.js)
            var sortedParams = SortObject(vnpParams);

            // Create sign data string (like querystring.stringify with encode: false)
            var signData = CreateSignData(sortedParams);

            // Create HMAC-SHA512 signature (like in Node.js)
            var signature = CreateSecureHash(signData);

            // Add signature to params
            sortedParams["vnp_SecureHash"] = signature;

            // Create query string (like querystring.stringify)
            var queryString = CreateQueryString(sortedParams);
            
            return _config.BaseUrl + "?" + queryString;
        }

        public bool ValidatePaymentResponse(Dictionary<string, string> vnpayParams)
        {
            try
            {
                var secureHash = vnpayParams["vnp_SecureHash"];
                vnpayParams.Remove("vnp_SecureHash");
                vnpayParams.Remove("vnp_SecureHashType");

                var sortedParams = SortObject(vnpayParams);
                var signData = CreateSignData(sortedParams);
                var checkSum = CreateSecureHash(signData);
                
                return secureHash == checkSum;
            }
            catch
            {
                return false;
            }
        }

        // Sort object by key (like sortObject in Node.js)
        private Dictionary<string, string> SortObject(Dictionary<string, string> obj)
        {
            var sorted = new Dictionary<string, string>();
            var keys = obj.Keys.OrderBy(k => k).ToList();
            
            foreach (var key in keys)
            {
                var encodedKey = Uri.EscapeDataString(key);
                var encodedValue = Uri.EscapeDataString(obj[key]).Replace("%20", "+");
                sorted[encodedKey] = encodedValue;
            }
            
            return sorted;
        }

        // Create sign data string (like createSignData in Node.js)
        private string CreateSignData(Dictionary<string, string> vnpayParams)
        {
            return string.Join("&", vnpayParams
                .Where(kvp => kvp.Key != "vnp_SecureHash" && kvp.Key != "vnp_SecureHashType")
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        // Create query string (like createQueryString in Node.js)
        private string CreateQueryString(Dictionary<string, string> vnpayParams)
        {
            return string.Join("&", vnpayParams
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        // Create secure hash (like createSecureHash in Node.js)
        private string CreateSecureHash(string data)
        {
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_config.HashSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }
    }
} 