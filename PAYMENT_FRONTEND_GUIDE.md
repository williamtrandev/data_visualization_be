# Hướng dẫn Implement Frontend cho Payment Redirect

## Tổng quan

Sau khi thanh toán VNPay thành công, hệ thống sẽ tự động:

1. **Upgrade user lên Pro** (nếu chưa là Pro)
2. **Redirect về frontend** với thông tin chi tiết
3. **Hiển thị thông báo thành công** cho user

## URL Redirect Structure

### Success URL (Khi thanh toán thành công)

```
http://localhost:8080/payment/success?
  orderId=ORDER_20241201123456_123&
  transactionId=12345678&
  amount=99000&
  status=success&
  upgradedToPro=true&
  message=Thanh toán thành công! Bạn đã được nâng cấp lên Pro.
```

### Failure URL (Khi thanh toán thất bại)

```
http://localhost:8080/payment/failed?
  orderId=ORDER_20241201123456_123&
  transactionId=12345678&
  responseCode=07&
  amount=99000&
  status=failed&
  message=Thanh toán thất bại. Vui lòng thử lại.
```

## Frontend Implementation

### 1. Payment Success Page (`/payment/success`)

```javascript
// React/Vue component example
import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";

const PaymentSuccess = () => {
    const [searchParams] = useSearchParams();
    const [paymentInfo, setPaymentInfo] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const orderId = searchParams.get("orderId");
        const transactionId = searchParams.get("transactionId");
        const amount = searchParams.get("amount");
        const status = searchParams.get("status");
        const upgradedToPro = searchParams.get("upgradedToPro") === "true";
        const message = searchParams.get("message");

        // Verify payment with backend
        const verifyPayment = async () => {
            try {
                const response = await fetch(
                    `/api/payment/check-payment-result?orderId=${orderId}`,
                    {
                        headers: {
                            Authorization: `Bearer ${localStorage.getItem(
                                "token"
                            )}`,
                        },
                    }
                );

                if (response.ok) {
                    const data = await response.json();
                    setPaymentInfo(data);
                }
            } catch (error) {
                console.error("Error verifying payment:", error);
            } finally {
                setLoading(false);
            }
        };

        if (orderId) {
            verifyPayment();
        }
    }, [searchParams]);

    if (loading) {
        return <div>Đang xác thực thanh toán...</div>;
    }

    return (
        <div className="payment-success">
            <div className="success-icon">✅</div>
            <h1>Thanh toán thành công!</h1>

            {paymentInfo?.isPro && (
                <div className="pro-upgrade-notice">
                    <h2>🎉 Chúc mừng! Bạn đã được nâng cấp lên Pro</h2>
                    <p>
                        Bây giờ bạn có thể sử dụng tất cả các tính năng Pro của
                        ứng dụng.
                    </p>
                </div>
            )}

            <div className="payment-details">
                <h3>Chi tiết giao dịch:</h3>
                <p>
                    <strong>Mã đơn hàng:</strong> {paymentInfo?.orderId}
                </p>
                <p>
                    <strong>Mã giao dịch:</strong> {paymentInfo?.transactionId}
                </p>
                <p>
                    <strong>Số tiền:</strong>{" "}
                    {paymentInfo?.amount?.toLocaleString("vi-VN")} VND
                </p>
                <p>
                    <strong>Trạng thái:</strong> {paymentInfo?.status}
                </p>
            </div>

            <div className="actions">
                <button onClick={() => (window.location.href = "/")}>
                    Về trang chủ
                </button>
                <button onClick={() => (window.location.href = "/dashboard")}>
                    Đi đến Dashboard
                </button>
            </div>
        </div>
    );
};

export default PaymentSuccess;
```

### 2. Payment Failure Page (`/payment/failed`)

```javascript
const PaymentFailed = () => {
    const [searchParams] = useSearchParams();
    const [paymentInfo, setPaymentInfo] = useState(null);

    useEffect(() => {
        const orderId = searchParams.get("orderId");
        const transactionId = searchParams.get("transactionId");
        const responseCode = searchParams.get("responseCode");
        const amount = searchParams.get("amount");
        const status = searchParams.get("status");
        const message = searchParams.get("message");

        setPaymentInfo({
            orderId,
            transactionId,
            responseCode,
            amount,
            status,
            message,
        });
    }, [searchParams]);

    const getErrorMessage = (responseCode) => {
        const errorMessages = {
            "07": "Giao dịch bị nghi ngờ gian lận",
            "09": "Giao dịch không thành công",
            65: "Tài khoản không đủ số dư",
            75: "Ngân hàng đang bảo trì",
            79: "Khách hàng hủy giao dịch",
            99: "Lỗi không xác định",
        };
        return errorMessages[responseCode] || "Thanh toán thất bại";
    };

    return (
        <div className="payment-failed">
            <div className="error-icon">❌</div>
            <h1>Thanh toán thất bại</h1>

            <div className="error-message">
                <p>{getErrorMessage(paymentInfo?.responseCode)}</p>
                <p>{paymentInfo?.message}</p>
            </div>

            <div className="payment-details">
                <h3>Chi tiết giao dịch:</h3>
                <p>
                    <strong>Mã đơn hàng:</strong> {paymentInfo?.orderId}
                </p>
                <p>
                    <strong>Mã giao dịch:</strong> {paymentInfo?.transactionId}
                </p>
                <p>
                    <strong>Số tiền:</strong>{" "}
                    {paymentInfo?.amount?.toLocaleString("vi-VN")} VND
                </p>
                <p>
                    <strong>Mã lỗi:</strong> {paymentInfo?.responseCode}
                </p>
            </div>

            <div className="actions">
                <button
                    onClick={() => (window.location.href = "/payment/create")}
                >
                    Thử lại thanh toán
                </button>
                <button onClick={() => (window.location.href = "/")}>
                    Về trang chủ
                </button>
            </div>
        </div>
    );
};
```

### 3. CSS Styling

```css
.payment-success,
.payment-failed {
    max-width: 600px;
    margin: 50px auto;
    padding: 40px;
    text-align: center;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.payment-success {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
}

.payment-failed {
    background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
    color: white;
}

.success-icon,
.error-icon {
    font-size: 64px;
    margin-bottom: 20px;
}

.pro-upgrade-notice {
    background: rgba(255, 255, 255, 0.2);
    padding: 20px;
    border-radius: 8px;
    margin: 20px 0;
    backdrop-filter: blur(10px);
}

.payment-details {
    background: rgba(255, 255, 255, 0.1);
    padding: 20px;
    border-radius: 8px;
    margin: 20px 0;
    text-align: left;
}

.payment-details h3 {
    margin-top: 0;
    text-align: center;
}

.actions {
    margin-top: 30px;
}

.actions button {
    margin: 0 10px;
    padding: 12px 24px;
    border: none;
    border-radius: 6px;
    background: rgba(255, 255, 255, 0.2);
    color: white;
    cursor: pointer;
    transition: all 0.3s ease;
}

.actions button:hover {
    background: rgba(255, 255, 255, 0.3);
    transform: translateY(-2px);
}
```

## API Endpoints

### 1. Kiểm tra kết quả thanh toán

```
GET /api/payment/check-payment-result?orderId={orderId}
Authorization: Bearer {token}
```

**Response:**

```json
{
    "orderId": "ORDER_20241201123456_123",
    "status": "SUCCESS",
    "amount": 99000,
    "transactionId": "12345678",
    "responseCode": "00",
    "message": "Payment successful",
    "createdAt": "2024-12-01T12:34:56Z",
    "updatedAt": "2024-12-01T12:35:00Z",
    "isPro": true,
    "success": true,
    "canUpgrade": false
}
```

### 2. Kiểm tra trạng thái Pro

```
GET /api/payment/pro-status
Authorization: Bearer {token}
```

**Response:**

```json
{
    "isPro": true,
    "message": "User is a Pro member"
}
```

## Luồng xử lý hoàn chỉnh

1. **User click thanh toán** → Gọi API `POST /api/payment/create-payment`
2. **Redirect đến VNPay** → User thanh toán
3. **VNPay callback** → Backend xử lý và tự động upgrade Pro
4. **Redirect về frontend** → Hiển thị trang success/failure
5. **Frontend verify** → Gọi API `GET /api/payment/check-payment-result`
6. **Hiển thị thông báo** → User thấy được upgrade lên Pro

## Lưu ý quan trọng

1. **Tự động upgrade**: Backend sẽ tự động upgrade user lên Pro khi thanh toán thành công
2. **Verification**: Frontend nên verify lại với backend để đảm bảo tính chính xác
3. **Error handling**: Xử lý các trường hợp lỗi network, token expired, etc.
4. **User experience**: Hiển thị loading state và thông báo rõ ràng
5. **Security**: Luôn verify payment với backend, không chỉ dựa vào URL parameters
