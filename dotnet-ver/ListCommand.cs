using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace dotnet_ver
{
  class ListCommand
  {

    [Option("-d|--directory", Description = "Start directory to be scanned for .csproj files. (default is current directory)")]
    public string StartDirectory { get; } = null;


    private int OnExecute()
    { 
      string currentDirectory = StartDirectory ?? Directory.GetCurrentDirectory();
      var projectFiles = Directory.EnumerateFileSystemEntries(currentDirectory, "*.??proj", SearchOption.AllDirectories);

      foreach (var projectFile in projectFiles)
      {
        var document = XDocument.Load(projectFile);
        XElement projectNode;

        Console.WriteLine(projectFile);

        try
        {
          projectNode = document.GetElement("Project");
        }
        catch (Exception)
        {
          Console.WriteLine("  (Skipped. Not a .NET core project.)");
          Console.WriteLine();
          continue;
        }

        var sdk = projectNode?.Attribute("Sdk");
        if (sdk == null)
        {
          Console.WriteLine("  (Skipped. Not a .NET core project.)");
          Console.WriteLine();
          continue;
        }

        GetVersion(projectNode, "Version");
        GetVersion(projectNode, "FileVersion");
        GetVersion(projectNode, "AssemblyVersion");
        GetVersion(projectNode, "PackageVersion");
        GetVersion(projectNode, "InformationVersion");
          
        Console.WriteLine();
      }

      return 0;
    }

    private static void GetVersion(XElement projectNode, string elementName)
    {
      if (projectNode == null) return;

      string oldVersion = "";
      var versionNode = projectNode
                .Elements("PropertyGroup")
                .SelectMany(it => it.Elements(elementName))
                .SingleOrDefault();

      oldVersion = versionNode?.Value; 

      Console.WriteLine($"  {elementName,-20} = {oldVersion}"); 
    }
  }
}
