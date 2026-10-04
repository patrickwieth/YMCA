using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class DebugMapGeneratorCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--debug-map-generator";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("OUTPUT-DIRECTORY [mode=tactical|operational|strategic] [preset=mountain-valleys|two-sides|hill-fortress|battle-nexus|arabia|water-basins|continents|archipelago|migration] [seed=N] [size=auto|128x128] " +
			"[players=8] [teams=2] [expansions=1] [resources=none|glitter|patches] " +
			"[finite=true|false] [tech=none|sparse|dense] [final-only=true|false] [validate-only=true|false] [export-map=true|false]",
			"Generate planning masks and debug PNGs without creating an OpenRA map.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			try
			{
				var options = ParseOptions(args);
				var outputDirectory = Path.GetFullPath(args[1]);
				var plan = MapPlanGenerator.Generate(options);
				var validateOnly = ValuesBooleanOption(args, "validate-only");
				if (!validateOnly)
				{
					var finalOnly = ValuesFinalOnly(args);
					if (finalOnly)
						new MapPlanDebugRenderer().SaveFinal(plan, outputDirectory);
					else
						new MapPlanDebugRenderer().SaveAll(plan, outputDirectory);
					SaveMetadata(plan, options, outputDirectory);
				}
				if (ValuesBooleanOption(args, "export-map"))
				{
					var mapPath = new RubberduckMapExporter(utility.ModData).Export(plan, options, outputDirectory);
					Console.WriteLine($"Playable Rubberduck map: {mapPath}");
				}

				Console.WriteLine($"Generated {options.Preset} seed {options.Seed} (attempt {plan.GenerationAttempt}).");
				Console.WriteLine($"Debug output: {outputDirectory}");
				foreach (var message in plan.ValidationMessages)
					Console.WriteLine($"  {message}");
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"Map generation failed: {e}");
				throw;
			}
		}

		static bool ValuesFinalOnly(string[] args) => ValuesBooleanOption(args, "final-only");

		static bool ValuesBooleanOption(string[] args, string name)
		{
			for (var i = 2; i < args.Length; i++)
				if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
					return ParseBoolean(name, args[i].Substring(args[i].IndexOf('=') + 1));
			return false;
		}

		internal static MapGenerationOptions ParseOptions(string[] args)
		{
			var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			for (var i = 2; i < args.Length; i++)
			{
				var separator = args[i].IndexOf('=');
				if (separator <= 0 || separator == args[i].Length - 1)
					throw new ArgumentException($"Invalid option '{args[i]}'. Expected key=value.");

				values[args[i].Substring(0, separator)] = args[i].Substring(separator + 1);
			}

			var options = new MapGenerationOptions();
			if (values.TryGetValue("mode", out var mode))
				options.Mode = ParseEnum<GeneratedMapMode>("mode", mode);
			if (values.TryGetValue("preset", out var preset))
				options.Preset = ParsePreset(preset);
			if (values.TryGetValue("seed", out var seed))
				options.Seed = ParseInteger("seed", seed);
			if (values.TryGetValue("players", out var players))
				options.Players = ParseInteger("players", players);
			if (values.TryGetValue("teams", out var teams))
				options.Teams = ParseInteger("teams", teams);
			if (values.TryGetValue("expansions", out var expansions))
				options.ExpansionsPerPlayer = ParseInteger("expansions", expansions);
			if (values.TryGetValue("finite", out var finite))
				options.FiniteResources = ParseBoolean("finite", finite);
			if (values.TryGetValue("resources", out var resources))
				options.Resources = ParseEnum<ResourceFieldLayout>("resources", resources);
			if (values.TryGetValue("tech", out var tech))
				options.TechBuildings = ParseEnum<TechBuildingDensity>("tech", tech);
			if (values.TryGetValue("size", out var size) && !size.Equals("auto", StringComparison.OrdinalIgnoreCase))
				ParseSize(size, options);
			else
				SetAutomaticSize(options);
			if (values.TryGetValue("final-only", out var finalOnly))
				ParseBoolean("final-only", finalOnly);
			if (values.TryGetValue("validate-only", out var validateOnly))
				ParseBoolean("validate-only", validateOnly);
			if (values.TryGetValue("export-map", out var exportMap))
				ParseBoolean("export-map", exportMap);

			foreach (var key in values.Keys)
				if (key != "mode" && key != "preset" && key != "seed" && key != "players" && key != "teams" &&
					key != "expansions" && key != "finite" && key != "resources" && key != "tech" && key != "size" &&
					key != "final-only" && key != "validate-only" && key != "export-map")
					throw new ArgumentException($"Unknown option '{key}'.");

			if (options.Preset == MapGenerationPreset.TwoSides)
			{
				options.Resources = ResourceFieldLayout.Patches;
				options.FiniteResources = false;
			}
			else if (options.Preset == MapGenerationPreset.HillFortress)
				options.Resources = ResourceFieldLayout.Glitter;
			else if (options.Preset == MapGenerationPreset.Arabia)
			{
				options.MountainLines = true;
				options.WaterPatches = false;
				options.Resources = ResourceFieldLayout.Patches;
				options.FiniteResources = false;
				options.TechBuildings = TechBuildingDensity.Sparse;
			}
			else if (options.Preset == MapGenerationPreset.WaterBasins)
			{
				options.MountainLines = false;
				options.WaterPatches = true;
				options.Resources = ResourceFieldLayout.Glitter;
				options.FiniteResources = true;
				options.TechBuildings = TechBuildingDensity.Sparse;
			}
			else if (options.Preset == MapGenerationPreset.Continents)
			{
				options.Resources = ResourceFieldLayout.Patches;
				options.FiniteResources = false;
				options.TechBuildings = TechBuildingDensity.Sparse;
			}
			else if (options.Preset == MapGenerationPreset.Archipelago)
			{
				options.Resources = ResourceFieldLayout.Patches;
				options.FiniteResources = false;
				options.TechBuildings = TechBuildingDensity.Dense;
			}
			else if (options.Preset == MapGenerationPreset.Migration)
			{
				options.Resources = ResourceFieldLayout.Glitter;
				options.FiniteResources = true;
				options.TechBuildings = TechBuildingDensity.Dense;
				var automaticSize = !values.TryGetValue("size", out var requestedSize) ||
					requestedSize.Equals("auto", StringComparison.OrdinalIgnoreCase);
				if (automaticSize && options.Width < 112)
					options.Width = options.Height = 112;
			}

			return options;
		}

		static MapGenerationPreset ParsePreset(string value)
		{
			var normalized = value.Replace("-", "", StringComparison.Ordinal);
			return ParseEnum<MapGenerationPreset>("preset", normalized);
		}

		static void SetAutomaticSize(MapGenerationOptions options)
		{
			var size = options.Players switch
			{
				2 => 80,
				3 => 80,
				4 => 96,
				5 => 112,
				6 => 112,
				7 => 128,
				8 => 128,
				9 => 144,
				10 => 144,
				11 => 160,
				12 => 160,
				16 => 192,
				_ => throw new ArgumentException("Automatic map sizing supports 2–12 or 16 players."),
			};
			options.Width = size;
			options.Height = size;
		}

		static void ParseSize(string value, MapGenerationOptions options)
		{
			var dimensions = value.Split('x', 'X');
			if (dimensions.Length != 2)
				throw new ArgumentException("Size must use WIDTHxHEIGHT, for example 128x128.");

			options.Width = ParseInteger("size width", dimensions[0]);
			options.Height = ParseInteger("size height", dimensions[1]);
		}

		static int ParseInteger(string name, string value)
		{
			if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
				throw new ArgumentException($"Invalid integer for {name}: '{value}'.");

			return result;
		}

		static bool ParseBoolean(string name, string value)
		{
			if (!bool.TryParse(value, out var result))
				throw new ArgumentException($"Invalid boolean for {name}: '{value}'. Use true or false.");

			return result;
		}

		static T ParseEnum<T>(string name, string value)
			where T : struct
		{
			if (!Enum.TryParse<T>(value, true, out var result))
				throw new ArgumentException($"Invalid value for {name}: '{value}'.");

			return result;
		}

		static void SaveMetadata(MapPlan plan, MapGenerationOptions options, string directory)
		{
			var counts = CountFeatures(plan);
			using (var writer = new StreamWriter(
				Path.Combine(directory, $"plan-players-{plan.Spawns.Count}-seed-{plan.Seed}.yaml")))
			{
				writer.WriteLine($"Mode: {options.Mode}");
				writer.WriteLine($"Archetype: {options.Archetype}");
				writer.WriteLine($"Preset: {options.Preset}");
				writer.WriteLine($"Seed: {options.Seed.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine($"GenerationAttempt: {plan.GenerationAttempt.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine($"Size: {plan.Width.ToString(CultureInfo.InvariantCulture)},{plan.Height.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine($"Players: {options.Players.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine($"Teams: {options.Teams.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine($"SymmetrySlots: {plan.SymmetrySlotCount.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine($"ExpansionsPerPlayer: {options.ExpansionsPerPlayer.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine($"Resources: {options.Resources}");
				writer.WriteLine($"FiniteResources: {options.FiniteResources}");
				writer.WriteLine($"TechBuildings: {options.TechBuildings}");
				writer.WriteLine($"MountainLines: {options.MountainLines}");
				writer.WriteLine($"WaterPatches: {options.WaterPatches}");
				writer.WriteLine("Counts:");
				foreach (var pair in counts)
					writer.WriteLine($"  {pair.Key}: {pair.Value.ToString(CultureInfo.InvariantCulture)}");
				writer.WriteLine("Validation:");
				foreach (var message in plan.ValidationMessages)
					writer.WriteLine($"  - {message}");
			}
		}

		static Dictionary<string, int> CountFeatures(MapPlan plan)
		{
			var counts = new Dictionary<string, int>
			{
				{ "MountainCells", 0 },
				{ "WaterCells", 0 },
				{ "ShallowWaterCells", 0 },
				{ "BottleneckCells", 0 },
				{ "LaunchShoreCells", 0 },
				{ "CliffShoreCells", 0 },
				{ "ResourceCells", 0 },
				{ "RichResourceCells", 0 },
				{ "ResourceGenerators", 0 },
				{ "TechBuildings", 0 },
				{ "DestructibleBridgeCells", 0 },
				{ "StrategicCheckpoints", plan.StrategicCheckpoints.Count },
			};

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (plan.TerrainAt(x, y) == PlannedTerrain.Mountain)
						counts["MountainCells"]++;
					if (plan.TerrainAt(x, y) == PlannedTerrain.Water)
						counts["WaterCells"]++;
					if (plan.TerrainAt(x, y) == PlannedTerrain.ShallowWater)
						counts["ShallowWaterCells"]++;
					if (plan.HasFeature(x, y, PlannedFeature.Bottleneck))
						counts["BottleneckCells"]++;
					if (plan.HasFeature(x, y, PlannedFeature.LaunchShore))
						counts["LaunchShoreCells"]++;
					if (plan.HasFeature(x, y, PlannedFeature.CliffShore))
						counts["CliffShoreCells"]++;
					if (plan.HasFeature(x, y, PlannedFeature.Resource))
						counts["ResourceCells"]++;
					if (plan.HasFeature(x, y, PlannedFeature.RichResource))
						counts["RichResourceCells"]++;
					if (plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
						counts["ResourceGenerators"]++;
					if (plan.HasFeature(x, y, PlannedFeature.TechBuilding))
						counts["TechBuildings"]++;
					if (plan.HasFeature(x, y, PlannedFeature.DestructibleBridge))
						counts["DestructibleBridgeCells"]++;
				}

			return counts;
		}
	}
}
