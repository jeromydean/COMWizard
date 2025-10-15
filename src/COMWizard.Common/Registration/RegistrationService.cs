using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace COMWizard.Common.Registration
{
  public class RegistrationService : IRegistrationService
  {
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int DllRegisterServerDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int DllUnregisterServerDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private delegate int DllInstallDelegate([MarshalAs(UnmanagedType.Bool)] bool install, string cmdLine);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryW(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]

    private static extern bool FreeLibrary(IntPtr hModule);

    //[DllImport("kernel32.dll", EntryPoint = "GetProcAddressW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
    //private static extern IntPtr GetProcAddressW(IntPtr hModule, string procName);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    public void Register(string path)
    {
      using (ScopedNativeLibrary lib = LoadLibraryScoped(path))
      {
        DllRegisterServerDelegate proc = GetExport<DllRegisterServerDelegate>(lib, "DllRegisterServer");
        int hr = proc();
        ThrowIfFailed(hr);
      }
    }

    public static void Unregister(string dllPath)
    {
      using (ScopedNativeLibrary lib = LoadLibraryScoped(dllPath))
      {
        DllUnregisterServerDelegate proc = GetExport<DllUnregisterServerDelegate>(lib, "DllUnregisterServer");
        int hr = proc();
        ThrowIfFailed(hr);
      }
    }

    private sealed class ScopedNativeLibrary : IDisposable
    {
      public IntPtr Handle { get; private set; }

      public ScopedNativeLibrary(IntPtr handle)
      {
        Handle = handle;
      }

      public void Dispose()
      {
        if (Handle != IntPtr.Zero)
        {
          Free(Handle);
          Handle = IntPtr.Zero;
        }
      }
    }

    private static void ThrowIfFailed(int hr)
    {
      if (hr < 0)
      {
        Marshal.ThrowExceptionForHR(hr);
      }
    }

    private static T GetExport<T>(ScopedNativeLibrary lib, string name) where T : Delegate
    {
      IntPtr addr = GetSymbolAddress(lib.Handle, name);
      if (addr == IntPtr.Zero)
      {
        throw new Win32Exception(Marshal.GetLastWin32Error());
      }

      T del = Marshal.GetDelegateForFunctionPointer<T>(addr);
      return del;
    }

    private static IntPtr GetSymbolAddress(IntPtr handle, string symbol)
    {
      IntPtr p = GetProcAddress(handle, symbol);
      return p;
    }

    private static void Free(IntPtr hm)
    {
      //.net core 3.0+ only
      //try
      //{
      //  NativeLibrary.Free(hm);
      //  return;
      //}
      //catch { }

      bool ok = FreeLibrary(hm);
      if (!ok) { } //log?
    }

    private static ScopedNativeLibrary LoadLibraryScoped(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        throw new ArgumentNullException(nameof(path));
      }

      string fullPath = Path.GetFullPath(path);

      IntPtr handle;
      //.net core 3.0+ only
      //bool loaded = NativeLibrary.TryLoad(fullPath, out handle);
      //if (loaded)
      //{
      //  return new ScopedNativeLibrary(handle);
      //}

      IntPtr h = LoadLibraryW(fullPath);
      if (h == IntPtr.Zero)
      {
        throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadLibrary failed: " + fullPath);
      }

      return new ScopedNativeLibrary(h);
    }
  }
}