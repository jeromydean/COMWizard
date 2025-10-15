using System.Runtime.CompilerServices;
using COMWizard.Common.Messaging;

namespace COMWizard.Engine
{
  public class RegistrationEngine : IRegistrationEngine
  {
    public async IAsyncEnumerable<RegistrationResultMessage> Register(IEnumerable<string> paths,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      await foreach (RegistrationResultMessage registrationResultMessage in RegisterCore(paths, cancellationToken))
      {
        yield return registrationResultMessage;
      }
    }

    private async IAsyncEnumerable<RegistrationResultMessage> RegisterCore(IEnumerable<string> paths,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      await using (ProcessLauncher processLauncher = new ProcessLauncher())
      {
        await processLauncher.ConnectAsync(cancellationToken).ConfigureAwait(false);

        //TODO iterate over the paths and have the launcher create the correct helper process to extract the registration entries

        await Task.Delay(10000);

        yield break;
      }
    }
  }
}