import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import './PaymentPages.css';

const PaymentSuccessPage = () => {
	const [searchParams] = useSearchParams();
	const navigate = useNavigate();
	const [paymentInfo, setPaymentInfo] = useState(null);
	const [loading, setLoading] = useState(true);
	const [error, setError] = useState(null);

	useEffect(() => {
		const orderId = searchParams.get('orderId');
		const transactionId = searchParams.get('transactionId');
		const amount = searchParams.get('amount');
		const status = searchParams.get('status');
		const upgradedToPro = searchParams.get('upgradedToPro') === 'true';
		const message = searchParams.get('message');

		// Verify payment with backend
		const verifyPayment = async () => {
			try {
				const token = localStorage.getItem('token');
				if (!token) {
					setError('Token không hợp lệ. Vui lòng đăng nhập lại.');
					setLoading(false);
					return;
				}

				const response = await fetch(`/api/payment/check-payment-result?orderId=${orderId}`, {
					headers: {
						'Authorization': `Bearer ${token}`,
						'Content-Type': 'application/json'
					}
				});

				if (response.ok) {
					const data = await response.json();
					setPaymentInfo(data);
				} else if (response.status === 401) {
					setError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
				} else if (response.status === 404) {
					setError('Không tìm thấy thông tin thanh toán.');
				} else {
					setError('Có lỗi xảy ra khi xác thực thanh toán.');
				}
			} catch (error) {
				console.error('Error verifying payment:', error);
				setError('Không thể kết nối đến máy chủ. Vui lòng thử lại.');
			} finally {
				setLoading(false);
			}
		};

		if (orderId) {
			verifyPayment();
		} else {
			setError('Thiếu thông tin đơn hàng.');
			setLoading(false);
		}
	}, [searchParams]);

	const handleGoHome = () => {
		navigate('/');
	};

	const handleGoDashboard = () => {
		navigate('/dashboard');
	};

	if (loading) {
		return (
			<div className="payment-loading">
				<div className="loading-spinner"></div>
				<p>Đang xác thực thanh toán...</p>
			</div>
		);
	}

	if (error) {
		return (
			<div className="payment-error">
				<div className="error-icon">⚠️</div>
				<h1>Lỗi xác thực</h1>
				<p>{error}</p>
				<div className="actions">
					<button onClick={handleGoHome}>Về trang chủ</button>
					<button onClick={() => window.location.reload()}>Thử lại</button>
				</div>
			</div>
		);
	}

	return (
		<div className="payment-success">
			<div className="success-icon">✅</div>
			<h1>Thanh toán thành công!</h1>

			{paymentInfo?.isPro && (
				<div className="pro-upgrade-notice">
					<h2>🎉 Chúc mừng! Bạn đã được nâng cấp lên Pro</h2>
					<p>Bây giờ bạn có thể sử dụng tất cả các tính năng Pro của ứng dụng:</p>
					<ul>
						<li>✅ Tạo không giới hạn biểu đồ</li>
						<li>✅ Xuất dữ liệu chất lượng cao</li>
						<li>✅ Truy cập các template premium</li>
						<li>✅ Hỗ trợ ưu tiên</li>
					</ul>
				</div>
			)}

			<div className="payment-details">
				<h3>Chi tiết giao dịch:</h3>
				<div className="detail-row">
					<span className="label">Mã đơn hàng:</span>
					<span className="value">{paymentInfo?.orderId}</span>
				</div>
				<div className="detail-row">
					<span className="label">Mã giao dịch:</span>
					<span className="value">{paymentInfo?.transactionId}</span>
				</div>
				<div className="detail-row">
					<span className="label">Số tiền:</span>
					<span className="value">{paymentInfo?.amount?.toLocaleString('vi-VN')} VND</span>
				</div>
				<div className="detail-row">
					<span className="label">Trạng thái:</span>
					<span className="value status-success">{paymentInfo?.status}</span>
				</div>
				<div className="detail-row">
					<span className="label">Thời gian:</span>
					<span className="value">
						{paymentInfo?.updatedAt ? new Date(paymentInfo.updatedAt).toLocaleString('vi-VN') : 'N/A'}
					</span>
				</div>
			</div>

			<div className="actions">
				<button className="btn-primary" onClick={handleGoDashboard}>
					Đi đến Dashboard
				</button>
				<button className="btn-secondary" onClick={handleGoHome}>
					Về trang chủ
				</button>
			</div>

			<div className="help-text">
				<p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ hỗ trợ khách hàng.</p>
			</div>
		</div>
	);
};

export default PaymentSuccessPage; 