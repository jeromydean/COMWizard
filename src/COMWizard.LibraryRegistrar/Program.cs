using System.IO.Pipes;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Extensions;
using COMWizard.Common.Registration;
using COMWizard.Common.Registration.Extensions;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace COMWizard.LibraryRegistrar
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      if (!(args.Length != 2
        || !string.Equals(args[0], "--pipe", StringComparison.OrdinalIgnoreCase)
        || !args[1].StartsWith("comwizard.registrar-")))
      {
        JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
          Formatting = Formatting.Indented,
          Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

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
                try
                {
                  string proxiedRegistryKeyName = null;
                  using (RegistryProxy registryProxy = new())
                  {
                    proxiedRegistryKeyName = registryProxy.Path;

                    registrationService.Register(registrationRequest.Path);
                  }

                  string responseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "COMWizard",
                    registrationRequest.SHA256);

                  using (RegistryKey registrationRootKey = RegistryKeyExtensions.Open(proxiedRegistryKeyName))
                  {
                    registrationRootKey.SaveKey(Path.Combine(responseDirectory,
                      "registration.hive"));
                  }

                  await File.WriteAllTextAsync(Path.Combine(responseDirectory, "fileinfo.json"), JsonConvert.SerializeObject(registrationRequest.FileInformation, jsonSerializerSettings));

                  await pipeClientStream.WriteMessageAsync(new RegistrationSuccessResultMessage
                  {
                    FileInformation = registrationRequest.FileInformation,
                    OutputPath = responseDirectory,
                    Name = Path.GetFileName(registrationRequest.Path)
                  }, cts.Token).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                  await pipeClientStream.WriteMessageAsync(new RegistrationFailureResultMessage
                  {
                    Name = Path.GetFileName(registrationRequest.Path),
                    Path = registrationRequest.Path,
                    Exception = $"An exception occurred while attempting to register file.  {ex}"
                  }, cts.Token).ConfigureAwait(false);
                }
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