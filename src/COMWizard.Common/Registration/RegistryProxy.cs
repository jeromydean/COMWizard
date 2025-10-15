using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace COMWizard.Common.Registration
{
  public class RegistryProxy : IDisposable
  {
    #region native
    private const string Advapi32 = "advapi32.dll";

    private static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
    private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
    private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
    private static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
    private static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));

    //[Flags]
    //private enum RegSam : int
    //{
    //  KEY_QUERY_VALUE = 0x0001,
    //  KEY_SET_VALUE = 0x0002,
    //  KEY_CREATE_SUB_KEY = 0x0004,
    //  KEY_ENUMERATE_SUB_KEYS = 0x0008,
    //  KEY_NOTIFY = 0x0010,
    //  KEY_CREATE_LINK = 0x0020,
    //  KEY_WOW64_64KEY = 0x0100,
    //  KEY_WOW64_32KEY = 0x0200,
    //  KEY_READ = (STANDARD_RIGHTS_READ | KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS | KEY_NOTIFY),
    //  KEY_WRITE = (STANDARD_RIGHTS_WRITE | KEY_SET_VALUE | KEY_CREATE_SUB_KEY),
    //  KEY_ALL_ACCESS = (STANDARD_RIGHTS_ALL | KEY_QUERY_VALUE | KEY_SET_VALUE | KEY_CREATE_SUB_KEY | KEY_ENUMERATE_SUB_KEYS | KEY_NOTIFY | KEY_CREATE_LINK),
    //}

    private const int ERROR_SUCCESS = 0;

    private const int STANDARD_RIGHTS_READ = 0x00020000;
    private const int STANDARD_RIGHTS_WRITE = 0x00020000;
    private const int STANDARD_RIGHTS_ALL = 0x001F0000;

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegOverridePredefKey(
        IntPtr hKey,
        IntPtr hNewHKey);

    //[DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    //private static extern int RegOpenKeyEx(
    //    IntPtr hKey,
    //    string lpSubKey,
    //    int ulOptions,
    //    RegSam samDesired,
    //    out IntPtr phkResult);

    //[DllImport(Advapi32, SetLastError = true)]
    //private static extern int RegCloseKey(IntPtr hKey);
    #endregion

    private bool _disposed;
    private Stack<nint> _mapped = new Stack<nint>();

    public readonly string Path;

    public RegistryProxy()
    {
      using (RegistryKey mappedRoot = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(Guid.NewGuid().ToString()))
      {
        Path = mappedRoot.Name;

        //Computer\HKEY_CLASSES_ROOT\WOW6432Node\CLSID\{D5DE8D20-5BB8-11D1-A1E3-00A0C90F2731}
        using (RegistryKey hkcrMapped = mappedRoot.CreateSubKey("HKEY_CLASSES_ROOT"))
        {
          hkcrMapped.CreateSubKey("WOW6432Node\\CLSID")?.Dispose();
          hkcrMapped.CreateSubKey("CLSID")?.Dispose();
          MapRegistryKey(HKEY_CLASSES_ROOT, hkcrMapped.Handle.DangerousGetHandle());
        }

        using (RegistryKey hklmMapped = mappedRoot.CreateSubKey("HKEY_LOCAL_MACHINE"))
        {
          hklmMapped.CreateSubKey("SOFTWARE\\Classes")?.Dispose();
          MapRegistryKey(HKEY_LOCAL_MACHINE, hklmMapped.Handle.DangerousGetHandle());
        }

        using (RegistryKey hkuMapped = mappedRoot.CreateSubKey("HKEY_USERS"))
        {
          MapRegistryKey(HKEY_USERS, hkuMapped.Handle.DangerousGetHandle());
        }

        using (RegistryKey hkccMapped = mappedRoot.CreateSubKey("HKEY_CURRENT_CONFIG"))
        {
          MapRegistryKey(HKEY_CURRENT_CONFIG, hkccMapped.Handle.DangerousGetHandle());
        }

        using (RegistryKey hkcuMapped = mappedRoot.CreateSubKey("HKEY_CURRENT_USER"))
        {
          MapRegistryKey(HKEY_CURRENT_USER, hkcuMapped.Handle.DangerousGetHandle());
        }
      }
    }

    private void MapRegistryKey(nint source, nint dest)
    {
      int err = RegOverridePredefKey(source, dest);
      if (err != ERROR_SUCCESS)
      {
        throw new Win32Exception(err, "RegOverridePredefKey failed.");
      }

      _mapped.Push(source);
    }

    public void Dispose()
    {
      if (!_disposed)
      {
        using (RegistryKey clsidRoot = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("CLSID", true))
        {
          if (clsidRoot != null)
          {
            clsidRoot.DeleteSubKeyTree("{D5DE8D20-5BB8-11D1-A1E3-00A0C90F2731}", false);
          }
        }

        using (RegistryKey typelibRoot = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\TypeLib", true))
        {
          if (typelibRoot != null)
          {
            typelibRoot.DeleteSubKeyTree("{000204EF-0000-0000-C000-000000000046}", false);
            typelibRoot.DeleteSubKeyTree("{EA544A21-C82D-11D1-A3E4-00A0C90AEA82}", false);
          }
        }

        using (RegistryKey interfacesRoot = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\Interface", true))
        {
          if (interfacesRoot != null)
          {
            string[] interfaceNames = interfacesRoot.GetSubKeyNames();
            foreach (string interfaceName in interfaceNames)
            {
              using (RegistryKey interfaceKey = interfacesRoot.OpenSubKey(interfaceName, true))
              {
                if (interfaceKey != null)
                {
                  using (RegistryKey typeLibKey = interfaceKey.OpenSubKey("TypeLib", true))
                  {
                    if (typeLibKey != null)
                    {
                      object? defaultTypeLibValue;
                      if ((defaultTypeLibValue = typeLibKey.GetValue(null)) != null
                        && (string.Equals("{000204EF-0000-0000-C000-000000000046}", defaultTypeLibValue as string, StringComparison.OrdinalIgnoreCase) || string.Equals("{EA544A21-C82D-11D1-A3E4-00A0C90AEA82}", defaultTypeLibValue as string, StringComparison.OrdinalIgnoreCase)))
                      {
                        interfacesRoot.DeleteSubKeyTree(interfaceName);
                      }
                    }
                  }
                }
              }
            }
          }
        }

        while (_mapped.Any())
        {
          //ignore the result on purpose since we want to go through with all of them
          RegOverridePredefKey(_mapped.Pop(), IntPtr.Zero);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
      }
    }
  }
}