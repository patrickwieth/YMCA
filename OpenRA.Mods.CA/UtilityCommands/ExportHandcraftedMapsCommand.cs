using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class ExportHandcraftedMapsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--export-handcrafted-maps";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3 || args.Length == 4 && (args[3] == "review" || args[3] == "detail" || args[3] == "reveal" || args[3] == "reveal-satellite");

		[Desc("REFERENCE_ROOT OUTPUT [review]", "Export faithful flat handcrafted maps with logical borders and original gameplay terrain.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			Directory.CreateDirectory(args[2]);
			CheckUnmaskedMap(utility.ModData, args[2]);
			var summary = new List<object>();
			foreach (var file in Directory.GetFiles(Path.Combine(args[1], "layouts"), "*.layout.json").OrderBy(f => f))
			{
				var layout = JObject.Parse(File.ReadAllText(file));
				var slug = (string)layout["slug"];
				var original = MiniYaml.FromFile(Path.Combine(args[1], "originals", slug, "map.yaml")).ToDictionary(n => n.Key, n => n.Value);
				var size = layout["target_map_size"].Values<int>().ToArray();
				var bounds = layout["target_enclosing_bounds"].Values<int>().ToArray();
				var sourceBounds = layout["source_bounds"].Values<int>().ToArray();
				var translation = layout["cell_translation"].Values<int>().ToArray();
				using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], size[0], size[1]);
				map.CellBounds = new Rectangle(sourceBounds[0] + translation[0], sourceBounds[1] + translation[1], sourceBounds[2], sourceBounds[3]);
				map.SetBounds(new PPos(bounds[0], bounds[1]), new PPos(bounds[0] + bounds[2] - 1, bounds[1] + bounds[3] - 1));
				map.Title = original["Title"].Value + " (Handcrafted)";
				map.Author = original["Author"].Value + " / YMCA isometric conversion";
				map.RequiresMod = "ca";
				map.Categories = new[] { "Conquest", "Tactical", "Handcrafted Maps" };
				map.PlayerDefinitions = original["Players"].Nodes;
				foreach (var c in map.AllCells)
				{
					map.Tiles[c] = new TerrainTile(3992, 0);
					map.Height[c] = 0;
				}

				var navalCoast = slug == "two-sides-naval";
				var terrain = ((JArray)layout["cells"]).ToDictionary(r => new CPos((int)r[2], (int)r[3]), r => (string)r[6]);
				var starts = original["Actors"].Nodes.Where(n => n.Value.Value == "mpspawn")
					.Select(n => FieldLoader.GetValue<CPos>("Location", n.Value.ToDictionary()["Location"].Value) + new CVec(translation[0], translation[1]));
				var protectedCells = ((JArray)layout["cells"]).Where(r => (int)r[9] != 0).Select(r => new CPos((int)r[2], (int)r[3])).ToHashSet();
				var reclaimed = slug == "all-that-glitters-barren" ? HandcraftedRockTrim.Apply(terrain, starts, protectedCells) : new HashSet<CPos>();
				var overrides = new List<MiniYamlNode>();
				foreach (var row in (JArray)layout["cells"])
				{
					var c = new CPos((int)row[2], (int)row[3]);
					var type = terrain[c];
					var tile = type switch
					{
						"Clear" => slug == "dune-patrol" ? 1020 : slug == "all-that-glitters-barren" ? 1030 : 1000,
						"Rock" => navalCoast ? 14012 : 3992, "Rough" => 1040, "Beach" => 1020, "Water" or "River" => 1050,
						_ => throw new InvalidDataException("Unmapped source terrain: " + type)
					};
					map.Tiles[c] = new TerrainTile((ushort)tile, 0);
					map.Resources[c] = new ResourceTile((byte)row[9], (byte)row[10]);
					overrides.Add(new MiniYamlNode(c.ToString(), new MiniYaml(type)));
				}
				map.BaseTerrainOverrides = new MiniYaml("", overrides);

				var autoTile = new AutoTile(map.Rules.Actors["world"].TraitInfo<AutoTileInfo>());
				var updates = terrain.Keys.Where(c => autoTile.IsAutoTileTemplate(map.Tiles[c].Type))
					.Select(c => (Cell: c, Tile: autoTile.ResolveTemplate(map, c))).ToArray();
				foreach (var update in updates)
					if (!navalCoast && !RubberduckShoreTopology.IsLegacyGrassSand(update.Tile) && !RubberduckShoreTopology.IsLegacyGrassDirt(update.Tile))
						map.Tiles[update.Cell] = new TerrainTile(update.Tile, 0);

				var actors = new List<MiniYamlNode>();
				foreach (var node in original["Actors"].Nodes)
				{
					var values = node.Value.ToDictionary();
					if (values.TryGetValue("Location", out var location))
					{
						var cell = FieldLoader.GetValue<CPos>("Location", location.Value) + new CVec(translation[0], translation[1]);
						values["Location"] = new MiniYaml(cell.ToString());
					}
					actors.Add(new MiniYamlNode(node.Key, new ActorReference(node.Value.Value, values).Save()));
				}

				// Visual rock faces only: no terrain heights, new collision or modified passages.
				var rocks = terrain.Where(p => p.Value == "Rock").Select(p => p.Key).ToHashSet();
				var remainingRocks = new HashSet<CPos>(rocks);
				var smallRocks = new HashSet<CPos>();
				while (remainingRocks.Count > 0)
				{
					var first = remainingRocks.OrderBy(c => c.Y).ThenBy(c => c.X).First();
					var pending = new Queue<CPos>(); var component = new List<CPos>();
					remainingRocks.Remove(first); pending.Enqueue(first);
					while (pending.Count > 0)
					{
						var c = pending.Dequeue(); component.Add(c);
						foreach (var direction in PlateauTopology.Directions)
							if (remainingRocks.Remove(c + direction)) pending.Enqueue(c + direction);
					}
					if (component.Count <= 12) smallRocks.UnionWith(component);
				}
				rocks.ExceptWith(smallRocks);
				foreach (var c in smallRocks.OrderBy(c => c.Y).ThenBy(c => c.X))
				{
					var wetNeighbors = Enumerable.Range(-1, 3).Sum(x => Enumerable.Range(-1, 3).Count(y =>
						terrain.TryGetValue(c + new CVec(x, y), out var t) && (t == "Water" || t == "River")));
					map.Tiles[c] = new TerrainTile((ushort)(wetNeighbors >= 3 || navalCoast && wetNeighbors > 0 ? 1050 : slug == "dune-patrol" ? 1020 : slug == "all-that-glitters-barren" ? 1030 : 1000), 0);
					var hash = unchecked((uint)(c.X * 73856093 ^ c.Y * 19349663));
					hash = unchecked((hash ^ (hash >> 13)) * 1274126177);
					var variant = (hash >> 16) % 4;
					// The low cave-like source cut reads as a black hole at the beach.
					// Use its solid native rubble companion only near the sandy coast.
					if (navalCoast && variant == 3 && Enumerable.Range(-2, 5).Any(x => Enumerable.Range(-2, 5).Any(y =>
						terrain.TryGetValue(c + new CVec(x, y), out var t) && (t == "Beach" || t == "Clear")))) variant = 2;
					actors.Add(new MiniYamlNode("HandcraftedBoulder" + actors.Count, new ActorReference("terrain.handcrafted.boulder" + variant)
						{ new LocationInit(c), new OwnerInit("Neutral") }.Save()));
				}
				if (navalCoast) HandcraftedNavalShore.Apply(map, terrain);
				// Old multi-cell cliff sprites include passable roof cells between blocked
				// pixels. Join their VISUAL roofs instead of drawing a pile on every cell.
				// BaseTerrainOverrides still preserves every original passable cell.
				var roofFill = terrain.Where(p => !navalCoast && !reclaimed.Contains(p.Key) && (p.Value == "Clear" || slug == "all-that-glitters-barren" && p.Value == "Rough") && map.Resources[p.Key].Type == 0 &&
					Enumerable.Range(-1, 3).Sum(x => Enumerable.Range(-1, 3).Count(y => (x != 0 || y != 0) && rocks.Contains(p.Key + new CVec(x, y)))) >= 4)
					.Select(p => p.Key).ToArray();
				foreach (var c in roofFill) { rocks.Add(c); map.Tiles[c] = new TerrainTile(3992, 0); }
				if (slug == "dune-patrol")
					foreach (var column in rocks.Where(c => c.Y < map.CellBounds.Y + 3).GroupBy(c => c.X).ToArray())
						for (var y = map.CellBounds.Y; y <= column.Max(c => c.Y); y++)
						{
							var c = new CPos(column.Key, y);
							if (map.Resources[c].Type != 0) throw new InvalidDataException("Dune boundary roof intersects resources.");
							rocks.Add(c); map.Tiles[c] = new TerrainTile(3992, 0);
						}
				if (slug == "all-that-glitters-barren")
					foreach (var c in HandcraftedRockRoof.EnclosedCells(map, terrain, rocks, starts, reclaimed))
					{ rocks.Add(c); map.Tiles[c] = new TerrainTile(3992, 0); }
				var rockActors = navalCoast ? new List<MiniYamlNode>() : RubberduckRockCoastRenderer.Actors(map, rocks, "HandcraftedRock",
					c => rocks.Contains(c) ? (ushort)3992 : !navalCoast || terrain.TryGetValue(c, out var t) && (t == "Water" || t == "River") ? (ushort)1050 : (ushort)1000,
					naturalJoints: true, clipMapEdge: true, lowRearEnds: true, allowPointContacts: true);
				if (navalCoast)
					foreach (var c in rocks.OrderBy(c => c.Y).ThenBy(c => c.X))
						{
							var mask = 0;
							for (var direction = 0; direction < 4; direction++)
								if (terrain.TryGetValue(c + PlateauTopology.Directions[direction], out var t) && (t == "Water" || t == "River")) mask |= 1 << direction;
							if (mask == 0) continue;
							var variant = (int)(unchecked((uint)(c.X * 73856093 ^ c.Y * 19349663)) % 3);
							rockActors.Add(new MiniYamlNode("HandcraftedBank" + rockActors.Count,
								new ActorReference($"terrain.handcrafted.bank{mask}-{variant}") { new LocationInit(c), new OwnerInit("Neutral") }.Save()));
						}
				actors.AddRange(rockActors);
				if (navalCoast)
					actors.AddRange(HandcraftedGrassTrees.Actors(map, terrain, starts,
						original["Actors"].Nodes.Where(n => n.Value.ToDictionary().ContainsKey("Location"))
						.Select(n => FieldLoader.GetValue<CPos>("Location", n.Value.ToDictionary()["Location"].Value) + new CVec(translation[0], translation[1]))));
				map.ActorDefinitions = actors;
				using var provenanceStream = new StreamReader(utility.ModData.DefaultFileSystem.Open("ca|bits/terrain/rubberduck/derived/handcrafted/provenance.json"));
				var cuts = (JArray)JObject.Parse(provenanceStream.ReadToEnd())["cuts"];
				var definitions = new StringBuilder(); var sequences = new StringBuilder();
				for (var i = 0; i < cuts.Count; i++)
				{
					definitions.Append($"terrain.handcrafted.boulder{i}:\n\tInherits: ^RubberduckTerrainDecoration\n");
					sequences.Append($"terrain.handcrafted.boulder{i}:\n\tidle:\n\t\tFilename: bits/terrain/rubberduck/derived/handcrafted/{cuts[i]["file"]}\n\t\tOffset: 0, {cuts[i]["offset"][1]}\n");
				}
				if (navalCoast)
					for (var i = 0; i < HandcraftedGrassTrees.Images.Length; i++)
						definitions.Append($"terrain.handcrafted.grasstree{i}:\n\tInherits: ^RubberduckTerrainDecoration\n\tRenderSprites:\n\t\tImage: {HandcraftedGrassTrees.Images[i]}\n\t\tPalette: handcrafted-trees\n");
				if (navalCoast)
					for (var mask = 1; mask < 16; mask++)
						for (var variant = 0; variant < 3; variant++)
						{
							var name = $"terrain.handcrafted.bank{mask}-{variant}";
							definitions.Append($"{name}:\n\tInherits: ^RubberduckTerrainDecoration\n");
							sequences.Append($"{name}:\n\tidle:\n\t\tFilename: bits/terrain/rubberduck/derived/handcrafted/banks/bank-{mask}-{variant}.png\n");
						}
				if (!navalCoast)
					foreach (var corner in new[] { 26, 30 })
						sequences.Append($"terrain.rubberduck.coast-piece-{corner}.bank:\n\tidle:\n\t\tFilename: bits/terrain/rubberduck/derived/handcrafted/closed-corners/corner-{corner}.png\n");
				// Classic scenery is indexed-color SHP art, not the new PNG terrain palette.
				// Retain the source biome's sprite filenames and palette on these maps only.
				using var decorationsStream = utility.ModData.DefaultFileSystem.Open("ca|sequences/decorations.yaml");
				var decorations = MiniYaml.FromStream(decorationsStream, "ca|sequences/decorations.yaml").ToDictionary(n => n.Key, n => n.Value);
				var sourceTileset = original["Tileset"].Value;
				var treeNames = original["Actors"].Nodes.Select(n => n.Value.Value.ToLowerInvariant())
					.Concat(navalCoast ? HandcraftedGrassTrees.Images : Array.Empty<string>())
					.Where(n => System.Text.RegularExpressions.Regex.IsMatch(n, @"^t(c)?[0-9]+(\.husk)?$"))
					.SelectMany(n => new[] { n, n.EndsWith(".husk", StringComparison.Ordinal) ? n : n + ".husk" }).Distinct().OrderBy(n => n);
				foreach (var name in treeNames)
				{
					if (utility.ModData.DefaultRules.Actors.ContainsKey(name))
						definitions.Append($"{(name.EndsWith(".husk", StringComparison.Ordinal) ? name.Substring(0, name.Length - 5).ToUpperInvariant() + ".Husk" : name.ToUpperInvariant())}:\n\tRenderSprites:\n\t\tPalette: handcrafted-trees\n");
					if (decorations.TryGetValue(name, out var sequence) && sequence.ToDictionary().TryGetValue("Defaults", out var defaults) &&
						defaults.ToDictionary().TryGetValue("TilesetFilenames", out var files) && files.ToDictionary().TryGetValue(sourceTileset, out var filename))
						sequences.Append($"{name}:\n\tDefaults:\n\t\tScale: 1.8\n\t\tTilesetFilenames:\n\t\t\tRUBBERDUCK-TEMPERATE: {filename.Value}\n");
				}
				map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString(), "handcrafted-scenery"));
				var palette = sourceTileset == "DESERT" ? "ca|bits/desert/desert.pal" : sourceTileset == "BARREN" ? "ca|bits/barren/barren.pal" : "ca|bits/palettes/temperat.pal";
				var rockRules = definitions + (navalCoast ? "" : "^RubberduckBankAlignedRearCoast:\n\tRockCoastBody:\n" +
					"\t\tOccludingTiles: 3992\n\t\tClipToMap: true\n\t\tClosedSideFaces: true\n") + "World:\n\tHandcraftedMapSupport:\n" +
					(navalCoast ? "\tRubberduckMaterialTransitions:\n\t\tAuthoredDryJoins: false\n" : "") +
					$"\tPaletteFromFile@handcrafted-trees:\n\t\tName: handcrafted-trees\n\t\tFilename: {palette}\n\t\tShadowIndex: 3, 4\n";
				map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rockRules, "handcrafted-rocks"));
				if (args.Length == 4)
				{
					var center = new CPos(map.CellBounds.X + map.CellBounds.Width / 2, map.CellBounds.Y + map.CellBounds.Height / 2);
					if (args[3] == "detail" && slug == "two-sides-naval") center = new CPos(map.CellBounds.X + 25, map.CellBounds.Y + 35);
					var oilAccess = layout["oil_ground_access"].SelectMany(a => a["from_starts"].Values<bool>()).Distinct().ToArray();
					if (oilAccess.Length > 1) throw new InvalidDataException("Mixed oil-access fixture needs per-start expectations.");
					var expectedOilAccess = oilAccess.Length == 0 || oilAccess[0] ? "true" : "false";
					var zoom = args[3] == "detail" ? "0.45" : slug == "dune-patrol" ? "0.12" : slug == "all-that-glitters-barren" ? "0.085" : "0.065";
					var revealing = args[3].StartsWith("reveal", StringComparison.Ordinal);
					var probe = revealing ? $"\tMapRevealPerformanceProbe:\n\t\tDebugReveal: {(args[3] == "reveal" ? "true" : "false")}\n" : $"\tHandcraftedRuntimeProbe:\n\t\tExpectedOilGroundAccess: {expectedOilAccess}\n";
					map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rockRules + probe + $"\tTerrainCalibrationView:\n\t\tCenter: {center}\n\t\tZoomScale: {zoom}\n\t\tMinimumZoomScale: 0.04\n\t\tScreenshotTicks: 60\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: {revealing.ToString().ToLowerInvariant()}\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: {(!revealing).ToString().ToLowerInvariant()}\n\t\tExploredMapCheckboxLocked: true\n", "handcrafted-review"));
					map.Title += " [REVIEW ONLY]";
				}

				var path = Path.Combine(args[2], "handcrafted-" + slug + ".oramap");
				using (var package = ZipFileLoader.Create(path)) map.Save(package);
				using var input = File.OpenRead(path);
				using var zip = new ZipFileLoader.ReadOnlyZipFile(input, path);
				using var reload = new Map(utility.ModData, zip);
				if (reload.InvalidCustomRules || reload.ReplacedInvalidTerrainTiles.Count != 0)
					throw new InvalidDataException("Export failed rules/terrain validation: " + slug, reload.InvalidCustomRulesException);
				var count = 0;
				foreach (var c in reload.AllCells)
				{
					if (reload.Contains(c) != terrain.ContainsKey(c)) throw new InvalidDataException("Playable footprint changed: " + c);
					if (!reload.Contains(c)) continue;
					count++;
					if (reload.CellContaining(reload.CenterOfCell(c)) != c) throw new InvalidDataException("World coordinate roundtrip failed.");
					if (reload.GetTerrainInfo(c).Type != terrain[c] || reload.Height[c] != 0 || reload.Ramp[c] != 0 || !reload.Resources[c].Equals(map.Resources[c]))
						throw new InvalidDataException("Gameplay terrain or resources changed: " + c);
				}
				// Resource depletion must restore the original ground class, not the visual tile's class.
				foreach (var sample in terrain.GroupBy(p => p.Value).Select(g => g.First()))
				{
					reload.CustomTerrain[sample.Key] = reload.Rules.TerrainInfo.GetTerrainIndex("Water");
					if (reload.GetTerrainInfo(sample.Key).Type != "Water") throw new InvalidDataException("Dynamic terrain override failed.");
					reload.CustomTerrain[sample.Key] = byte.MaxValue;
					if (reload.GetTerrainInfo(sample.Key).Type != sample.Value) throw new InvalidDataException("Original ground was lost after dynamic override removal.");
				}
				var random = new MersenneTwister(1967);
				for (var i = 0; i < 1000; i++)
				{
					if (!reload.Contains(reload.ChooseRandomCell(random)) || !reload.Contains(reload.ChooseRandomEdgeCell(random)))
						throw new InvalidDataException("Random cell escaped logical bounds.");
				}
				foreach (var c in reload.AllCells)
					if (!reload.Contains(reload.Clamp(c)) || !reload.Contains(reload.ChooseClosestEdgeCell(c)))
						throw new InvalidDataException("Clamping escaped logical bounds.");
				var middle = reload.CenterOfCell(new CPos(reload.CellBounds.X + reload.CellBounds.Width / 2, reload.CellBounds.Y + reload.CellBounds.Height / 2));
				foreach (var direction in new[] { new WVec(1024, 0, 0), new WVec(-1024, 0, 0), new WVec(0, 1024, 0), new WVec(0, -1024, 0) })
				{
					var distance = reload.DistanceToEdge(middle, direction).Length;
					var inside = middle + direction * Math.Max(0, distance - 2) / direction.Length;
					var outside = middle + direction * (distance + 4) / direction.Length;
					if (!reload.Contains(reload.CellContaining(inside)) || reload.Contains(reload.CellContaining(outside)))
						throw new InvalidDataException("Logical border ray does not match aircraft/map edge semantics.");
				}
				summary.Add(new { slug, reload.Uid, PlayableCells = count, OriginalActors = original["Actors"].Nodes.Count(), RockDecorations = rockActors.Count, Boulders = smallRocks.Count, ReclaimedRockCells = reclaimed.OrderBy(c => c.Y).ThenBy(c => c.X).Select(c => new[] { c.X, c.Y }).ToArray() });
				Console.WriteLine($"Exported and verified {slug}: {reload.Uid}, {count} cells, {rockActors.Count} rock decorations");
			}
			File.WriteAllText(Path.Combine(args[2], "validation.json"), JsonConvert.SerializeObject(summary, Formatting.Indented));
		}

		static void CheckUnmaskedMap(ModData mod, string output)
		{
			using var control = new Map(mod, (DefaultTerrain)mod.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 32, 64);
			control.SetBounds(new PPos(1, 17), new PPos(30, 62));
			control.RequiresMod = "ca";
			control.Title = "Unmasked geometry regression fixture";
			foreach (var c in control.AllCells)
			{
				var uv = c.ToMPos(control);
				if (control.Contains(c) != control.Bounds.Contains(uv.U, uv.V))
					throw new InvalidDataException("Default flat-map bounds changed.");
			}
			var path = Path.Combine(output, "unmasked-regression.oramap");
			using (var package = ZipFileLoader.Create(path)) control.Save(package);
			using (var input = File.OpenRead(path))
			using (var zip = new ZipFileLoader.ReadOnlyZipFile(input, path))
			using (var reader = new StreamReader(zip.GetStream("map.yaml")))
			{
				var yaml = reader.ReadToEnd();
				if (yaml.Contains("CellBounds:", StringComparison.Ordinal) || yaml.Contains("BaseTerrainOverrides:", StringComparison.Ordinal))
					throw new InvalidDataException("Default maps gained handcrafted-only metadata.");
			}
			File.Delete(path);
		}
	}
}
