using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CsvHelper;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;

namespace ModListCommand
{
    public class ModListCommand : Mod
    {
        private ModToolkit _toolkit = new ModToolkit();

        public override void Entry(IModHelper helper)
        {
            
            helper.ConsoleCommands.Add("list_mods", "outputs a list of installed mods", ListMods);
        }

        private string GetUpdateLinks(IEnumerable<string> updateKeys, string separator = ";")
        {
            return string.Join(separator, updateKeys?.Select(u => _toolkit.GetUpdateUrl(u)).DefaultIfEmpty().ToArray() ?? new[] {"no update key"});
        }

        private CsvRecord CreateRecord(IManifest manifest)
        {
            return new CsvRecord()
            {
                Links = GetUpdateLinks(manifest.UpdateKeys),
                Version = manifest.Version.ToString(),
                Description = manifest.Description,
                Author = manifest.Author,
                Name = manifest.Name
            };
        }

        private void ListMods(string commandName, string[] args)
        {
            if (args.Length == 0 || args[0] == "console")
            {
                foreach (var mod in Helper.ModRegistry.GetAll())
                {
                    var links = GetUpdateLinks(mod.Manifest.UpdateKeys);

                    Monitor.Log($"{mod.Manifest.Name} v{mod.Manifest.Version} by {mod.Manifest.Author} {links} :\n"
                              + $"\t{mod.Manifest.Description}", LogLevel.Info);
                }
            }
            else if (args.Length == 2 && args[0] == "csv" && !string.IsNullOrWhiteSpace(args[1]))
            {
                using (var mem = new MemoryStream())
                using (var writer = new StreamWriter(mem))
                using (var csvWriter = new CsvWriter(writer))
                {
                    try
                    {
                        var data = Helper.ModRegistry.GetAll().Select(m => CreateRecord(m.Manifest));

                        csvWriter.Configuration.Delimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                        csvWriter.Configuration.ShouldQuote = (s, context) => true;
                        csvWriter.Configuration.HasHeaderRecord = true;
                        csvWriter.Configuration.AutoMap<CsvRecord>();

                        csvWriter.WriteHeader<CsvRecord>();
                        csvWriter.NextRecord();
                        csvWriter.WriteRecords(data);
                        writer.Flush();

                        File.WriteAllLines(args[1], new[] { Encoding.UTF8.GetString(mem.ToArray()) });
                        Monitor.Log($"List of mods written to `${args[1]}`", LogLevel.Info);
                        Process.Start(args[1]);
                    }
                    catch (Exception e)
                    {
                        Monitor.Log(e.Message, LogLevel.Error);
                    }
                }
            }
            else
            {
                ShowUsage(commandName);
            }
        }

        private void ShowUsage(string commandName)
        {
            Monitor.Log($"{commandName} [console|csv] [csvFile]", LogLevel.Warn);
        }
    }
}
