namespace DataVisualizationAPI.Services
{
    public static class EmailTemplates
    {
        private static string GetBaseTemplate(string content)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Data Visualization</title>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                        }}
                        .header {{
                            background-color: #4a90e2;
                            color: white;
                            padding: 20px;
                            text-align: center;
                            border-radius: 5px 5px 0 0;
                        }}
                        .content {{
                            background-color: #ffffff;
                            padding: 20px;
                            border: 1px solid #e0e0e0;
                            border-radius: 0 0 5px 5px;
                        }}
                        .otp-code {{
                            background-color: #f5f5f5;
                            padding: 15px;
                            text-align: center;
                            font-size: 24px;
                            font-weight: bold;
                            color: #4a90e2;
                            margin: 20px 0;
                            border-radius: 5px;
                            letter-spacing: 5px;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            background-color: #4a90e2;
                            color: white;
                            text-decoration: none;
                            border-radius: 5px;
                            margin: 20px 0;
                        }}
                        .footer {{
                            text-align: center;
                            margin-top: 20px;
                            color: #666;
                            font-size: 12px;
                        }}
                        .warning {{
                            background-color: #fff3cd;
                            color: #856404;
                            padding: 10px;
                            border-radius: 5px;
                            margin: 20px 0;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Data Visualization</h1>
                        </div>
                        <div class='content'>
                            {content}
                        </div>
                        <div class='footer'>
                            <p>This is an automated message, please do not reply.</p>
                            <p>&copy; {DateTime.Now.Year} Data Visualization. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        public static string GetPasswordResetTemplate(string resetLink)
        {
            var content = $@"
                <h2>Password Reset Request</h2>
                <p>We received a request to reset your password. Click the button below to reset your password:</p>
                <p style='text-align: center;'>
                    <a href='{resetLink}' class='button'>Reset Password</a>
                </p>
                <p>Or copy and paste this link into your browser:</p>
                <p style='word-break: break-all;'>{resetLink}</p>
                <div class='warning'>
                    <p><strong>Note:</strong> This link will expire in 1 hour.</p>
                </div>
                <p>If you did not request this password reset, please ignore this email or contact support if you have concerns.</p>
                <p>Best regards,<br>Data Visualization Team</p>";

            return GetBaseTemplate(content);
        }

        public static string GetEmailChangeOTPTemplate(string otp)
        {
            var content = $@"
                <h2>Email Change Verification</h2>
                <p>You have requested to use this email address for your account. To complete the verification, please use the following code:</p>
                <div class='otp-code'>{otp}</div>
                <div class='warning'>
                    <p><strong>Note:</strong> This code will expire in 5 minutes.</p>
                </div>
                <p>If you did not request this change, please ignore this email or contact support if you have concerns.</p>
                <p>Best regards,<br>Data Visualization Team</p>";

            return GetBaseTemplate(content);
        }

        public static string GetEmailChangeNotificationTemplate(string oldEmail, string newEmail)
        {
            var content = $@"
                <h2>Email Address Changed</h2>
                <p>Your email address has been successfully changed:</p>
                <div class='warning'>
                    <p><strong>Old Email:</strong> {oldEmail}</p>
                    <p><strong>New Email:</strong> {newEmail}</p>
                </div>
                <p>If you did not make this change, please contact support immediately.</p>
                <p>Best regards,<br>Data Visualization Team</p>";

            return GetBaseTemplate(content);
        }
    }
} 