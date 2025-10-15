using System.IO.Pipes;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Enums;
using COMWizard.Common.Messaging.Extensions;
using COMWizard.Engine.Parsing;

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
      List<PEMetadata> i386COMLibraries = new List<PEMetadata>();

      foreach (PEMetadata peMetadata in paths.Select(p => _peParsingService.Parse(p)))
      {
        //we only support x86 unmanaged COM libraries at the moment
        if (peMetadata.IsPortableExecutable
          && peMetadata.Architecture == Machine.I386//&& (peMetadata.Architecture == Machine.I386 || peMetadata.Architecture == Machine.Amd64)
          && peMetadata.IsCOM
          && peMetadata.IsLibrary
          && !peMetadata.IsAssembly)
        {
          i386COMLibraries.Add(peMetadata);
        }
        else
        {
          //TODO add some more information about why this file isn't supported
          yield return new RegistrationResultMessage
          {
            Path = peMetadata.Path
          };
        }
      }

      await foreach (RegistrationResultMessage registrationResultMessage in RegisterCore(i386COMLibraries.Select(l => l.Path), cancellationToken))
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

        string extractorPipeName = $"comwizard.extractor-{Guid.NewGuid().ToString("N")}";
        using (NamedPipeServerStream extractorPipeServerStream = new NamedPipeServerStream(extractorPipeName,
          PipeDirection.InOut,
          1,
          PipeTransmissionMode.Byte,
          PipeOptions.Asynchronous))
        {
          await processLauncher.ServerStream.WriteMessageAsync(new StartExtractorRequestMessage
          {
            Type = MessageType.StartExtractorRequest,
            ExtractorType = ExtractorType.Library,
            PipeName = extractorPipeName
          }, cancellationToken).ConfigureAwait(false);

          MessageBase extractorStartResponse = await processLauncher.ServerStream.ReadMessageAsync(cancellationToken);
          if (extractorStartResponse is not StartExtractorResultMessage)
          {
            throw new InvalidDataException("Expected extractor startup response");
          }

          await extractorPipeServerStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

          //now send the registration requests to the extractor with RegistrationRequestMessage
          //TODO iterate over the paths and have the launcher create the correct helper process to extract the registration entries
        }

        await Task.Delay(10000, cancellationToken);

        yield break;
      }
    }
  }
}