using System.Collections.Generic;
using StardewModdingAPI.Toolkit;
using System.Globalization;
using System.Diagnostics;
using StardewModdingAPI;
using System.Linq;
using System.IO;
using KBCsv;
using System;
using System.Collections.Immutable;

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

        private void ListMods(string _, string[] args)
        {
            if (args.Length == 0 || args[0] == "console")
            {
                ListModsToConsole();
            }
            else if (args.Length == 2 && args[0] == "csv" && !string.IsNullOrWhiteSpace(args[1]))
            {
                ListModsToCsv(csvPath: args[1]);
            }
            else
            {
                Monitor.Log($"Incorrect parameters!{Environment.NewLine}See the help:", LogLevel.Warn);
                Monitor.Log(HelpText, LogLevel.Info);
            }
        }
        private void ListModsToConsole()
        {
            foreach (var modInfo in EnumerateModInfos())
            {
                const string separator = "\n    - ";
                var links = string.Join(separator, modInfo.UpdateUrls);

                Monitor.Log($"{modInfo.Name} v{modInfo.Version} by {modInfo.Author}:{separator}{links}\n"
                          + $"{modInfo.Description}", LogLevel.Info);
            }
        }

        private void ListModsToCsv(string csvPath)
        {
            using (var streamWriter = new StreamWriter(csvPath))
            using (var writer = new CsvWriter(streamWriter)
            {
                ValueSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0],
                ValueDelimiter = '"',
                ForceDelimit = true
            })
            {
                writer.WriteRecord("Name", "Version", "Author", "Links", "Description");

                foreach (var modInfo in EnumerateModInfos())
                {
                    writer.WriteRecord(
                        modInfo.Name,
                        modInfo.Version.ToString(),
                        modInfo.Author,
                        string.Join(";", modInfo.UpdateUrls),
                        modInfo.Description);
                }

                Process.Start(csvPath);

                Monitor.Log($"{writer.RecordNumber} records written to `{Path.GetFullPath(csvPath)}`", LogLevel.Info);
            }
        }

        private IEnumerable<ModInfo> EnumerateModInfos()
        {
            return Helper.ModRegistry.GetAll()
                .Select(x => CreateModInfo(x.Manifest));
        }

        private ModInfo CreateModInfo(IManifest manifest)
        {
            return new ModInfo
            {
                Name = manifest.Name,
                Version = manifest.Version,
                Author = manifest.Author,
                Description = manifest.Description,
                UpdateUrls = GetUrls(manifest.UpdateKeys).ToImmutableList(),
            };
        }

        private IEnumerable<string> GetUrls(IEnumerable<string> updateKeys)
        {
            return updateKeys
                ?.Select(u => _toolkit.GetUpdateUrl(u) ?? u)
                .DefaultIfEmpty()
                ?? new[] { "no update key" };
        }
    }
}
