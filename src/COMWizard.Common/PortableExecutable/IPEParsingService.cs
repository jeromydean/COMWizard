namespace COMWizard.Common.PortableExecutable
{
  public interface IPEParsingService
  {
    PEMetadata Parse(string path);
  }
}
