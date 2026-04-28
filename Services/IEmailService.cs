namespace MemmoApi.Services
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string toEmail, string toName, string verificationLink);
    }
}
