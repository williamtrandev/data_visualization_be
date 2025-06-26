using DataVisualizationAPI.Data;
using DataVisualizationAPI.Models;
using DataVisualizationAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace DataVisualizationAPI.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly VNPayService _vnpayService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;

        public PaymentController(
            AppDbContext context, 
            VNPayService vnpayService,
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _vnpayService = vnpayService;
            _logger = logger;
            _configuration = configuration;
        }

        [Authorize]
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (user.IsPro)
                {
                    return BadRequest(new { message = "User is already a Pro member" });
                }

                // Create payment record
                var orderId = $"ORDER_{DateTime.Now:yyyyMMddHHmmss}_{userId}";
                var amount = 300000m; // 300,000 VND for Pro subscription

                var payment = new Payment
                {
                    UserId = user.Id,
                    OrderId = orderId,
                    Amount = amount,
                    PaymentMethod = "VNPAY",
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    Message = "Payment initiated",
                    ResponseCode = "00",
                    TransactionId = ""
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Get client IP address
                // var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var ipAddress = "127.0.0.1";
                _logger.LogInformation("Creating payment URL for IP: {IpAddress}", ipAddress);

                // Create VNPay payment URL
                var paymentUrl = _vnpayService.CreatePaymentUrl(orderId, amount, ipAddress);
                _logger.LogInformation("Payment URL created successfully: {PaymentUrl}", paymentUrl);

                return Ok(new { paymentUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return StatusCode(500, new { message = "An error occurred while creating payment", error = ex.Message });
            }
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            try
            {
                // Log all request information
                _logger.LogInformation("Received VNPay return request");
                _logger.LogInformation("Request URL: {Scheme}://{Host}{Path}{QueryString}", 
                    Request.Scheme, Request.Host, Request.Path, Request.QueryString);
                _logger.LogInformation("Request Method: {Method}", Request.Method);
                
                // Log all query parameters
                foreach (var param in Request.Query)
                {
                    _logger.LogInformation("Query parameter: {Key}={Value}", param.Key, param.Value);
                }

                // If no parameters, return a test response
                if (!Request.Query.Any())
                {
                    _logger.LogWarning("No parameters received from VNPay");
                    return Ok(new { 
                        message = "Callback URL is accessible but no parameters received",
                        timestamp = DateTime.UtcNow,
                        url = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}"
                    });
                }

                // Extract all VNPay parameters
                var vnpParams = new Dictionary<string, string>();
                var vnp_SecureHash = "";

                foreach (var param in Request.Query)
                {
                    if (param.Key.StartsWith("vnp_"))
                    {
                        if (param.Key == "vnp_SecureHash")
                        {
                            vnp_SecureHash = param.Value.ToString();
                        }
                        else
                        {
                            vnpParams[param.Key] = param.Value.ToString();
                        }
                    }
                }

                // Check required parameters
                if (!vnpParams.ContainsKey("vnp_TxnRef") || 
                    !vnpParams.ContainsKey("vnp_TransactionNo") || 
                    !vnpParams.ContainsKey("vnp_ResponseCode") || 
                    string.IsNullOrEmpty(vnp_SecureHash))
                {
                    _logger.LogWarning("Missing required parameters from VNPay");
                    return BadRequest(new { message = "Missing required parameters" });
                }

                var vnp_TxnRef = vnpParams["vnp_TxnRef"];
                var vnp_TransactionNo = vnpParams["vnp_TransactionNo"];
                var vnp_ResponseCode = vnpParams["vnp_ResponseCode"];

                _logger.LogInformation("Received VNPay return with parameters: TxnRef={TxnRef}, TransactionNo={TransactionNo}, ResponseCode={ResponseCode}, SecureHash={SecureHash}",
                    vnp_TxnRef, vnp_TransactionNo, vnp_ResponseCode, vnp_SecureHash);

                // Validate signature using the same logic as Node.js
                var isValidSignature = _vnpayService.ValidatePaymentResponse(vnpParams);
                if (!isValidSignature)
                {
                    _logger.LogWarning("Invalid payment response signature");
                    
                    // Log the signature data for debugging (like in Node.js)
                    var sortedParams = vnpParams.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                    var signData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    _logger.LogWarning("Expected signature data: {SignatureData}", signData);
                    
                    return BadRequest(new { message = "Invalid payment response signature" });
                }

                var payment = await _context.Payments
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.OrderId == vnp_TxnRef);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for OrderId: {OrderId}", vnp_TxnRef);
                    return NotFound(new { message = "Payment not found" });
                }

                if (vnp_ResponseCode == "00")
                {
                    // Payment successful - update payment status
                    payment.Status = "SUCCESS";
                    payment.TransactionId = vnp_TransactionNo;
                    payment.ResponseCode = vnp_ResponseCode;
                    payment.Message = "Payment successful";
                    payment.UpdatedAt = DateTime.UtcNow;

                    // Automatically upgrade user to Pro if not already Pro
                    var wasProBefore = payment.User.IsPro;
                    if (!payment.User.IsPro)
                    {
                        payment.User.IsPro = true;
                        payment.User.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("User automatically upgraded to Pro for OrderId: {OrderId}", vnp_TxnRef);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Payment successful for OrderId: {OrderId}", vnp_TxnRef);

                    // Redirect to frontend success page with detailed information
                    var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:8080";
                    var successUrl = $"{frontendUrl}/payment/success?" +
                        $"orderId={vnp_TxnRef}&" +
                        $"transactionId={vnp_TransactionNo}&" +
                        $"amount={payment.Amount}&" +
                        $"status=success&" +
                        $"upgradedToPro={!wasProBefore}&" +
                        $"message=Thanh toán thành công! Bạn đã được nâng cấp lên Pro.";
                    
                    return Redirect(successUrl);
                }
                else
                {
                    // Payment failed - update payment status
                    payment.Status = "FAILED";
                    payment.TransactionId = vnp_TransactionNo;
                    payment.ResponseCode = vnp_ResponseCode;
                    payment.Message = "Payment failed";
                    payment.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogWarning("Payment failed for OrderId: {OrderId} with ResponseCode: {ResponseCode}", 
                        vnp_TxnRef, vnp_ResponseCode);

                    // Redirect to frontend failure page with detailed information
                    var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:8080";
                    var failureUrl = $"{frontendUrl}/payment/failed?" +
                        $"orderId={vnp_TxnRef}&" +
                        $"transactionId={vnp_TransactionNo}&" +
                        $"responseCode={vnp_ResponseCode}&" +
                        $"amount={payment.Amount}&" +
                        $"status=failed&" +
                        $"message=Thanh toán thất bại. Vui lòng thử lại.";
                    
                    return Redirect(failureUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return");
                return StatusCode(500, new { message = "An error occurred while processing payment", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("pro-status")]
        public async Task<IActionResult> GetProStatus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new { 
                    isPro = user.IsPro,
                    message = user.IsPro ? "User is a Pro member" : "User is not a Pro member"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Pro status");
                return StatusCode(500, new { message = "An error occurred while getting Pro status", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("payment-status/{orderId}")]
        public async Task<IActionResult> GetPaymentStatus(string orderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var payment = await _context.Payments
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.UserId == int.Parse(userId));

                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                return Ok(new
                {
                    orderId = payment.OrderId,
                    status = payment.Status,
                    amount = payment.Amount,
                    transactionId = payment.TransactionId,
                    responseCode = payment.ResponseCode,
                    message = payment.Message,
                    createdAt = payment.CreatedAt,
                    updatedAt = payment.UpdatedAt,
                    isPro = payment.User.IsPro
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { message = "An error occurred while getting payment status", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("complete-payment/{orderId}")]
        public async Task<IActionResult> CompletePayment(string orderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var payment = await _context.Payments
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.UserId == int.Parse(userId));

                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                // Kiểm tra payment đã thành công chưa
                if (payment.Status != "SUCCESS")
                {
                    return BadRequest(new { message = "Payment is not successful", status = payment.Status });
                }

                // Kiểm tra user đã là Pro chưa
                if (payment.User.IsPro)
                {
                    return BadRequest(new { message = "User is already a Pro member" });
                }

                // Update user to Pro
                payment.User.IsPro = true;
                payment.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User upgraded to Pro for OrderId: {OrderId}", orderId);

                return Ok(new
                {
                    message = "User successfully upgraded to Pro",
                    orderId = payment.OrderId,
                    transactionId = payment.TransactionId,
                    isPro = payment.User.IsPro
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing payment for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { message = "An error occurred while completing payment", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("check-payment-result")]
        public async Task<IActionResult> CheckPaymentResult([FromQuery] string orderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                if (string.IsNullOrEmpty(orderId))
                {
                    return BadRequest(new { message = "OrderId is required" });
                }

                var payment = await _context.Payments
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.UserId == int.Parse(userId));

                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                return Ok(new
                {
                    orderId = payment.OrderId,
                    status = payment.Status,
                    amount = payment.Amount,
                    transactionId = payment.TransactionId,
                    responseCode = payment.ResponseCode,
                    message = payment.Message,
                    createdAt = payment.CreatedAt,
                    updatedAt = payment.UpdatedAt,
                    isPro = payment.User.IsPro,
                    success = payment.Status == "SUCCESS",
                    canUpgrade = payment.Status == "SUCCESS" && !payment.User.IsPro
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment result for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { message = "An error occurred while checking payment result", error = ex.Message });
            }
        }

        [HttpPost("verify-vnpay-return")]
        public async Task<IActionResult> VerifyVNPayReturn([FromBody] VNPayReturnDto dto)
        {
            try
            {
                // Kiểm tra các trường bắt buộc
                if (dto == null || dto.allParams == null || 
                    string.IsNullOrEmpty(dto.vnp_TxnRef) ||
                    string.IsNullOrEmpty(dto.vnp_TransactionNo) ||
                    string.IsNullOrEmpty(dto.vnp_ResponseCode) ||
                    string.IsNullOrEmpty(dto.vnp_SecureHash))
                {
                    return BadRequest(new { message = "Missing required parameters" });
                }

                // Thêm vnp_SecureHash vào allParams nếu chưa có (phục vụ validate)
                if (!dto.allParams.ContainsKey("vnp_SecureHash"))
                    dto.allParams["vnp_SecureHash"] = dto.vnp_SecureHash;

                // Validate chữ ký
                var isValidSignature = _vnpayService.ValidatePaymentResponse(new Dictionary<string, string>(dto.allParams));
                if (!isValidSignature)
                {
                    return BadRequest(new { message = "Invalid payment response signature" });
                }

                // Tìm payment theo orderId
                var payment = await _context.Payments
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.OrderId == dto.vnp_TxnRef);

                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                // Cập nhật trạng thái payment
                if (dto.vnp_ResponseCode == "00")
                {
                    payment.Status = "SUCCESS";
                    payment.TransactionId = dto.vnp_TransactionNo;
                    payment.ResponseCode = dto.vnp_ResponseCode;
                    payment.Message = "Payment successful";
                    payment.UpdatedAt = DateTime.UtcNow;

                    // Upgrade user lên Pro nếu chưa Pro
                    if (!payment.User.IsPro)
                    {
                        payment.User.IsPro = true;
                        payment.User.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    payment.Status = "FAILED";
                    payment.TransactionId = dto.vnp_TransactionNo;
                    payment.ResponseCode = dto.vnp_ResponseCode;
                    payment.Message = "Payment failed";
                    payment.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    orderId = payment.OrderId,
                    transactionId = payment.TransactionId,
                    amount = payment.Amount,
                    status = payment.Status,
                    isPro = payment.User.IsPro,
                    message = payment.Message,
                    responseCode = payment.ResponseCode,
                    updatedAt = payment.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying VNPay return");
                return StatusCode(500, new { message = "An error occurred while verifying payment", error = ex.Message });
            }
        }
    }
} 