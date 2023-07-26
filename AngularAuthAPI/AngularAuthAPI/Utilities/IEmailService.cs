using AngularAuthAPI.Models;

namespace AngularAuthAPI.Utilities
{
    public interface IEmailService
    {
        void SendEmail(EmailModel emailModel);
    }
}
