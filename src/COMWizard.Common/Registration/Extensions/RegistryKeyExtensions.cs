using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace COMWizard.Common.Registration.Extensions
{
  public static class RegistryKeyExtensions
  {
    private const int SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_BACKUP_NAME = "SeBackupPrivilege";

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct LUID
    {
      public uint LowPart;
      public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TOKEN_PRIVILEGES
    {
      public int PrivilegeCount;
      public LUID Luid;
      public int Attributes;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
    private const uint TOKEN_QUERY = 0x8;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegSaveKey(Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
      string lpFile,
      IntPtr lpSecurityAttributes);

    public static void SaveKey(this RegistryKey key, string path)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      File.Delete(path);//if the file exists RegSaveKey will throw an exception

      //must have SE_BACKUP_NAME privilege
      if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
      {
        throw new Win32Exception(Marshal.GetLastWin32Error());
      }

      if (!LookupPrivilegeValue(null, SE_BACKUP_NAME, out LUID luid))
      {
        throw new Win32Exception(Marshal.GetLastWin32Error());
      }

      TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
      {
        PrivilegeCount = 1,
        Luid = luid,
        Attributes = SE_PRIVILEGE_ENABLED
      };

      if (!AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
      {
        throw new Win32Exception(Marshal.GetLastWin32Error());
      }

      int result = RegSaveKey(key.Handle, path, IntPtr.Zero);
      if (result != 0)
      {
        throw new Win32Exception(result);
      }
    }

    public static RegistryKey? Open(this string path, bool writable = false)
    {
      string[] pathParts = path.Split(new[] { '\\' }, 2);
      string hiveName = pathParts.First();
      RegistryKey hive = hiveName switch
      {
        "HKEY_CLASSES_ROOT" => Microsoft.Win32.Registry.ClassesRoot,
        "HKEY_CURRENT_USER" => Microsoft.Win32.Registry.CurrentUser,
        "HKEY_LOCAL_MACHINE" => Microsoft.Win32.Registry.LocalMachine,
        "HKEY_USERS" => Microsoft.Win32.Registry.Users,
        "HKEY_CURRENT_CONFIG" => Microsoft.Win32.Registry.CurrentConfig,
        _ => throw new ArgumentException($"Unknown registry hive: {hiveName}", nameof(path)),
      };

      return pathParts.Length > 1
        ? hive.OpenSubKey(pathParts[1], writable)
        : hive;
    }
  }
}