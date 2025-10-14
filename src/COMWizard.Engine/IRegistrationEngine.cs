using COMWizard.Common.Messaging;

namespace COMWizard.Engine
{
  public interface IRegistrationEngine
  {
    IAsyncEnumerable<RegistrationResultMessage> Register(IEnumerable<string> paths,
      CancellationToken cancellationToken = default);
  }
}
