namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions
{
    public interface IMailSenderClient
    {
        void SendMessage(IMailMessage msg);
    }
}