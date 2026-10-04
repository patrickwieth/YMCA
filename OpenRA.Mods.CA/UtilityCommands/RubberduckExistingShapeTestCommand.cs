using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckExistingShapeTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-existing-shape-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("MAP OUTPUT", "Find a supported edge extension in a generated map and export a real-editor native replan regression.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			bool Native(string type) => type.StartsWith("terrain.rubberduck.nativecliff", StringComparison.Ordinal) || type.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal);
			var output = Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
			using var stream = File.OpenRead(args[1]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
			using var map = new Map(utility.ModData, zip);
			var candidates = map.AllCells.Where(c => map.Contains(c) && map.Height[c] == 4 && map.Tiles[c].Type == 1000)
				.SelectMany(c => PlateauTopology.Directions.Take(2).Select(d => c + d))
				.Where(c => map.Contains(c) && map.Height[c] == 0 && map.Ramp[c] == 0 &&
					Enumerable.Range(1, 2).All(step => !map.Contains(c + new CVec(step, step)) || map.Height[c + new CVec(step, step)] == 0)).Distinct().ToArray();
			PlateauShapePlan plan = null; var cell = default(CPos);
			var errors = new HashSet<string>();
			var nativeAnchors = map.ActorDefinitions.Where(n => Native(n.Value.Value))
				.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).GetOrDefault<LocationInit>())
				.Where(location => location != null).Select(location => location.Value).ToHashSet();
			foreach (var candidate in candidates.OrderByDescending(c => nativeAnchors.Contains(c))
				.ThenByDescending(c => PlateauTopology.Directions.Any(d => nativeAnchors.Contains(c - d))).Take(512))
			{
				try
				{
					var candidatePlan = PlateauShapePlan.Create(map, map.ActorDefinitions, new[] { candidate }, true, Array.Empty<CPos>());
					// A supported edit may belong to a fallback-only component. Keep
					// searching: this regression specifically exercises native replanning.
					if (!candidatePlan.BeforeActors.Any(n => Native(n.Value.Value)) || candidatePlan.NativePieces == 0) continue;
					plan = candidatePlan; cell = candidate; break;
				}
				catch (InvalidOperationException e) { errors.Add(e.Message); }
			}
			if (plan == null) throw new InvalidDataException("No supported extension: " + string.Join("; ", errors));
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath)); File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			File.WriteAllText(Path.Combine(output, "preflight-results.txt"), $"PASS existing native extension at {cell}: {plan.Patch.Count} changed cells, {plan.BeforeActors.Count} old cliff objects, {plan.NativePieces} native pieces after replan.\n");
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tExistingPlateauShapeProbe:\n\t\tCell: {cell}\n\t\tResultPath: {resultPath}\nEditorWorld:\n\tExistingPlateauShapeProbe:\n\t\tCell: {cell}\n\t\tResultPath: {resultPath}\n", "existing-shape"));
			map.Title = "Existing generated plateau shape editor test";
			var path = Path.Combine(output, "rubberduck-existing-shape-test.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			Console.WriteLine($"Native extension {cell}: {plan.Patch.Count} cells, {plan.NativePieces} native pieces. Editor fixture: {path}");
		}
	}
}
