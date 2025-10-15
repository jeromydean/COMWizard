using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Win32;

namespace COMWizard.Common.OfflineRegistry
{
  /// <summary>
  /// Represents a key in an offline registry hive
  /// </summary>
  public class OfflineRegistryKey : IDisposable
  {
    private IntPtr _keyHandle;
    private readonly OfflineRegistryHive _hive;
    private readonly string _keyPath;
    private readonly bool _ownsHandle;
    private bool _disposed;

    public string Path => _keyPath;
    public string Name => _keyPath.Split('\\').Last();

    internal OfflineRegistryKey(IntPtr keyHandle, OfflineRegistryHive hive, string keyPath, bool ownsHandle)
    {
      _keyHandle = keyHandle;
      _hive = hive;
      _keyPath = keyPath;
      _ownsHandle = ownsHandle;
    }

    /// <summary>
    /// Opens a subkey
    /// </summary>
    public OfflineRegistryKey OpenSubKey(string name)
    {
      ThrowIfDisposed();
      int result = OffRegNative.OROpenKey(_keyHandle, name, out IntPtr subKeyHandle);
      if (result != 0)
        return null;

      string subKeyPath = string.IsNullOrEmpty(_keyPath) ? name : $"{_keyPath}\\{name}";
      return new OfflineRegistryKey(subKeyHandle, _hive, subKeyPath, true);
    }

    /// <summary>
    /// Creates a subkey or opens it if it already exists
    /// </summary>
    public OfflineRegistryKey CreateSubKey(string name)
    {
      ThrowIfDisposed();
      int result = OffRegNative.ORCreateKey(_keyHandle, name, null,
          OffRegNative.REG_OPTION_NON_VOLATILE, IntPtr.Zero,
          out IntPtr subKeyHandle, out int disposition);

      if (result != 0)
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Create subkey", result));

      string subKeyPath = string.IsNullOrEmpty(_keyPath) ? name : $"{_keyPath}\\{name}";
      return new OfflineRegistryKey(subKeyHandle, _hive, subKeyPath, true);
    }

    /// <summary>
    /// Deletes a subkey
    /// </summary>
    public void DeleteSubKey(string name)
    {
      ThrowIfDisposed();
      int result = OffRegNative.ORDeleteKey(_keyHandle, name);
      if (result != 0)
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Delete subkey", result));
    }

    /// <summary>
    /// Gets the names of all subkeys
    /// </summary>
    public string[] GetSubKeyNames()
    {
      ThrowIfDisposed();
      var subKeyNames = new System.Collections.Generic.List<string>();
      int index = 0;

      while (true)
      {
        var nameBuilder = new StringBuilder(256);
        int nameLength = nameBuilder.Capacity;
        var classBuilder = new StringBuilder(256);
        int classLength = classBuilder.Capacity;

        int result = OffRegNative.OREnumKey(_keyHandle, index, nameBuilder,
            ref nameLength, classBuilder, ref classLength, out long lastWriteTime);

        if (result == 259) // ERROR_NO_MORE_ITEMS
          break;

        if (result != 0)
          throw new InvalidOperationException(OffRegNative.GetErrorMessage("Enumerate subkeys", result));

        subKeyNames.Add(nameBuilder.ToString());
        index++;
      }

      return subKeyNames.ToArray();
    }

    /// <summary>
    /// Gets the names of all values
    /// </summary>
    public string[] GetValueNames()
    {
      ThrowIfDisposed();
      var valueNames = new List<string>();
      int index = 0;

      while (true)
      {
        var nameBuilder = new StringBuilder(16384);
        int nameLength = nameBuilder.Capacity;
        int dataSize = 0;

        int result = OffRegNative.OREnumValue(_keyHandle, index, nameBuilder,
            ref nameLength, out int type, IntPtr.Zero, ref dataSize);

        if (result == 259) // ERROR_NO_MORE_ITEMS
          break;

        if (result == 234) // ERROR_MORE_DATA
        {
          // The name length returned indicates how much space is needed
          // This shouldn't happen with 16384 chars, but handle it just in case
          nameBuilder = new StringBuilder(nameLength + 1);
          result = OffRegNative.OREnumValue(_keyHandle, index, nameBuilder,
              ref nameLength, out type, IntPtr.Zero, ref dataSize);
        }

        if (result != 0)
          throw new InvalidOperationException(OffRegNative.GetErrorMessage("Enumerate values", result));

        valueNames.Add(nameBuilder.ToString());
        index++;
      }

      return valueNames.ToArray();
    }

    /// <summary>
    /// Gets a registry value
    /// </summary>
    public object GetValue(string name, object defaultValue = null)
    {
      ThrowIfDisposed();
      int dataSize = 0;

      // Get the size first
      int result = OffRegNative.ORGetValue(_keyHandle, null, name, out int type, IntPtr.Zero, ref dataSize);
      if (result != 0)
        return defaultValue;

      // Allocate buffer and get the data
      IntPtr dataPtr = Marshal.AllocHGlobal(dataSize);
      try
      {
        result = OffRegNative.ORGetValue(_keyHandle, null, name, out type, dataPtr, ref dataSize);
        if (result != 0)
          return defaultValue;

        return ParseRegistryValue(type, dataPtr, dataSize);
      }
      finally
      {
        Marshal.FreeHGlobal(dataPtr);
      }
    }

    public RegistryValue GetValueExtended(string name)
    {
      ThrowIfDisposed();

      RegistryValue registryValue = new RegistryValue();

      int dataSize = 0;

      // Get the size first
      int result = OffRegNative.ORGetValue(_keyHandle, null, name, out int type, IntPtr.Zero, ref dataSize);
      if (result != 0)
        return null;

      // Allocate buffer and get the data
      IntPtr dataPtr = Marshal.AllocHGlobal(dataSize);
      try
      {
        result = OffRegNative.ORGetValue(_keyHandle, null, name, out type, dataPtr, ref dataSize);
        if (result != 0)
          return null;

        registryValue.Data = ParseRegistryValue(type, dataPtr, dataSize);
        registryValue.Type = Enum.IsDefined(typeof(RegistryType), type) ? (RegistryType)type : null;
      }
      finally
      {
        Marshal.FreeHGlobal(dataPtr);
      }

      return registryValue;
    }

    /// <summary>
    /// Sets a registry value
    /// </summary>
    public void SetValue(string name, object value, RegistryValueKind valueKind = RegistryValueKind.Unknown)
    {
      ThrowIfDisposed();

      if (valueKind == RegistryValueKind.Unknown)
        valueKind = InferValueKind(value);

      byte[] data = ConvertToRegistryData(value, valueKind);
      int result = OffRegNative.ORSetValue(_keyHandle, name, (int)valueKind, data, data.Length);

      if (result != 0)
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Set value", result));
    }

    /// <summary>
    /// Deletes a value
    /// </summary>
    public void DeleteValue(string name)
    {
      ThrowIfDisposed();
      int result = OffRegNative.ORDeleteValue(_keyHandle, name);
      if (result != 0)
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Delete value", result));
    }

    /// <summary>
    /// Gets the security descriptor for this key
    /// </summary>
    public RegistrySecurity GetAccessControl(AccessControlSections includeSections = AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group)
    {
      ThrowIfDisposed();

      int securityInfo = 0;
      if ((includeSections & AccessControlSections.Owner) != 0)
        securityInfo |= OffRegNative.OWNER_SECURITY_INFORMATION;
      if ((includeSections & AccessControlSections.Group) != 0)
        securityInfo |= OffRegNative.GROUP_SECURITY_INFORMATION;
      if ((includeSections & AccessControlSections.Access) != 0)
        securityInfo |= OffRegNative.DACL_SECURITY_INFORMATION;
      if ((includeSections & AccessControlSections.Audit) != 0)
        securityInfo |= OffRegNative.SACL_SECURITY_INFORMATION;

      // First call to get required buffer size
      int bufferSize = 0;
      int result = OffRegNative.ORGetKeySecurity(_keyHandle, securityInfo, IntPtr.Zero, ref bufferSize);

      if (result != 122) // ERROR_INSUFFICIENT_BUFFER
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Get key security (size)", result));

      // Allocate buffer and get the security descriptor
      IntPtr securityDescriptor = Marshal.AllocHGlobal(bufferSize);
      try
      {
        result = OffRegNative.ORGetKeySecurity(_keyHandle, securityInfo, securityDescriptor, ref bufferSize);
        if (result != 0)
          throw new InvalidOperationException(OffRegNative.GetErrorMessage("Get key security", result));

        // Convert to byte array
        byte[] securityDescriptorBytes = new byte[bufferSize];
        Marshal.Copy(securityDescriptor, securityDescriptorBytes, 0, bufferSize);

        // Create RegistrySecurity from binary form
        var registrySecurity = new RegistrySecurity();
        registrySecurity.SetSecurityDescriptorBinaryForm(securityDescriptorBytes);
        return registrySecurity;
      }
      finally
      {
        Marshal.FreeHGlobal(securityDescriptor);
      }
    }

    /// <summary>
    /// Sets the security descriptor for this key
    /// </summary>
    public void SetAccessControl(RegistrySecurity registrySecurity, AccessControlSections includeSections = AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group)
    {
      ThrowIfDisposed();

      if (registrySecurity == null)
        throw new ArgumentNullException(nameof(registrySecurity));

      int securityInfo = 0;
      if ((includeSections & AccessControlSections.Owner) != 0)
        securityInfo |= OffRegNative.OWNER_SECURITY_INFORMATION;
      if ((includeSections & AccessControlSections.Group) != 0)
        securityInfo |= OffRegNative.GROUP_SECURITY_INFORMATION;
      if ((includeSections & AccessControlSections.Access) != 0)
        securityInfo |= OffRegNative.DACL_SECURITY_INFORMATION;
      if ((includeSections & AccessControlSections.Audit) != 0)
        securityInfo |= OffRegNative.SACL_SECURITY_INFORMATION;

      // Get the binary form of the security descriptor
      byte[] securityDescriptorBytes = registrySecurity.GetSecurityDescriptorBinaryForm();

      IntPtr securityDescriptor = Marshal.AllocHGlobal(securityDescriptorBytes.Length);
      try
      {
        Marshal.Copy(securityDescriptorBytes, 0, securityDescriptor, securityDescriptorBytes.Length);

        int result = OffRegNative.ORSetKeySecurity(_keyHandle, securityInfo, securityDescriptor);
        if (result != 0)
          throw new InvalidOperationException(OffRegNative.GetErrorMessage("Set key security", result));
      }
      finally
      {
        Marshal.FreeHGlobal(securityDescriptor);
      }
    }

    private object ParseRegistryValue(int type, IntPtr dataPtr, int dataSize)
    {
      switch ((RegistryValueKind)type)
      {
        case RegistryValueKind.String:
          return Marshal.PtrToStringUni(dataPtr);

        case RegistryValueKind.DWord:
          return Marshal.ReadInt32(dataPtr);

        case RegistryValueKind.QWord:
          return Marshal.ReadInt64(dataPtr);

        case RegistryValueKind.Binary:
          byte[] binaryData = new byte[dataSize];
          Marshal.Copy(dataPtr, binaryData, 0, dataSize);
          return binaryData;

        case RegistryValueKind.MultiString:
          string multiString = Marshal.PtrToStringUni(dataPtr, dataSize / 2);
          return multiString.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

        default:
          byte[] rawData = new byte[dataSize];
          Marshal.Copy(dataPtr, rawData, 0, dataSize);
          return rawData;
      }
    }

    private RegistryValueKind InferValueKind(object value)
    {
      if (value is string) return RegistryValueKind.String;
      if (value is int) return RegistryValueKind.DWord;
      if (value is long) return RegistryValueKind.QWord;
      if (value is byte[]) return RegistryValueKind.Binary;
      if (value is string[]) return RegistryValueKind.MultiString;
      return RegistryValueKind.Binary;
    }

    private byte[] ConvertToRegistryData(object value, RegistryValueKind valueKind)
    {
      switch (valueKind)
      {
        case RegistryValueKind.String:
          return Encoding.Unicode.GetBytes((string)value + "\0");

        case RegistryValueKind.DWord:
          return BitConverter.GetBytes((int)value);

        case RegistryValueKind.QWord:
          return BitConverter.GetBytes((long)value);

        case RegistryValueKind.Binary:
          return (byte[])value;

        case RegistryValueKind.MultiString:
          string[] strings = (string[])value;
          string combined = string.Join("\0", strings) + "\0\0";
          return Encoding.Unicode.GetBytes(combined);

        default:
          throw new ArgumentException($"Unsupported value kind: {valueKind}");
      }
    }

    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(OfflineRegistryKey));
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (_ownsHandle && _keyHandle != IntPtr.Zero)
        {
          OffRegNative.ORCloseKey(_keyHandle);
          _keyHandle = IntPtr.Zero;
        }
        _disposed = true;
      }
    }

    ~OfflineRegistryKey()
    {
      Dispose(false);
    }
  }
}