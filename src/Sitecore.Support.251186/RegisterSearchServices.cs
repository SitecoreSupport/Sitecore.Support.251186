namespace Sitecore.Support.XA.Foundation.Search.Pipelines.IoC
{
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore.DependencyInjection;
  using Sitecore.XA.Foundation.Search.Interfaces;
  using Sitecore.XA.Foundation.Search.Services;
  using Sitecore.XA.Foundation.Search.Wrappers;

  public class RegisterSearchServices : IServicesConfigurator
  {
    public void Configure(IServiceCollection serviceCollection)
    {
      serviceCollection.AddSingleton<IFacetService, Sitecore.Support.XA.Foundation.Search.Services.FacetService>();
    }
  }
}