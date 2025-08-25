using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordlessOTP.API.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PasswordlessOTP.API.Services;

public class NotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;
    private readonly SendGridClient? _sendGridClient;

    public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Initialize SendGrid client if API key is provided
        var sendGridApiKey = _configuration["Notification:Email:ApiKey"];
        if (!string.IsNullOrEmpty(sendGridApiKey))
        {
            _sendGridClient = new SendGridClient(sendGridApiKey);
        }
    }

    public async Task<bool> SendOTPAsync(OTPDeliveryMethod deliveryMethod, string recipient, string otpCode, string userName)
    {
        try
        {
            switch (deliveryMethod)
            {
                case OTPDeliveryMethod.SMS:
                    return await SendOTPViaSMSAsync(recipient, otpCode, userName);
                
                case OTPDeliveryMethod.Email:
                    return await SendOTPViaEmailAsync(recipient, otpCode, userName);
                
                default:
                    _logger.LogWarning("Unsupported delivery method: {DeliveryMethod}", deliveryMethod);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP via {DeliveryMethod} to {Recipient}", deliveryMethod, recipient);
            return false;
        }
    }

    public async Task<bool> SendWelcomeMessageAsync(OTPDeliveryMethod deliveryMethod, string recipient, string userName)
    {
        try
        {
            switch (deliveryMethod)
            {
                case OTPDeliveryMethod.SMS:
                    return await SendWelcomeViaSMSAsync(recipient, userName);
                
                case OTPDeliveryMethod.Email:
                    return await SendWelcomeViaEmailAsync(recipient, userName);
                
                default:
                    _logger.LogWarning("Unsupported delivery method: {DeliveryMethod}", deliveryMethod);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome message via {DeliveryMethod} to {Recipient}", deliveryMethod, recipient);
            return false;
        }
    }

    public async Task<bool> SendSecurityAlertAsync(OTPDeliveryMethod deliveryMethod, string recipient, string userName, string alertType)
    {
        try
        {
            switch (deliveryMethod)
            {
                case OTPDeliveryMethod.SMS:
                    return await SendSecurityAlertViaSMSAsync(recipient, userName, alertType);
                
                case OTPDeliveryMethod.Email:
                    return await SendSecurityAlertViaEmailAsync(recipient, userName, alertType);
                
                default:
                    _logger.LogWarning("Unsupported delivery method: {DeliveryMethod}", deliveryMethod);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security alert via {DeliveryMethod} to {Recipient}", deliveryMethod, recipient);
            return false;
        }
    }

    private async Task<bool> SendOTPViaSMSAsync(string phoneNumber, string otpCode, string userName)
    {
        try
        {
            var accountSid = _configuration["Notification:SMS:AccountSid"];
            var authToken = _configuration["Notification:SMS:AuthToken"];
            var fromNumber = _configuration["Notification:SMS:FromNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
            {
                _logger.LogWarning("Twilio configuration is incomplete");
                return false;
            }

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: $"Hi {userName}, your AccessOTP code is: {otpCode}. Valid for 5 minutes. Do not share this code.",
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(phoneNumber)
            );

            _logger.LogInformation("SMS OTP sent successfully to {PhoneNumber}, SID: {MessageSid}", phoneNumber, message.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS OTP to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    private async Task<bool> SendOTPViaEmailAsync(string email, string otpCode, string userName)
    {
        try
        {
            if (_sendGridClient == null)
            {
                _logger.LogWarning("SendGrid client not initialized");
                return false;
            }

            var fromEmail = _configuration["Notification:Email:FromEmail"];
            var fromName = _configuration["Notification:Email:FromName"];

            if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
            {
                _logger.LogWarning("SendGrid configuration is incomplete");
                return false;
            }

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email, userName);
            var subject = "Your AccessOTP Code";
            
            var htmlContent = GenerateOTPEmailHtml(userName, otpCode);
            var plainTextContent = GenerateOTPEmailPlainText(userName, otpCode);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email OTP sent successfully to {Email}", email);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send email OTP to {Email}. Status: {StatusCode}", email, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email OTP to {Email}", email);
            return false;
        }
    }

    private async Task<bool> SendWelcomeViaSMSAsync(string phoneNumber, string userName)
    {
        try
        {
            var accountSid = _configuration["Notification:SMS:AccountSid"];
            var authToken = _configuration["Notification:SMS:AuthToken"];
            var fromNumber = _configuration["Notification:SMS:FromNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
            {
                return false;
            }

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: $"Welcome to AccessOTP, {userName}! Your account has been successfully created. You can now log in using OTP authentication.",
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(phoneNumber)
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    private async Task<bool> SendWelcomeViaEmailAsync(string email, string userName)
    {
        try
        {
            if (_sendGridClient == null) return false;

            var fromEmail = _configuration["Notification:Email:FromEmail"];
            var fromName = _configuration["Notification:Email:FromName"];

            if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
            {
                return false;
            }

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email, userName);
            var subject = "Welcome to AccessOTP!";
            
            var htmlContent = GenerateWelcomeEmailHtml(userName);
            var plainTextContent = GenerateWelcomeEmailPlainText(userName);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await _sendGridClient.SendEmailAsync(msg);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to {Email}", email);
            return false;
        }
    }

    private async Task<bool> SendSecurityAlertViaSMSAsync(string phoneNumber, string userName, string alertType)
    {
        try
        {
            var accountSid = _configuration["Notification:SMS:AccountSid"];
            var authToken = _configuration["Notification:SMS:AuthToken"];
            var fromNumber = _configuration["Notification:SMS:FromNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
            {
                return false;
            }

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: $"Security Alert: {alertType} detected for your AccessOTP account, {userName}. If this wasn't you, please contact support immediately.",
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(phoneNumber)
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security alert SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    private async Task<bool> SendSecurityAlertViaEmailAsync(string email, string userName, string alertType)
    {
        try
        {
            if (_sendGridClient == null) return false;

            var fromEmail = _configuration["Notification:Email:FromEmail"];
            var fromName = _configuration["Notification:Email:FromName"];

            if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
            {
                return false;
            }

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email, userName);
            var subject = $"Security Alert - {alertType}";
            
            var htmlContent = GenerateSecurityAlertEmailHtml(userName, alertType);
            var plainTextContent = GenerateSecurityAlertEmailPlainText(userName, alertType);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await _sendGridClient.SendEmailAsync(msg);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security alert email to {Email}", email);
            return false;
        }
    }

    private string GenerateOTPEmailHtml(string userName, string otpCode)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Your AccessOTP Code</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .otp-code {{ font-size: 32px; font-weight: bold; text-align: center; color: #007bff; margin: 20px 0; padding: 20px; background-color: #f8f9fa; border-radius: 5px; letter-spacing: 5px; }}
                    .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🔐 AccessOTP</h1>
                    </div>
                    <p>Hi {userName},</p>
                    <p>Here's your one-time password to access your account:</p>
                    <div class='otp-code'>{otpCode}</div>
                    <p><strong>This code is valid for 5 minutes.</strong></p>
                    <p>If you didn't request this code, please ignore this email or contact support if you have concerns.</p>
                    <div class='footer'>
                        <p>© 2024 AccessOTP. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateOTPEmailPlainText(string userName, string otpCode)
    {
        return $"Hi {userName},\n\nHere's your one-time password to access your account:\n\n{otpCode}\n\nThis code is valid for 5 minutes.\n\nIf you didn't request this code, please ignore this email or contact support if you have concerns.\n\n© 2024 AccessOTP. All rights reserved.";
    }

    private string GenerateWelcomeEmailHtml(string userName)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Welcome to AccessOTP!</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .welcome {{ text-align: center; margin: 20px 0; }}
                    .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🎉 Welcome to AccessOTP!</h1>
                    </div>
                    <div class='welcome'>
                        <h2>Hi {userName}!</h2>
                        <p>Your AccessOTP account has been successfully created.</p>
                        <p>You can now log in using secure, passwordless OTP authentication.</p>
                    </div>
                    <p>Thank you for choosing AccessOTP for secure authentication!</p>
                    <div class='footer'>
                        <p>© 2024 AccessOTP. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateWelcomeEmailPlainText(string userName)
    {
        return $"Hi {userName}!\n\nYour AccessOTP account has been successfully created.\n\nYou can now log in using secure, passwordless OTP authentication.\n\nThank you for choosing AccessOTP for secure authentication!\n\n© 2024 AccessOTP. All rights reserved.";
    }

    private string GenerateSecurityAlertEmailHtml(string userName, string alertType)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Security Alert - {alertType}</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .alert {{ background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 20px; margin: 20px 0; }}
                    .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🚨 Security Alert</h1>
                    </div>
                    <div class='alert'>
                        <h2>Hi {userName},</h2>
                        <p><strong>Security Alert: {alertType}</strong> has been detected for your AccessOTP account.</p>
                        <p>If this activity wasn't initiated by you, please contact our support team immediately.</p>
                    </div>
                    <p>Your security is our top priority.</p>
                    <div class='footer'>
                        <p>© 2024 AccessOTP. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateSecurityAlertEmailPlainText(string userName, string alertType)
    {
        return $"Hi {userName},\n\nSecurity Alert: {alertType} has been detected for your AccessOTP account.\n\nIf this activity wasn't initiated by you, please contact our support team immediately.\n\nYour security is our top priority.\n\n© 2024 AccessOTP. All rights reserved.";
    }
}

