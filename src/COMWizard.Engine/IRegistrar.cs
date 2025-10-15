using COMWizard.Common.Messaging;

namespace COMWizard.Engine
{
  internal interface IRegistrar
  {
    IAsyncEnumerable<RegistrationResultMessage> Register(CancellationToken cancellationToken = default);
  }
}