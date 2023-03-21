using System.Collections.Generic;
using StardewModdingAPI.Toolkit;
using System.Globalization;
using System.Diagnostics;
using StardewModdingAPI;
using System.Linq;
using System.IO;
using KBCsv;
using System;

namespace ModListCommand
{
    public sealed class ModListCommand : Mod
    {
        private const string CommandName = "list_mods";

        private const string HelpText = $"""
            Lists currently loaded mods.

            Usage: {CommandName} [console]
            - the console parameter is optional
            Lists mods to the console.

            Usage: {CommandName} csv <path>
            - path (required): the (absolute or relative) path to the file to be created. The base directory for relative paths is the game's installation directory (i.e. where Stardew Valley.dll is).

            Examples:
            {CommandName}
            {CommandName} console    (does the same as the previous example)
            {CommandName} csv c:\temp\mods.csv
            """;

        private readonly ModToolkit _toolkit = new();

        public override void Entry(IModHelper helper)
        {
            helper.ConsoleCommands.Add(CommandName, HelpText, ListMods);
        }

        public string GetUpdateLinks(IEnumerable<string> updateKeys, string separator = ";")
        {
            return string.Join(separator, updateKeys?.Select(u => _toolkit.GetUpdateUrl(u) ?? u).DefaultIfEmpty().ToArray() ?? new[] {"no update key"});
        }

        private void ListMods(string commandName, string[] args)
        {
            if (args.Length == 0 || args[0] == "console")
            {
                foreach (var mod in Helper.ModRegistry.GetAll())
                {
                    const string separator = "\n    - ";
                    var links = GetUpdateLinks(mod.Manifest.UpdateKeys, separator);

                    Monitor.Log($"{mod.Manifest.Name} v{mod.Manifest.Version} by {mod.Manifest.Author}:{separator}{links}\n"
                              + $"{mod.Manifest.Description}", LogLevel.Info);
                }
            }
            else if (args.Length == 2 && args[0] == "csv" && !string.IsNullOrWhiteSpace(args[1]))
            { 
                try
                { 
                    using (var streamWriter = new StreamWriter(args[1]))
                    using (var writer = new CsvWriter(streamWriter)
                    {
                        ValueSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0],
                        ValueDelimiter = '"',
                        ForceDelimit = true
                    })
                    {
                        try
                        {
                            writer.WriteRecord("Name", "Version", "Author", "Links", "Description");

                            foreach (var manifest in Helper.ModRegistry.GetAll().Select(e => e.Manifest))
                            {
                                writer.WriteRecord(manifest.Name, manifest.Version.ToString(), manifest.Author,
                                                   GetUpdateLinks(manifest.UpdateKeys), manifest.Description);
                            }

                            Process.Start(args[1]);

                            Monitor.Log($"{writer.RecordNumber} records written to `{Path.GetFullPath(args[1])}`", LogLevel.Info);
                        }
                        catch (Exception e)
                        {
                            Monitor.Log(e.Message, LogLevel.Error);
                        }
                    }
                }
                catch(Exception e)
                {
                    Monitor.Log(e.Message, LogLevel.Error);
                }
            }
            else
            {
                Monitor.Log($"Incorrect parameters!{Environment.NewLine}See the help:", LogLevel.Warn);
                Monitor.Log(HelpText, LogLevel.Info);
            }
        }
    }
}
