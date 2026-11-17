using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckPlateauMatrixCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-plateau-matrix";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2 && args.Length <= 4 &&
			args.Skip(2).All(a => new[] { "tactical", "operational", "strategic", "reuse" }.Contains(a));

		[Desc("OUTPUT [tactical|operational|strategic] [reuse]",  "Validate and export Mountain Valleys player/mode/seed cases in one utility process.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]);
			Directory.CreateDirectory(output);
			var report = new StringBuilder("mode\tplayers\tseed\tplateaus\tattempt\tresult\n");
			var oldReport = Path.Combine(output, "matrix.tsv");
			var attempts = File.Exists(oldReport) ? File.ReadAllLines(oldReport).Skip(1).Select(line => line.Split('\t'))
				.ToDictionary(row => $"{row[0]}-{row[1]}-{row[2]}", row => int.Parse(row[4])) : new System.Collections.Generic.Dictionary<string, int>();
			var modes = args.Skip(2).Where(a => a != "reuse").Distinct().ToArray();
			var passed = 0;
			var failed = 0;
			foreach (var mode in modes.Length > 0 ? modes : new[] { "tactical", "operational", "strategic" })
				foreach (var seed in new[] { 42, 43, 44 })
					foreach (var players in seed == 42 ? new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16 } : new[] { 4, 8, 16 })
					{
						var directory = Path.Combine(output, $"{mode}-{players}-{seed}");
						try
						{
							var options = DebugMapGeneratorCommand.ParseOptions(new[] { "--debug-map-generator", directory,
								$"mode={mode}", $"players={players}", $"seed={seed}", "preset=mountain-valleys", "size=auto", "teams=2" });
							var plan = MapPlanGenerator.Generate(options);
							if (plan.Plateaus.Count == 0) throw new InvalidDataException("No usable plateaus generated.");
							var existing = Directory.Exists(directory) ? Directory.GetFiles(directory, "*.oramap").SingleOrDefault() : null;
							if (!args.Contains("reuse") || !attempts.TryGetValue($"{mode}-{players}-{seed}", out var oldAttempt) ||
								oldAttempt != plan.GenerationAttempt || existing == null || !TerrainMatches(plan, existing, utility.ModData.Manifest.Get<MapGrid>().MaximumTerrainHeight))
								new RubberduckMapExporter(utility.ModData).Export(plan, options, directory);
							else
								Console.WriteLine($"Verified existing binary terrain/resources: {mode}/{players}/{seed}.");
							report.AppendLine($"{mode}\t{players}\t{seed}\t{plan.Plateaus.Count}\t{plan.GenerationAttempt}\tPASS");
							passed++;
						}
						catch (Exception e)
						{
							report.AppendLine($"{mode}\t{players}\t{seed}\t0\t-1\tFAIL: {e.Message.Replace('\n', ' ').Replace('\r', ' ')}");
							failed++;
						}
						File.WriteAllText(Path.Combine(output, "matrix.tsv"), report.ToString());
						Console.WriteLine($"Plateau matrix: {passed} passed, {failed} failed ({mode}/{players}/{seed}).");
					}
			if (failed != 0) throw new InvalidDataException($"Plateau matrix failed {failed} of {passed + failed} cases.");
		}

		static bool TerrainMatches(MapPlan plan, string path, int padding)
		{
			// Binary terrain equality does not prove native piece placement equality.
			// Until reuse compares the actor graph too, always re-export native contours.
			if (plan.RequireClosedPlateaus || plan.NativeCliffPieces.Count > 0) return false;
			using var archive = ZipFile.OpenRead(path);
			using var entry = archive.GetEntry("map.bin").Open();
			using var bytes = new MemoryStream();
			entry.CopyTo(bytes);
			bytes.Position = 0;
			using var reader = new BinaryReader(bytes);
			if (reader.ReadByte() != 2 || reader.ReadUInt16() != plan.Width + 2) return false;
			var height = reader.ReadUInt16();
			if (height != plan.Height + padding + 2) return false;
			var tiles = reader.ReadUInt32();
			var heights = reader.ReadUInt32();
			var resources = reader.ReadUInt32();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var index = (x + 1) * height + y + padding + 1;
					var hasSurface = plan.PlateauSurfaces.TryGetValue(new MPos(x, y), out var surface);
					var mountain = plan.TerrainAt(x, y) == PlannedTerrain.Mountain;
					bytes.Position = heights + index;
					if (reader.ReadByte() != (hasSurface ? surface.Height : mountain ? 4 : 0)) return false;
					bytes.Position = tiles + index * 3;
					var expected = hasSurface && surface.Ramp != 0 ? 14000 + surface.Ramp : mountain ? 3992 : 1000;
					if (reader.ReadUInt16() != expected) return false;
					if ((hasSurface || mountain) && reader.ReadByte() != 0) return false;
					bytes.Position = resources + index * 2;
					var rich = plan.HasFeature(x, y, PlannedFeature.RichResource);
					var ore = plan.HasFeature(x, y, PlannedFeature.Resource);
					if (reader.ReadByte() != (rich ? 2 : ore ? 1 : 0) || reader.ReadByte() != (rich ? 3 : ore ? 12 : 0)) return false;
				}
			return true;
		}
	}
}
