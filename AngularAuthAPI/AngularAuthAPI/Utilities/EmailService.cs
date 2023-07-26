using AngularAuthAPI.Models;
using MailKit.Net.Smtp;
using MimeKit;

namespace AngularAuthAPI.Utilities
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void SendEmail(EmailModel emailModel)
        {
            var emailMessage = new MimeMessage();
            var from = _configuration["EmalSetting:From"];
            emailMessage.From.Add(new MailboxAddress("Reset password", from));
            emailMessage.To.Add(new MailboxAddress(emailModel.To, emailModel.To));
            emailMessage.Subject = emailModel.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = string.Format(emailModel.Content)
            };

            using(var client = new SmtpClient())
            {
                try
                {
                    client.Connect(_configuration["EmailSetting:SmtpServer"], 465, true);
                    client.Authenticate(_configuration["EmailSetting:From"], _configuration["EmailSetting:Password"]);
                    client.Send(emailMessage);
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    client.Disconnect(true);
                    client.Dispose();
                }
            }
        }
    }
}
