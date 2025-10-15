using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using COMWizard.Common.Messaging;
using COMWizard.Common.PortableExecutable;

namespace COMWizard.Engine
{
  public class RegistrationEngine : IRegistrationEngine
  {
    private readonly IPEParsingService _peParsingService;

    public RegistrationEngine(IPEParsingService peParsingService)
    {
      _peParsingService = peParsingService;
    }

    public async IAsyncEnumerable<RegistrationResultMessage> Register(IEnumerable<string> paths,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      //figure out what kind of extractors we will need
      List<PEMetadata> supportedFiles = new List<PEMetadata>();

      foreach (PEMetadata peMetadata in paths.Select(p => _peParsingService.Parse(p)))
      {
        //we only support x86 unmanaged COM libraries at the moment
        if (peMetadata.IsPortableExecutable
          && peMetadata.Architecture == Machine.I386//&& (peMetadata.Architecture == Machine.I386 || peMetadata.Architecture == Machine.Amd64)
          && peMetadata.IsCOM
          && peMetadata.IsLibrary
          && !peMetadata.IsAssembly)
        {
          supportedFiles.Add(peMetadata);
        }
        else
        {
          //TODO add some more information about why this file isn't supported
          yield return new RegistrationFailureResultMessage
          {
            Name = Path.GetFileName(peMetadata.Path),
            Path = peMetadata.Path,
            Exception = $"Attempt to register an unsupported file '{peMetadata.Path}'."
          };
        }
      }

      if (supportedFiles.Any())
      {
        await using (RegistrationManager registrationManager = new RegistrationManager())
        {
          await registrationManager.ConnectAsync(cancellationToken).ConfigureAwait(false);

          await foreach (RegistrationResultMessage registrationResultMessage in registrationManager.Register(supportedFiles, cancellationToken))
          {
            yield return registrationResultMessage;
          }
        }
      }
    }
  }
}