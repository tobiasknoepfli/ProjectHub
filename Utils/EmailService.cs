using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sleipnir.App.Utils
{
    public class EmailService
    {
        private const string ResendApiKey = "re_8aNgu2hC_DMwDZUXS9THWzDj6dkH7ehmN"; 
        private readonly HttpClient _httpClient;

        public EmailService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ResendApiKey);
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
        {
            try
            {
                var payload = new
                {
                    from = "Sleipnir <onboarding@resend.dev>",
                    to = new[] { toEmail },
                    subject = "Sleipnir Verification Code",
                    html = $@"
                        <div style='font-family: sans-serif; padding: 20px; background-color: #0E1638; color: white; border-radius: 10px;'>
                            <h2 style='color: #7C4DFF;'>Verify your Email</h2>
                            <p>You requested to unlock or modify your Sleipnir profile.</p>
                            <div style='background: #1A1D2E; padding: 15px; border-radius: 5px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #7C4DFF; border: 1px solid #7C4DFF;'>
                                {otpCode}
                            </div>
                            <p style='margin-top: 20px; font-size: 12px; color: #8B949E;'>If you didn't request this, you can safely ignore this email.</p>
                        </div>"
                };

                var response = await _httpClient.PostAsJsonAsync("https://api.resend.com/emails", payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }
    }
}
