using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace COMWizard.Common.PortableExecutable
{
  public class PEParsingService : IPEParsingService
  {
    public PEMetadata Parse(string path)
    {
      PEMetadata metadata = new PEMetadata
      {
        Path = path
      };

      using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
      {
        using (SHA256 sha256 = SHA256.Create())
        {
          metadata.SHA256 = BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "");//Convert.ToHexString(sha256.ComputeHash(fileStream));
          fileStream.Position = 0;
        }

        if (fileStream.Length >= 64)//dos header size
        {
          using (BinaryReader br = new BinaryReader(fileStream))
          {
            if (br.ReadUInt16() == 0x5A4D)//MZ
            {
              metadata.IsPortableExecutable = true;

              br.BaseStream.Position = 0;
              using (PEReader peReader = new PEReader(fileStream))
              {
                metadata.Magic = peReader.PEHeaders.PEHeader.Magic;

                metadata.IsLibrary = peReader.PEHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.Dll)
                  && peReader.PEHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.ExecutableImage);

                metadata.IsExecutable = !peReader.PEHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.Dll)
                  && peReader.PEHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.ExecutableImage);

                if (metadata.IsLibrary || metadata.IsExecutable)
                {
                  metadata.Architecture = peReader.PEHeaders.CoffHeader.Machine;
                  metadata.Exports = GetExportNames(peReader).ToList();

                  metadata.IsCOM = metadata.Exports.Any(e => string.Equals("DllGetClassObject", e, StringComparison.OrdinalIgnoreCase))
                    && (metadata.Exports.Any(e => string.Equals("DllCanUnloadNow", e, StringComparison.OrdinalIgnoreCase))
                      || metadata.Exports.Any(e => string.Equals("DllRegisterServer", e, StringComparison.OrdinalIgnoreCase))
                      || metadata.Exports.Any(e => string.Equals("DllUnregisterServer", e, StringComparison.OrdinalIgnoreCase)));
                }

                //cli assembly inspection
                if (peReader.HasMetadata)
                {
                  metadata.IsAssembly = true;

                  CorHeader? corHeader;
                  if ((corHeader = peReader.PEHeaders.CorHeader) != null)
                  {
                    metadata.MajorRuntimeVersion = corHeader.MajorRuntimeVersion;
                    metadata.MinorRuntimeVersion = corHeader.MinorRuntimeVersion;

                    //corflags
                    metadata.ILOnly = (corHeader.Flags & CorFlags.ILOnly) == CorFlags.ILOnly;
                    metadata.Requires32Bit = (corHeader.Flags & CorFlags.Requires32Bit) == CorFlags.Requires32Bit;
                    metadata.ILLibrary = (corHeader.Flags & CorFlags.ILLibrary) == CorFlags.ILLibrary;
                    metadata.StrongNameSigned = (corHeader.Flags & CorFlags.StrongNameSigned) == CorFlags.StrongNameSigned;
                    metadata.NativeEntryPoint = (corHeader.Flags & CorFlags.NativeEntryPoint) == CorFlags.NativeEntryPoint;
                    metadata.TrackDebugData = (corHeader.Flags & CorFlags.TrackDebugData) == CorFlags.TrackDebugData;
                    metadata.Prefers32Bit = (corHeader.Flags & CorFlags.Prefers32Bit) == CorFlags.Prefers32Bit;
                  }

                  MetadataReader metadataReader = peReader.GetMetadataReader();
                  if (metadataReader.IsAssembly)
                  {
                    AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();
                    string assemblyName = metadataReader.GetString(assemblyDefinition.Name);
                    Version version = assemblyDefinition.Version;

                    foreach (CustomAttributeHandle customAttributeHandle in assemblyDefinition.GetCustomAttributes())
                    {
                      CustomAttribute customAttribute = metadataReader.GetCustomAttribute(customAttributeHandle);
                      EntityHandle attributeCtor = customAttribute.Constructor;
                      if (attributeCtor.Kind == HandleKind.MemberReference)
                      {
                        EntityHandle container = metadataReader.GetMemberReference((MemberReferenceHandle)attributeCtor).Parent;
                        if (container.Kind == HandleKind.TypeReference)
                        {
                          TypeReference typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)container);

                          string typeNamespace = metadataReader.GetString(typeRef.Namespace);
                          string typeName = metadataReader.GetString(typeRef.Name);

                          if (string.Equals(typeNamespace, "System.Runtime.Versioning")
                            && string.Equals(typeName, "TargetFrameworkAttribute"))
                          {
                            BlobHandle blob = customAttribute.Value;
                            BlobReader reader = metadataReader.GetBlobReader(blob);

                            if (reader.ReadUInt16() == 0x0001)
                            {
                              string targetFramework = reader.ReadSerializedString();
                              if (!string.IsNullOrEmpty(targetFramework))
                              {
                                metadata.Framework = new FrameworkName(targetFramework);
                                break;
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }

      return metadata;
    }

    private IEnumerable<string> GetExportNames(PEReader peReader)
    {
      DirectoryEntry exportDir = peReader.PEHeaders.PEHeader.ExportTableDirectory;

      if (exportDir.Size == 0 || exportDir.RelativeVirtualAddress == 0)
      {
        yield break;
      }

      PEMemoryBlock exportBlock = peReader.GetSectionData(exportDir.RelativeVirtualAddress);
      BlobReader r = exportBlock.GetReader();

      //IMAGE_EXPORT_DIRECTORY - 40 bytes
      r.Offset = 0;
      uint characteristics = r.ReadUInt32();
      uint timeDateStamp = r.ReadUInt32();
      ushort majorVersion = r.ReadUInt16();
      ushort minorVersion = r.ReadUInt16();
      uint nameRva = r.ReadUInt32();
      uint baseOrdinal = r.ReadUInt32();
      uint numberOfFunctions = r.ReadUInt32();
      uint numberOfNames = r.ReadUInt32();
      uint addrOfFunctions = r.ReadUInt32();
      uint addrOfNames = r.ReadUInt32();
      uint addrOfOrdinals = r.ReadUInt32();

      if (numberOfNames == 0 || addrOfNames == 0)
      {
        yield break;
      }

      PEMemoryBlock nameTableBlock = peReader.GetSectionData((int)addrOfNames);
      BlobReader nr = nameTableBlock.GetReader();

      for (int i = 0; i < numberOfNames; i++)
      {
        uint namePtrRva = nr.ReadUInt32();
        string name = ReadNullTerminatedAsciiString(peReader, namePtrRva);
        if (!string.IsNullOrEmpty(name))
        {
          yield return name;
        }
      }
    }

    private string ReadNullTerminatedAsciiString(PEReader peReader, uint rva)
    {
      PEMemoryBlock block = peReader.GetSectionData((int)rva);
      BlobReader reader = block.GetReader();
      StringBuilder sb = new StringBuilder();

      for (int i = 0; i < 4096 && reader.RemainingBytes > 0; i++)
      {
        byte b = reader.ReadByte();
        if (b == 0) break;
        sb.Append((char)b);
      }

      return sb.ToString();
    }
  }
}