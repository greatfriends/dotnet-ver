using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using Semver;

namespace dotnet_ver
{
    [Command("dotnet ver", Description = ".NET Core Project Version Tool", ExtendedHelpText = @"
Examples:

  dotnet ver                              // Automatically increment the patch version (e.g., 0.1.1 -> 0.1.2)
  dotnet ver -v 0.1.10                    // Set an exact version (e.g., 0.1.1 -> 0.1.10)
  dotnet ver -d ../others                 // Increment the patch version for projects in a specific directory
  dotnet ver -d ../others -v 1.2          // Set an exact version for projects in a specific directory
  dotnet ver -v 0.1.15 -p                 // Set an exact version and return a plain version number
  dotnet ver -p                           // Auto increment the patch version and return a plain version number

  dotnet ver list                         // List the current versions without modifying them
  dotnet ver list -d ../others            // List the current versions from a specific directory")]
    [Subcommand(typeof(ListCommand))]
    class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);


        [Option("-v|--version", Description = @"Specifies the version string to set in the project file.
        If not specified, dotnet-ver will increment the Patch number by one.
        (e.g., 0.1.1 will be updated to 0.1.2)")]
        public string Version { get; } = null;

        [Option("-d|--directory", Description = @"Specifies the starting directory to scan for .csproj files.
        If not specified, the tool will use the current directory as the default.")]
        public string StartDirectory { get; } = null;

        [Option("-p|--plain", Description = @"Outputs just the plain version number, without any additional information or formatting.
        This option is useful when you want to capture the version number for scripting or automation purposes.")]
        public bool Plain { get; } = false;

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

                var (_, v) = SetVersion(projectNode, "Version", versionString, Plain);
                SetVersion(projectNode, "FileVersion", v, Plain);
                SetVersion(projectNode, "AssemblyVersion", v, Plain);
                SetVersion(projectNode, "PackageVersion", v, Plain);
                SetVersion(projectNode, "InformationVersion", v, Plain);

                File.WriteAllText(projectFile, document.ToString());

                if (Plain) {
                    Console.WriteLine(v);
                }
            }

            return 0;
        }

        private static (string oldVersion, string newVersion) SetVersion(XElement projectNode, string elementName, string value, bool isPlain)
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
                var ver = SemVersion.Parse(oldVersion, SemVersionStyles.Any);
                var x = SemVersion.ParsedFrom(ver.Major, ver.Minor, ver.Patch + 1, ver.Prerelease);
                value = x.ToString();
            }

            versionNode.SetValue(value);

            if (!isPlain)
                Console.WriteLine($"  Set {elementName,-20} from {oldVersion} --> {value}");

            return (oldVersion, value);
        }
    }
}
