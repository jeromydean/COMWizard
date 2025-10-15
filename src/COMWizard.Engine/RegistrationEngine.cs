using System.IO.Pipes;
using System.Runtime.CompilerServices;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Enums;
using COMWizard.Common.Messaging.Extensions;

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