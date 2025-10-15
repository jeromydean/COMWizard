using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using COMWizard.Common.Messaging;
using COMWizard.Common.Messaging.Extensions;

namespace COMWizard.Launcher
{
  internal class Program
  {
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    static async Task Main(string[] args)
    {
      if (!(args.Length != 2
        || !string.Equals(args[0], "--pipe", StringComparison.OrdinalIgnoreCase)
        || !args[1].StartsWith("comwizard.processlauncher-")))
      {
        if (!IsProcessElevated())
        {
          string? processPath = Environment.ProcessPath;
          ProcessStartInfo processStartInfo = new ProcessStartInfo
          {
            FileName = processPath,
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
          };

          foreach (string argument in args)
          {
            processStartInfo.ArgumentList.Add(argument);
          }

          try
          {
            using (Process elevatedProcess = Process.Start(processStartInfo)) { }
            return;
          }
          catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)//ERROR_CANCELLED -- user cancelled the elevation
          {
            Environment.Exit(1223);
          }
          catch (Exception ex)
          {
            Environment.Exit(1);
          }
        }

        //hide the console window to dissuade user from closing us early
        IntPtr consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
          ShowWindow(consoleWindow, SW_HIDE);
        }

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

    private static bool IsProcessElevated()
    {
      using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
      {
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
      }
    }
  }
}