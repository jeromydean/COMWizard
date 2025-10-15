using COMWizard.Engine.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace COMWizard.Engine.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection ConfigureEngineServices(this IServiceCollection services)
    {
      services.AddSingleton<IPEParsingService, PEParsingService>();
      services.AddSingleton<IRegistrationEngine, RegistrationEngine>();

      return services;
    }
  }
}