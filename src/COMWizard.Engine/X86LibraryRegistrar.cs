using System.IO.Pipes;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Extensions;
using COMWizard.Engine.Parsing;

namespace COMWizard.Engine
{
  internal class X86LibraryRegistrar : IRegistrar
  {
    private readonly RegistrationManager _registrationManager;
    private readonly IEnumerable<PEMetadata> _work;

    public X86LibraryRegistrar(RegistrationManager registrationManager,
      IEnumerable<PEMetadata> work)
    {
      _registrationManager = registrationManager;
      _work = work;
    }

    public async IAsyncEnumerable<RegistrationResultMessage> Register(CancellationToken cancellationToken = default)
    {
      string pipeName = $"comwizard.registrar-{Guid.NewGuid().ToString("N")}";
      using (NamedPipeServerStream registrarPipeServerStream = new NamedPipeServerStream(pipeName,
        PipeDirection.InOut,
        1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous))
      {
        StartRegistrarResultMessage result = await _registrationManager.CreateRegistrar(pipeName,
          @"C:\Users\user\source\repos\COMWizard\src\COMWizard.LibraryExtractor\bin\Debug\net8.0\COMWizard.LibraryExtractor.exe",
          cancellationToken);

        await registrarPipeServerStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

        foreach(PEMetadata work in _work)
        {
          await registrarPipeServerStream.WriteMessageAsync(new RegistrationRequestMessage
          {
            Path = work.Path,
            SHA256 = work.SHA256
          }, cancellationToken).ConfigureAwait(false);

          MessageBase responseMessage = await registrarPipeServerStream.ReadMessageAsync(cancellationToken);
          if (responseMessage is RegistrationResultMessage registrationResult)
          {
            yield return registrationResult;
          }
        }

        await registrarPipeServerStream.WriteMessageAsync(new TerminateMessage(), cancellationToken).ConfigureAwait(false);
      }

      yield break;
    }
  }
}