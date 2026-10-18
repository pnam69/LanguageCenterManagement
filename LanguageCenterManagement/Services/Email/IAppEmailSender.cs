namespace LanguageCenterManagement.Services.Email
{
    public interface IAppEmailSender
    {
        Task SendAsync(
            string email,
            string subject,
            string htmlMessage);
    }
}
