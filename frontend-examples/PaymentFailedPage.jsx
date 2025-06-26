import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import './PaymentPages.css';

const PaymentFailedPage = () => {
	const [searchParams] = useSearchParams();
	const navigate = useNavigate();
	const [paymentInfo, setPaymentInfo] = useState(null);

	useEffect(() => {
		const orderId = searchParams.get('orderId');
		const transactionId = searchParams.get('transactionId');
		const responseCode = searchParams.get('responseCode');
		const amount = searchParams.get('amount');
		const status = searchParams.get('status');
		const message = searchParams.get('message');

		setPaymentInfo({
			orderId,
			transactionId,
			responseCode,
			amount,
			status,
			message
		});
	}, [searchParams]);

	const getErrorMessage = (responseCode) => {
		const errorMessages = {
			'07': 'Giao dịch bị nghi ngờ gian lận',
			'09': 'Giao dịch không thành công',
			'10': 'Khách hàng hủy giao dịch',
			'11': 'Giao dịch bị lỗi',
			'12': 'Giao dịch không hợp lệ',
			'13': 'Số tiền không hợp lệ',
			'24': 'Khách hàng hủy giao dịch',
			'51': 'Tài khoản không đủ số dư',
			'65': 'Tài khoản không đủ số dư',
			'75': 'Ngân hàng đang bảo trì',
			'79': 'Khách hàng hủy giao dịch',
			'99': 'Lỗi không xác định'
		};
		return errorMessages[responseCode] || 'Thanh toán thất bại';
	};

	const getErrorDescription = (responseCode) => {
		const descriptions = {
			'07': 'Giao dịch của bạn đã bị từ chối do nghi ngờ gian lận. Vui lòng liên hệ ngân hàng để được hỗ trợ.',
			'09': 'Giao dịch không thể hoàn thành. Vui lòng kiểm tra lại thông tin và thử lại.',
			'10': 'Bạn đã hủy giao dịch thanh toán.',
			'11': 'Đã xảy ra lỗi trong quá trình xử lý giao dịch.',
			'12': 'Giao dịch không hợp lệ. Vui lòng thử lại.',
			'13': 'Số tiền thanh toán không hợp lệ.',
			'24': 'Bạn đã hủy giao dịch thanh toán.',
			'51': 'Tài khoản của bạn không đủ số dư để thực hiện giao dịch.',
			'65': 'Tài khoản của bạn không đủ số dư để thực hiện giao dịch.',
			'75': 'Ngân hàng đang bảo trì hệ thống. Vui lòng thử lại sau.',
			'79': 'Bạn đã hủy giao dịch thanh toán.',
			'99': 'Đã xảy ra lỗi không xác định. Vui lòng thử lại sau.'
		};
		return descriptions[responseCode] || 'Đã xảy ra lỗi trong quá trình thanh toán. Vui lòng thử lại.';
	};

	const handleRetryPayment = () => {
		navigate('/payment/create');
	};

	const handleGoHome = () => {
		navigate('/');
	};

	const handleContactSupport = () => {
		// Mở email client hoặc chat support
		window.open('mailto:support@datavisualization.com?subject=Payment Issue', '_blank');
	};

	return (
		<div className="payment-failed">
			<div className="error-icon">❌</div>
			<h1>Thanh toán thất bại</h1>

			<div className="error-message">
				<h3>{getErrorMessage(paymentInfo?.responseCode)}</h3>
				<p>{getErrorDescription(paymentInfo?.responseCode)}</p>
			</div>

			<div className="payment-details">
				<h3>Chi tiết giao dịch:</h3>
				<div className="detail-row">
					<span className="label">Mã đơn hàng:</span>
					<span className="value">{paymentInfo?.orderId}</span>
				</div>
				<div className="detail-row">
					<span className="label">Mã giao dịch:</span>
					<span className="value">{paymentInfo?.transactionId || 'N/A'}</span>
				</div>
				<div className="detail-row">
					<span className="label">Số tiền:</span>
					<span className="value">{paymentInfo?.amount?.toLocaleString('vi-VN')} VND</span>
				</div>
				<div className="detail-row">
					<span className="label">Mã lỗi:</span>
					<span className="value status-error">{paymentInfo?.responseCode}</span>
				</div>
				<div className="detail-row">
					<span className="label">Trạng thái:</span>
					<span className="value status-error">{paymentInfo?.status}</span>
				</div>
			</div>

			<div className="suggestions">
				<h3>Bạn có thể thử:</h3>
				<ul>
					<li>✅ Kiểm tra lại thông tin thẻ/tài khoản</li>
					<li>✅ Đảm bảo tài khoản có đủ số dư</li>
					<li>✅ Thử lại với phương thức thanh toán khác</li>
					<li>✅ Liên hệ ngân hàng nếu cần hỗ trợ</li>
				</ul>
			</div>

			<div className="actions">
				<button className="btn-primary" onClick={handleRetryPayment}>
					Thử lại thanh toán
				</button>
				<button className="btn-secondary" onClick={handleGoHome}>
					Về trang chủ
				</button>
				<button className="btn-support" onClick={handleContactSupport}>
					Liên hệ hỗ trợ
				</button>
			</div>

			<div className="help-text">
				<p>
					Nếu vấn đề vẫn tiếp tục, vui lòng liên hệ với chúng tôi qua email:
					<a href="mailto:support@datavisualization.com">support@datavisualization.com</a>
				</p>
			</div>
		</div>
	);
};

export default PaymentFailedPage; 