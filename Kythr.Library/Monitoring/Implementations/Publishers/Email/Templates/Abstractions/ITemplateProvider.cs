namespace Kythr.Library.Monitoring.Implementations.Publishers.Email.Templates.Abstractions
{
    public interface ITemplateProvider
    {
        string GetTemplate(string name);
    }
}
