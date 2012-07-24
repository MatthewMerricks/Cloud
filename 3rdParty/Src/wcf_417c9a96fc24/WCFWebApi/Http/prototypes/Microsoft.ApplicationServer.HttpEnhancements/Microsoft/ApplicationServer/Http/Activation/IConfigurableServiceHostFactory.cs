using Microsoft.ApplicationServer.Http.Description;

namespace Microsoft.ApplicationServer.Http.Activation
{
    public interface IConfigurableServiceHostFactory
    {
        IHttpHostConfigurationBuilder Builder { get; set; }
    }
}