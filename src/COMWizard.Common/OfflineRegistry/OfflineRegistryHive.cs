using System;

namespace COMWizard.Common.OfflineRegistry
{
  /// <summary>
  /// Represents an offline registry hive
  /// </summary>
  public class OfflineRegistryHive : IDisposable
  {
    private IntPtr _hiveHandle;
    private bool _disposed;

    private OfflineRegistryHive(IntPtr hiveHandle)
    {
      _hiveHandle = hiveHandle;
    }

    /// <summary>
    /// Opens an existing offline registry hive file
    /// </summary>
    public static OfflineRegistryHive Open(string hivePath)
    {
      int result = OffRegNative.OROpenHive(hivePath, out IntPtr hiveHandle);
      if (result != 0)
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Open hive", result));

      return new OfflineRegistryHive(hiveHandle);
    }

    /// <summary>
    /// Creates a new offline registry hive in memory
    /// </summary>
    public static OfflineRegistryHive Create()
    {
      int result = OffRegNative.ORCreateHive(out IntPtr hiveHandle);
      if (result != 0)
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Create hive", result));

      return new OfflineRegistryHive(hiveHandle);
    }

    /// <summary>
    /// Saves the hive to a file
    /// </summary>
    public void Save(string hivePath, int osMajorVersion = 6, int osMinorVersion = 1)
    {
      ThrowIfDisposed();
      int result = OffRegNative.ORSaveHive(_hiveHandle, hivePath, osMajorVersion, osMinorVersion);
      if (result != 0)
        throw new InvalidOperationException(OffRegNative.GetErrorMessage("Save hive", result));
    }

    /// <summary>
    /// Opens the root key of the hive
    /// </summary>
    public OfflineRegistryKey Root
    {
      get
      {
        ThrowIfDisposed();
        return new OfflineRegistryKey(_hiveHandle, this, string.Empty, false);
      }
    }

    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(OfflineRegistryHive));
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
        if (_hiveHandle != IntPtr.Zero)
        {
          OffRegNative.ORCloseHive(_hiveHandle);
          _hiveHandle = IntPtr.Zero;
        }
        _disposed = true;
      }
    }

    ~OfflineRegistryHive()
    {
      Dispose(false);
    }
  }
}