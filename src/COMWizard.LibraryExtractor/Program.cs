using System.IO.Pipes;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Extensions;

namespace COMWizard.LibraryExtractor
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      if (!(args.Length != 2
        || !string.Equals(args[0], "--pipe", StringComparison.OrdinalIgnoreCase)
        || !args[1].StartsWith("comwizard.extractor-")))
      {
        using (CancellationTokenSource cts = new CancellationTokenSource())
        {
          Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

          using (NamedPipeClientStream pipeClientStream = new NamedPipeClientStream(".",
            args[1].Trim(),
            PipeDirection.InOut,
            PipeOptions.Asynchronous))
          {
            await pipeClientStream.ConnectAsync(cts.Token).ConfigureAwait(false);

            MessageBase? message;
            while ((message = await pipeClientStream.ReadMessageAsync(cts.Token)) != null)
            {
              if (message is TerminateMessage)
              {
                break;
              }
            }
          }
        }
      }
    }
  }
}