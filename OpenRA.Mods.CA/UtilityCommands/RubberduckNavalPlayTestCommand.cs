using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Mods.CA.MapGeneration;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckNavalPlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-naval-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3 || args.Length == 4 && int.TryParse(args[3], out var rank) && rank >= 0;
		[Desc("MAP OUTPUT [BODY_RANK=0]", "Create real shipyard/ship travel assertions on unchanged production terrain; water bodies sorted largest first.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			Directory.CreateDirectory(args[2]);
			using var stream = File.OpenRead(args[1]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
			using var map = new Map(utility.ModData, zip);
			if (!map.Tileset.StartsWith("RUBBERDUCK-", StringComparison.Ordinal) ||
				!RubberduckBridgeExporter.OnlyBridgeRules(map.RuleDefinitions))
				throw new InvalidDataException("Naval probe requires a production Rubberduck map (generated bridge rules are preserved).");
			var directory = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes");
			Directory.CreateDirectory(directory);
			var result = Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(args[2])))) + ".log");
			File.WriteAllText(result, "");
			File.WriteAllText(Path.Combine(args[2], "runtime-results-path.txt"), result);
			var rank = args.Length == 4 ? int.Parse(args[3]) : 0;
			var rules = map.RuleDefinitions?.Nodes.ToList() ?? new List<MiniYamlNode>();
			rules.AddRange(MiniYaml.FromString($"World:\n\tMapOptions:\n\t\tGameSpeed: fastest\n\t\tGameSpeedDropdownLocked: true\n\tNavalRouteProbe:\n\t\tBodyRank: {rank}\n\t\tResultPath: {result}\nPlayer:\n\tShroud:\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n", "naval-runtime"));
			map.RuleDefinitions = new MiniYaml("", rules);
			var path = Path.Combine(args[2], "rubberduck-naval-runtime.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var saved = File.OpenRead(path);
			using var savedZip = new ZipFileLoader.ReadOnlyZipFile(saved, path);
			using var reload = new Map(utility.ModData, savedZip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Invalid naval rules.", reload.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (!map.Tiles[cell].Equals(reload.Tiles[cell]) || map.Height[cell] != reload.Height[cell] || !map.Resources[cell].Equals(reload.Resources[cell]))
					throw new InvalidDataException("Naval probe changed terrain during save/reload.");
			File.WriteAllText(Path.Combine(args[2], "scope.txt"), "Locked fastest simulation speed for diagnostic throughput; travel comparisons are simulation ticks, not wall-clock seconds. Terrain/resources/actors are unchanged by export. Runtime uses actual footprint checks, production exits and unmodified locomotors. Observer/camera diagnostics are not a competitive balance proof.\n");
			Console.WriteLine(path);
		}
	}
}
