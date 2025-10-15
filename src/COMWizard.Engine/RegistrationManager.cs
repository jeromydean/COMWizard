using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Enums;
using COMWizard.Common.Messaging.Extensions;
using COMWizard.Common.PortableExecutable;
using COMWizard.Engine.Extensions;

namespace COMWizard.Engine
{
  internal class RegistrationManager : IAsyncDisposable, IDisposable
  {
    private bool _disposed;

    private string _pipeName = null;
    private NamedPipeServerStream _serverStream;
    private Process _launcherProcess;
    private readonly SemaphoreSlim _disposeLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _registrarCreationLock = new SemaphoreSlim(1, 1);

    //public NamedPipeServerStream ServerStream
    //{
    //  get => _serverStream;
    //}

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
      _pipeName = $"comwizard.processlauncher-{Guid.NewGuid().ToString("N")}";

      _serverStream = new NamedPipeServerStream(_pipeName,
        PipeDirection.InOut,
        1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous);

      ProcessStartInfo launcherStartInfo = new ProcessStartInfo
      {
        FileName = @"C:\Users\user\source\repos\COMWizard\src\COMWizard.Launcher\bin\Debug\net8.0\COMWizard.Launcher.exe",
        ArgumentList = { "--pipe", _pipeName },
        UseShellExecute = false,
        CreateNoWindow = true
      };

      _launcherProcess = Process.Start(launcherStartInfo);
      if (_launcherProcess == null
        || (_launcherProcess.HasExited && _launcherProcess.ExitCode != 0))
      {
        //if _launcherProcess.ExitCode == 1223 the user cancelled the required elevation
        await _serverStream.DisposeAsync().ConfigureAwait(false);
        _serverStream = null;
        throw new InvalidOperationException("Failed to start launcher process.");
      }

      try
      {
        await _serverStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
      }
      catch
      {
        await CleanupResourcesAsync().ConfigureAwait(false);
        throw;
      }
    }

    internal async Task<StartRegistrarResultMessage> CreateRegistrar(string pipeName,
      string processName,
      CancellationToken cancellationToken = default)
    {
      await _registrarCreationLock.WaitAsync().ConfigureAwait(false);
      try
      {
        await _serverStream.WriteMessageAsync(new StartRegistrarRequestMessage
        {
          ExtractorType = ExtractorType.Library,
          PipeName = pipeName
        }, cancellationToken).ConfigureAwait(false);

        MessageBase registrarStartResponse = await _serverStream.ReadMessageAsync(cancellationToken);
        if (registrarStartResponse is not StartRegistrarResultMessage serm)
        {
          throw new InvalidDataException("Expected registrar startup response");
        }

        return serm;
      }
      finally
      {
        _registrarCreationLock.Release();
      }
    }

    public async IAsyncEnumerable<RegistrationResultMessage> Register(IEnumerable<PEMetadata> files,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      //TODO once we have multiple registrars implemented this will change based upon the input to go to proper registrar
      int chunkSize = (int)Math.Ceiling(files.Count() / 2d);

//#if (DEBUG)
//      chunkSize = files.Count();
//#endif

      IEnumerable<IEnumerable<PEMetadata>> chunks = files.Chunk(chunkSize);

      IEnumerable<IRegistrar> registrars = chunks.Select(c => new X86LibraryRegistrar(this, c));

      await foreach (RegistrationResultMessage registrationResult in registrars.Select(r => r.Register(cancellationToken)).Interleave(cancellationToken))
      {
        yield return registrationResult;
      }
    }

    public async Task CloseAsync()
    {
      ObjectDisposedException.ThrowIf(_disposed, this);

      if (_serverStream != null && _serverStream.IsConnected)
      {
        try
        {
          await _serverStream.WriteMessageAsync(new TerminateMessage()).ConfigureAwait(false);
          await Task.Delay(100).ConfigureAwait(false);
        }
        catch{}
      }
    }

    private async Task CleanupResourcesAsync()
    {
      if (_serverStream != null)
      {
        try
        {
          if (_serverStream.IsConnected)
          {
            _serverStream.Disconnect();
          }
        }
        catch { }

        await _serverStream.DisposeAsync().ConfigureAwait(false);
        _serverStream = null;
      }

      if (_launcherProcess != null)
      {
        try
        {
          if (!_launcherProcess.HasExited)
          {
            _launcherProcess.Kill();
            await Task.Run(() => _launcherProcess.WaitForExit(1000)).ConfigureAwait(false);
          }
          _launcherProcess.Dispose();
        }
        catch { }
        finally
        {
          _launcherProcess = null;
        }
      }
    }

    public async ValueTask DisposeAsync()
    {
      if (_disposed)
      {
        return;
      }

      await _disposeLock.WaitAsync().ConfigureAwait(false);
      try
      {
        if (_disposed)
        {
          return;
        }

        try
        {
          await CloseAsync().ConfigureAwait(false);
        }
        catch { }

        await CleanupResourcesAsync().ConfigureAwait(false);

        _disposeLock.Dispose();
        _disposed = true;
      }
      finally
      {
        if (!_disposed)
        {
          _disposeLock.Release();
        }
      }

      GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
      if (_disposed)
      {
        return;
      }

      _disposeLock.Wait();
      try
      {
        if (_disposed)
        {
          return;
        }

        try
        {
          if (_serverStream != null && _serverStream.IsConnected)
          {
            _ = _serverStream.WriteMessageAsync(new TerminateMessage());
            Thread.Sleep(100);
          }
        }
        catch { }

        _serverStream?.Dispose();
        _serverStream = null;

        if (_launcherProcess != null)
        {
          try
          {
            if (!_launcherProcess.HasExited)
            {
              _launcherProcess.Kill();
              _launcherProcess.WaitForExit(1000);
            }
            _launcherProcess.Dispose();
          }
          catch { }
          finally
          {
            _launcherProcess = null;
          }
        }

        _disposeLock.Dispose();
        _disposed = true;
      }
      finally
      {
        if (!_disposed)
        {
          _disposeLock.Release();
        }
      }

      GC.SuppressFinalize(this);
    }

    ~RegistrationManager()
    {
      Dispose();
    }
  }
}