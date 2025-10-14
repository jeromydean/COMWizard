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
      Random random = new Random(Guid.NewGuid().GetHashCode());
      foreach (string path in paths)
      {
        RegistrationResultMessage? registrationResult = null;
        try
        {
          await Task.Delay(random.Next(1000, 1750), cancellationToken);
          registrationResult = new RegistrationResultMessage();
        }
        catch (TaskCanceledException)
        {
          yield break;
        }

        if (registrationResult != null)
        {
          yield return registrationResult;
        }
      }
    }
  }
}