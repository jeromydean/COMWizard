namespace COMWizard.Common.OfflineRegistry
{
  //https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-rprn/25cce700-7fcf-4bb6-a2f3-0f6d08430a55
  public enum RegistryType
  {
    REG_NONE = 0x00000000,
    REG_SZ = 0x00000001,
    REG_EXPAND_SZ = 0x00000002,
    REG_BINARY = 0x00000003,
    REG_DWORD = 0x00000004,//aka REG_DWORD_LITTLE_ENDIAN
    REG_DWORD_BIG_ENDIAN = 0x00000005,
    REG_LINK = 0x00000006,
    REG_MULTI_SZ = 0x00000007,
    REG_RESOURCE_LIST = 0x00000008,
    REG_QWORD = 0x0000000B//aka REG_QWORD_LITTLE_ENDIAN
  }
}