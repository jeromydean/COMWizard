using System.Diagnostics;
using System.IO.Pipes;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Extensions;
using COMWizard.Common.Registration;
using COMWizard.Common.Registration.Extensions;
using Microsoft.Win32;

namespace COMWizard.LibraryExtractor
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      if (!(args.Length != 2
        || !string.Equals(args[0], "--pipe", StringComparison.OrdinalIgnoreCase)
        || !args[1].StartsWith("comwizard.registrar-")))
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

            IRegistrationService registrationService = new RegistrationService();

            MessageBase? message;
            while ((message = await pipeClientStream.ReadMessageAsync(cts.Token)) != null)
            {
              if (message is RegistrationRequestMessage registrationRequest)
              {
                //Debugger.Launch();

                string proxiedRegistryKeyName = null;
                using (RegistryProxy registryProxy = new())
                {
                  proxiedRegistryKeyName = registryProxy.Path;

                  registrationService.Register(registrationRequest.Path);
                }

                using (RegistryKey registrationRootKey = RegistryKeyExtensions.Open(proxiedRegistryKeyName))
                {
                  registrationRootKey.SaveKey(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "COMWizard",
                    registrationRequest.SHA256,
                    "registration.hive"));
                }

                await pipeClientStream.WriteMessageAsync(new RegistrationResultMessage
                {
                  Path = registrationRequest.Path,
                }, cts.Token).ConfigureAwait(false);
              }
              else if (message is TerminateMessage)
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