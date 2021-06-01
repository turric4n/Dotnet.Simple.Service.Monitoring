namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions
{
    public interface IMailMessageFactory
    {
        IMailMessage Create(string email, string subject, string body);
    }
}