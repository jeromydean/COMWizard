using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;

namespace COMWizard.Common.PortableExecutable
{
  public class PEMetadata
  {
    public string Path { get; set; }
    public string SHA256 { get; set; }
    public bool IsPortableExecutable { get; set; }
    public PEMagic Magic { get; set; }
    public bool IsLibrary { get; set; }
    public bool IsExecutable { get; set; }
    public Machine Architecture { get; set; }
    public List<string> Exports { get; set; } = new List<string>();
    public bool IsCOM { get; set; }
    public bool IsAssembly { get; set; }
    public bool IsNetFrameworkAssembly
    {
      get
      {
        if (IsAssembly && Framework != null)
        {
          //.NET Framework(classic)
          //TargetFramework string FrameworkName.Version
          //.NETFramework,Version = v2.0  2.0.0.0
          //.NETFramework,Version = v3.5  3.5.0.0
          //.NETFramework,Version = v4.0  4.0.0.0
          //.NETFramework,Version = v4.5  4.5.0.0
          //.NETFramework,Version = v4.5.2  4.5.2.0
          //.NETFramework,Version = v4.6.1  4.6.1.0
          //.NETFramework,Version = v4.7  4.7.0.0
          //.NETFramework,Version = v4.7.2  4.7.2.0
          //.NETFramework,Version = v4.8  4.8.0.0
          //.NETFramework,Version = v4.8.1  4.8.1.0

          //.NET Core / .NET 5 + (.NETCoreApp)
          //TargetFramework string FrameworkName.Version
          //.NETCoreApp,Version = v1.0  1.0.0.0
          //.NETCoreApp,Version = v2.1  2.1.0.0
          //.NETCoreApp,Version = v3.1  3.1.0.0
          //.NETCoreApp,Version = v5.0  5.0.0.0
          //.NETCoreApp,Version = v6.0  6.0.0.0
          //.NETCoreApp,Version = v7.0  7.0.0.0
          //.NETCoreApp,Version = v8.0  8.0.0.0

          //.NET Standard
          //TargetFramework string FrameworkName.Version
          //.NETStandard,Version = v1.0 1.0.0.0
          //.NETStandard,Version = v1.6 1.6.0.0
          //.NETStandard,Version = v2.0 2.0.0.0
          //.NETStandard,Version = v2.1 2.1.0.0

          switch (Framework.Version.ToString())
          {
            case "2.0.0.0":
            case "3.5.0.0":
            case "4.0.0.0":
            case "4.5.0.0":
            case "4.5.2.0":
            case "4.6.1.0":
            case "4.7.0.0":
            case "4.7.2.0":
            case "4.8.0.0":
            case "4.8.1.0":
              return true;
          }
        }
        return false;
      }
    }
    public ushort? MajorRuntimeVersion { get; set; }
    public ushort? MinorRuntimeVersion { get; set; }
    public FrameworkName? Framework { get; set; }
    public bool? ILOnly { get; set; }
    public bool? Requires32Bit { get; set; }
    public bool? ILLibrary { get; set; }
    public bool? StrongNameSigned { get; set; }
    public bool? NativeEntryPoint { get; set; }
    public bool? TrackDebugData { get; set; }
    public bool? Prefers32Bit { get; set; }
  }
}
