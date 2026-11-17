using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class GenerateTournamentMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--generate-tournament-map";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 5;
		[Desc("PRESET SEED PLAYERS OUTPUT", "Generate a persistent Tactical tournament map and validated map.json receipt (protocol 1).")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			if (!Enum.TryParse<MapGenerationPreset>(args[1], out var preset) || !Enum.IsDefined(typeof(MapGenerationPreset), preset))
				throw new ArgumentException("Unknown map preset.");
			var seed = int.Parse(args[2], CultureInfo.InvariantCulture);
			var players = int.Parse(args[3], CultureInfo.InvariantCulture);
			if (seed < 1 || (players != 2 && players != 4)) throw new ArgumentException("Expected a positive seed and two or four players.");
			var output = Path.GetFullPath(args[4]);
			Directory.CreateDirectory(output);
			if (Directory.GetFiles(output).Length != 0) throw new IOException("Generation output must be empty.");
			Game.ModData = utility.ModData;
			var options = DebugMapGeneratorCommand.ParseOptions(new[] { "tournament", output, "mode=Tactical", "preset=" + preset,
				"seed=" + seed, "players=" + players, "teams=2", "size=auto" });
			var path = new RubberduckMapExporter(utility.ModData).Export(MapPlanGenerator.Generate(options), options, output);
			var canonical = Path.Combine(output, "generated.oramap");
			File.Move(path, canonical);
			string uid;
			using (var stream = File.OpenRead(canonical))
			using (var package = new ZipFileLoader.ReadOnlyZipFile(stream, canonical))
			using (var map = new Map(utility.ModData, package))
			{
				if (map.InvalidCustomRules) throw new InvalidDataException("Invalid generated rules.", map.InvalidCustomRulesException);
				uid = map.Uid;
			}
			string hash;
			using (var sha = SHA256.Create())
			using (var stream = File.OpenRead(canonical))
				hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
			File.WriteAllText(Path.Combine(output, "map.json"), JsonConvert.SerializeObject(new
			{
				Protocol = 1, Preset = preset.ToString(), Seed = seed, Players = players, Uid = uid,
				Sha256 = hash, EngineVersion = utility.ModData.Manifest.Metadata.Version
			}, Formatting.Indented));
			Console.WriteLine("Tournament map: " + uid);
		}
	}
}
