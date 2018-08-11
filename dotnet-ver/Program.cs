using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using Semver;

namespace dotnet_ver
{
  [Command("dotnet ver", Description = ".net core projects version tool", ExtendedHelpText = @"
Examples:

  dotnet ver                        // automatically set next version  (ex. 0.1.1 --> 0.1.2)
  dotnet ver -v 0.1.10              // set to exact version            (ex. 0.1.1 --> 0.1.10)
  dotnet ver -d ../others           // set next version for specific directory     
  dotnet ver -d ../others -v 1.2    // set to exact version for specific version

  dotnet ver list                   // just list all current versions witout modify them
  dotnet ver list -d ../others      // list all current versions from specific directory")]
  [Subcommand("list", typeof(ListCommand))]
  class Program
  {
    public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);


    [Option("-v|--version", Description = "Version string to set in the project file. (if not specify, dotnet-ver will increase Patch number)")]
    public string Version { get; } = null;

    [Option("-d|--directory", Description = "Start directory to be scanned for .csproj files. (default is current directory)")]
    public string StartDirectory { get; } = null;

    private int OnExecute()
    {
      var versionString = Version;
      string currentDirectory = StartDirectory ?? Directory.GetCurrentDirectory();
      var projectFiles = Directory.EnumerateFileSystemEntries(currentDirectory, "*.??proj", SearchOption.AllDirectories);

      foreach (var projectFile in projectFiles)
      {
        var document = XDocument.Load(projectFile);
        XElement projectNode;

        Console.WriteLine(projectFile);

        try
        {
          projectNode = document.GetOrCreateElement("Project");
        }
        catch (Exception)
        {
          Console.WriteLine("  (Skipped. Not a .NET core project.)");
          Console.WriteLine();
          continue;
        }

        var sdk = projectNode.Attribute("Sdk");
        if (sdk == null)
        {
          Console.WriteLine("  (Skipped. Not a .NET core project.)");
          Console.WriteLine();
          continue;
        }

        var (_, v) = SetVersion(projectNode, "Version", versionString);
        SetVersion(projectNode, "FileVersion", v);
        SetVersion(projectNode, "AssemblyVersion", v);
        SetVersion(projectNode, "PackageVersion", v);
        SetVersion(projectNode, "InformationVersion", v);

        File.WriteAllText(projectFile, document.ToString());

        Console.WriteLine();
      }

      return 0;
    }

    private static (string oldVersion, string newVersion) SetVersion(XElement projectNode, string elementName, string value)
    {
      string oldVersion = "";
      var versionNode = projectNode
                .Elements("PropertyGroup")
                .SelectMany(it => it.Elements(elementName))
                .SingleOrDefault();

      // If no version node exists, create it.
      if (versionNode == null)
      {
        versionNode = projectNode
            .GetOrCreateElement("PropertyGroup")
            .GetOrCreateElement(elementName);
      }

      oldVersion = versionNode.Value;

      if (value == null)
      {
        var ver = SemVersion.Parse(oldVersion);
        var x = new SemVersion(ver.Major, ver.Minor, ver.Patch + 1, ver.Prerelease, ver.Build);

        value = x.ToString();
      }

      versionNode.SetValue(value);

      Console.WriteLine($"  Set {elementName,-20} from {oldVersion} --> {value}");
      return (oldVersion, value);
    }
  }
}
