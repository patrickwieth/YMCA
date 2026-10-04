using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckSoakTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-soak-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 3 && args.Length <= 5 &&
			args.Skip(3).All(a => a.StartsWith("ticks=", StringComparison.Ordinal) && int.TryParse(a.Substring(6), out var ticks) && ticks >= 1000 && ticks <= 2000000 ||
				a.StartsWith("bots=", StringComparison.Ordinal) && int.TryParse(a.Substring(5), out var bots) && bots >= 0 && bots <= 15) &&
			args.Skip(3).Select(a => a.Split('=')[0]).Distinct().Count() == args.Length - 3;
		[Desc("MAP OUTPUT [ticks=15000] [bots=0]", "Create a camera/shroud soak fixture with optional real local bot clients; no production terrain edits.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var duration = int.Parse(args.Skip(3).FirstOrDefault(a => a.StartsWith("ticks=", StringComparison.Ordinal))?.Substring(6) ?? "15000");
			var bots = int.Parse(args.Skip(3).FirstOrDefault(a => a.StartsWith("bots=", StringComparison.Ordinal))?.Substring(5) ?? "0");
			Directory.CreateDirectory(args[2]);
			using var stream = File.OpenRead(args[1]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
			using var map = new Map(utility.ModData, zip);
			if (!map.Tileset.StartsWith("RUBBERDUCK-", StringComparison.Ordinal) ||
				map.RuleDefinitions != null && (!map.RuleDefinitions.Nodes.IsDefaultOrEmpty || !string.IsNullOrEmpty(map.RuleDefinitions.Value)))
				throw new InvalidDataException("Soak requires a production Rubberduck map without custom rules.");
			var directory = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes");
			Directory.CreateDirectory(directory);
			var result = Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(args[2])))) + ".log");
			File.WriteAllText(result, "");
			File.WriteAllText(Path.Combine(args[2], "runtime-results-path.txt"), result);
			var exploration = bots == 0 ? "Player:\n\tShroud:\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n" : "";
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tTerrainSoakProbe:\n\t\tDurationTicks: {duration}\n\t\tBotCount: {bots}\n\t\tResultPath: {result}\n" + exploration, "terrain-soak"));
			var path = Path.Combine(args[2], "rubberduck-soak.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var saved = File.OpenRead(path);
			using var savedZip = new ZipFileLoader.ReadOnlyZipFile(saved, path);
			using var reload = new Map(utility.ModData, savedZip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Invalid soak rules.", reload.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (!map.Tiles[cell].Equals(reload.Tiles[cell]) || map.Height[cell] != reload.Height[cell] || !map.Resources[cell].Equals(reload.Resources[cell]))
					throw new InvalidDataException("Soak changed terrain during save/reload.");
			Console.WriteLine(path);
		}
	}
}
