using System;
using System.Runtime.InteropServices;
using System.Text;

namespace COMWizard.Common.OfflineRegistry
{
  /// <summary>
  /// P/Invoke declarations for offreg.dll functions
  /// </summary>
  internal static class OffRegNative
  {
    private const string OffRegDll = "offreg.dll";

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int OROpenHive(string lpHivePath, out IntPtr phkResult);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORCreateHive(out IntPtr phkResult);

    [DllImport(OffRegDll, SetLastError = true)]
    public static extern int ORCloseHive(IntPtr hKey);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORSaveHive(IntPtr hKey, string lpHivePath, int dwOsMajorVersion, int dwOsMinorVersion);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int OROpenKey(IntPtr hKey, string lpSubKey, out IntPtr phkResult);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORCreateKey(IntPtr hKey, string lpSubKey, string lpClass, int dwOptions, IntPtr pSecurityDescriptor, out IntPtr phkResult, out int pdwDisposition);

    [DllImport(OffRegDll, SetLastError = true)]
    public static extern int ORCloseKey(IntPtr hKey);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int OREnumKey(IntPtr hKey, int dwIndex, StringBuilder lpName, ref int lpcName, StringBuilder lpClass, ref int lpcClass, out long pftLastWriteTime);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int OREnumValue(IntPtr hKey, int dwIndex, StringBuilder lpValueName, ref int lpcValueName, out int lpType, IntPtr lpData, ref int lpcbData);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORGetValue(IntPtr hKey, string lpSubKey, string lpValue, out int pdwType, IntPtr pvData, ref int pcbData);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORSetValue(IntPtr hKey, string lpValueName, int dwType, byte[] lpData, int cbData);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORDeleteKey(IntPtr hKey, string lpSubKey);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORDeleteValue(IntPtr hKey, string lpValueName);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORGetKeySecurity(IntPtr hKey, int SecurityInformation, IntPtr pSecurityDescriptor, ref int lpcbSecurityDescriptor);

    [DllImport(OffRegDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ORSetKeySecurity(IntPtr hKey, int SecurityInformation, IntPtr pSecurityDescriptor);

    public const int REG_OPTION_NON_VOLATILE = 0x00000000;
    public const int REG_CREATED_NEW_KEY = 0x00000001;
    public const int REG_OPENED_EXISTING_KEY = 0x00000002;

    public const int OWNER_SECURITY_INFORMATION = 0x00000001;
    public const int GROUP_SECURITY_INFORMATION = 0x00000002;
    public const int DACL_SECURITY_INFORMATION = 0x00000004;
    public const int SACL_SECURITY_INFORMATION = 0x00000008;

    /// <summary>
    /// Helper to create descriptive error messages with Win32 error details
    /// </summary>
    public static string GetErrorMessage(string operation, int errorCode)
    {
      int lastError = Marshal.GetLastWin32Error();
      string errorMessage = new System.ComponentModel.Win32Exception(lastError).Message;
      return $"{operation} failed. Error code: {errorCode}, Win32 Error: {lastError} ({errorMessage})";
    }
  }
}