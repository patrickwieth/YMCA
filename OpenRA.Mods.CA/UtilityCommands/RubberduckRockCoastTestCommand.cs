using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckRockCoastTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-rock-coast-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3 || (args.Length == 4 && (args[3] == "occlusion-water" || args[3] == "occlusion-water-shaded" || args[3] == "legacy-water" || args[3] == "legacy-water-shaded" || args[3] == "water-variants" || args[3] == "water-variants-shaded" || args[3] == "marker-water" || args[3] == "native-water" || args[3] == "native-water-shaded" || args[3] == "editor-materials-large" || args[3] == "materials" || args[3] == "materials-shaded" || args[3] == "detail" || args[3] == "corners" || args[3] == "editor" || args[3] == "gallery" || args[3] == "gallery-shaded" || args[3] == "editor-corners" || args[3] == "editor-corners-shaded"));
		[Desc("SOURCE OUTPUT [detail|corners|editor|gallery|gallery-shaded|materials|materials-shaded|editor-corners|editor-corners-shaded|editor-materials-large|native-water|native-water-shaded|marker-water|water-variants|water-variants-shaded|legacy-water|legacy-water-shaded|occlusion-water|occlusion-water-shaded]", "Calibrate flat rock coasts and real landing-craft cargo against accessible beaches.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var source = Path.GetFullPath(args[1]); var output = Path.GetFullPath(args[2]);
			var relative = Path.GetRelativePath(source, output);
			if (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
				throw new ArgumentException("Output must be outside source assets.");
			Directory.CreateDirectory(output);
			var sourcePath = Path.Combine(source, "grassland", "rock_cliffs.png");
			using var sourceStream = File.OpenRead(sourcePath); var image = new Png(sourceStream);
			if (image.Type != SpriteFrameType.Rgba32 || image.Width != 1536 || image.Height != 2048) throw new InvalidDataException("Unexpected native cliff sheet.");
			var provenance = new StringBuilder("Unwarped original rock_cliffs.png crops; no heightmap change.\nSHA256: " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))) + "\n");
			var images = RubberduckRockCoastArt.Export(image, output, provenance);
			File.WriteAllText(Path.Combine(output, "art-provenance.txt"), provenance.ToString());
			foreach (var side in new[] { (Slot: 26, Sign: 1), (Slot: 30, Sign: -1) })
			{
				using var encoded = new MemoryStream(images[$"coast-piece-{side.Slot}.png"]);
				var decoded = new Png(encoded);
				var offset = decoded.EmbeddedData["Offset"].Split(',').Select(int.Parse).ToArray();
				// Source roof apex (64,64) must meet the rear face socket
				// at (+/-64,-64), not protrude one isometric cell beyond it.
				if (decoded.Width != 128 || decoded.Height != 256 || offset[0] != side.Sign * 64 || offset[1] - 64 != -64)
					throw new InvalidDataException("Side corner roof misses its rear-face socket.");
				for (var row = 0; row < 256; row++)
					for (var column = 0; column < 128; column++)
						for (var channel = 0; channel < 4; channel++)
							if (decoded.Data[(row * 128 + column) * 4 + channel] != image.Data[((512 + row) * image.Width + side.Slot % 12 * 128 + column) * 4 + channel])
								throw new InvalidDataException("Side corner was altered instead of aligned.");
			}
			var nativeWater = args.Length == 4 && (args[3] == "native-water" || args[3] == "native-water-shaded");
			var legacyWater = args.Length == 4 && (args[3] == "legacy-water" || args[3] == "legacy-water-shaded");
			var occlusionReview = args.Length == 4 && (args[3] == "occlusion-water" || args[3] == "occlusion-water-shaded");
			var waterVariants = occlusionReview || legacyWater || args.Length == 4 && (args[3] == "water-variants" || args[3] == "water-variants-shaded");
			var markerWater = args.Length == 4 && args[3] == "marker-water";
			var materialGallery = waterVariants || markerWater || nativeWater || args.Length == 4 && (args[3] == "materials" || args[3] == "materials-shaded");
			var shaded = args.Length == 4 && (args[3] == "occlusion-water-shaded" || args[3] == "legacy-water-shaded" || args[3] == "editor-corners-shaded" || args[3] == "gallery-shaded" || args[3] == "materials-shaded" || args[3] == "native-water-shaded" || args[3] == "water-variants-shaded");
			var large = args.Length == 4 && args[3] == "editor-materials-large";
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo[shaded ? "RUBBERDUCK-TEMPERATE-SHADED" : "RUBBERDUCK-TEMPERATE"], large ? 192 : 112, large ? 192 : 128);
			map.SetBounds(new PPos(1, 17), large ? new PPos(190, 190) : new PPos(110, 126));
			var center = new MPos(48, 64).ToCPos(map);
			var gallery = large || materialGallery || shaded || args.Length == 4 && (args[3] == "gallery" || args[3] == "editor-corners");
			foreach (var c in map.AllCells)
			{
				var radius = Math.Max(Math.Abs(c.X - center.X), Math.Abs(c.Y - center.Y));
				map.Tiles[c] = new TerrainTile((ushort)(gallery && Math.Abs(c.X - center.X) >= 6 && Math.Abs(c.Y - center.Y) >= 6 ? 1050 : radius <= 10 ? 1000 : radius <= 12 ? 1020 : 1050), 0);
			}
			map.Title = "Flat rock coast and beach cargo test"; map.Author = "YMCA terrain calibration"; map.RequiresMod = "ca";
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var actors = new List<MiniYamlNode>();
			var corners = args.Length == 4 && args[3] == "corners";
			var editor = large || args.Length == 4 && (args[3] == "editor" || args[3] == "editor-corners" || args[3] == "editor-corners-shaded");
			IEnumerable<int> Lanes(int d) => corners && d < 2 ? Enumerable.Range(4, 9).Select(l => d == 1 ? -l : l) : Enumerable.Range(4, 5);
			var banks = new List<CPos>();
			for (var d = 0; d < 4; d++)
				foreach (var lane in Lanes(d)) banks.Add(center + PlateauTopology.Directions[d] * 12 + PlateauTopology.Directions[(d + 1) % 4] * lane);
			if (gallery)
				banks = map.AllCells.Where(map.Contains).Where(c => map.Tiles[c].Type != 1050 && PlateauTopology.Directions.Any(d => map.Contains(c + d) && map.Tiles[c + d].Type == 1050))
					.Where(c => !(Math.Abs(c.X - center.X) == 12 && Math.Abs(c.Y - center.Y) <= 2 || Math.Abs(c.Y - center.Y) == 12 && Math.Abs(c.X - center.X) <= 2)).ToList();
			for (var d = 0; d < 4; d++)
			{
				var single = center + PlateauTopology.Directions[d] * 12;
				var ends = RubberduckRockCoastRenderer.Actors(map, new[] { single }, "Single")
					.Where(n => n.Value.Value.StartsWith("terrain.rubberduck.coast-abutment-", StringComparison.Ordinal)).ToArray();
				if (ends.Length != 2 || ends.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).Distinct().Count() != 2)
					throw new InvalidDataException("A one-cell coast must have two distinct endpoint sockets.");
				RubberduckRockCoastPlan.Create(map, new[] { single }, Array.Empty<CPos>());
			}
			var galleryActors = gallery ? RubberduckRockCoastRenderer.Actors(map, banks, "GalleryCoast") : new List<MiniYamlNode>();
			if (gallery && Enumerable.Range(24, 8).Any(slot => !galleryActors.Any(n => n.Value.Value == "terrain.rubberduck.coast-piece-" + slot)))
				throw new InvalidDataException("Corner gallery is missing a contour orientation.");
			var patch = RubberduckRockCoastPlan.Create(map, banks, Array.Empty<CPos>());
			var denied = false;
			try { RubberduckRockCoastPlan.Create(map, banks, new[] { banks[0] }); } catch (InvalidOperationException) { denied = true; }
			if (!denied) throw new InvalidDataException("Protected coast accepted.");
			if (!editor) foreach (var pair in patch) map.Tiles[pair.Key] = pair.Value;
			if (materialGallery)
				for (var x = -4; x <= 4; x++)
					for (var y = -4; y <= 4; y++)
					{
						var tile = x < 0 ? (y < 0 ? 1030 : 1000) : (y < 0 ? 1020 : 1040);
						if (x == -4) tile = 1010;
						if (x >= -1 && x <= 0 && y >= -1 && y <= 0) tile = 1050;
						if (waterVariants && x >= 1 && x <= 2 && y >= 1 && y <= 2) tile = 1060;
						if (legacyWater && (tile == 1050 || tile == 1060)) tile += 20;
						var frame = legacyWater && (tile == 1070 || tile == 1080) ? (byte)((x * 7 + y * 11 + 80) % 16) : (byte)0;
						map.Tiles[center + new CVec(x, y)] = new TerrainTile((ushort)tile, frame);
					}
			if (map.AllCells.Any(c => map.Height[c] != 0 || map.Ramp[c] != 0)) throw new InvalidDataException("Pseudo-height coast changed elevation.");
			var result = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(result)); File.WriteAllText(result, ""); File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var viewCenter = waterVariants ? center + new CVec(2, -2) : gallery ? center + new CVec(3, -3) : corners ? center + new CVec(12, 12) : args.Length == 4 ? center + new CVec(12, 6) : center + new CVec(3, -3);
			var zoom = waterVariants ? "0.6" : gallery ? "0.25" : corners ? "0.4" : args.Length == 4 ? "0.6" : "0.2";
			var rules = new StringBuilder($"World:\n{(nativeWater || markerWater ? "\tRubberduckMaterialTransitions:\n\t\tNativeWater: " + (markerWater ? "false" : "true") + "\n" : "")}\tTerrainCalibrationView:\n\t\tCenter: {viewCenter}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: {zoom}\n\t\tScreenshotTicks: 1800\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var sequences = new StringBuilder();
			foreach (var kind in new[] { "infantry", "tank" })
				rules.AppendLine($"calibration.coast{kind}:\n\tInherits: {(kind == "infantry" ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == "infantry" ? "light_infantry" : "heavytank")}");
			for (var d = 0; d < 4; d++)
			{
				var outward = PlateauTopology.Directions[d]; var lateral = PlateauTopology.Directions[(d + 1) % 4];
				var rock = center + outward * 12 + lateral * (gallery ? 4 : corners && d == 1 ? -6 : 6);
				rules.AppendLine($"calibration.rockcoast{d}:\n\tAlwaysVisible:\n\tImmobile:\n\t\tOccupiesSpace: false\n\tRenderSprites:\n\tWithSpriteBody:\n\tBodyOrientation:\n\t\tQuantizedFacings: 1");
				foreach (var lane in Lanes(d))
				{
					var end = corners && d < 2 ? 0 : lane == 4 ? (d < 2 ? 2 : 1) : lane == 8 ? (d < 2 ? 1 : 2) : 0;
					var name = $"calibration.rockcoast{d}-{lane}";
					rules.AppendLine($"{name}:\n\tInherits: calibration.rockcoast{d}");
					sequences.AppendLine($"{name}:\n\tidle:\n\t\tFilename: {RubberduckRockCoastArt.Name(d, Math.Abs(lane + d) % 3, end)}.png");
					actors.Add(new MiniYamlNode("Coast" + actors.Count, new ActorReference(name) { new LocationInit(center + outward * 12 + lateral * lane), new OwnerInit("Neutral") }.Save()));
				}
				var boat = $"calibration.coastboat{d}";
				rules.AppendLine($"{boat}:\n\tInherits: LST\n\tRenderSprites:\n\t\tImage: lst\n\tCargo:\n\t\tInitialUnits: calibration.coastinfantry, calibration.coasttank\n\tRockCoastProbe:\n\t\tRock: {rock}\n\t\tBoarder: {rock - outward * 2}\n\t\tBeach: {center + outward * 13}\n\t\tLand: {center + outward * 9}\n\t\tResultPath: {result}");
				actors.Add(new MiniYamlNode("Boat" + d, new ActorReference(boat) { new LocationInit(rock + outward * 3), new OwnerInit("Multi0") }.Save()));
			}
			if (waterVariants)
			{
				rules.AppendLine($"calibration.waterbboat:\n\tInherits: LST\n\tRenderSprites:\n\t\tImage: lst\n\tShoreMovementProbe:\n\t\tWater: true\n\t\tTarget: {center + new CVec(2, 2)}\n\t\tForbidden: {center + new CVec(3, 3)}\n\t\tResultPath: {result}");
				actors.Add(new MiniYamlNode("WaterBBoat", new ActorReference("calibration.waterbboat")
				{
					new LocationInit(center + new CVec(1, 1)), new OwnerInit("Multi0")
				}.Save()));
			}
			if (legacyWater)
			{
				rules.AppendLine($"calibration.wateraboat:\n\tInherits: LST\n\tRenderSprites:\n\t\tImage: lst\n\tShoreMovementProbe:\n\t\tWater: true\n\t\tTarget: {center}\n\t\tForbidden: {center + new CVec(-2, -2)}\n\t\tResultPath: {result}");
				actors.Add(new MiniYamlNode("WaterABoat", new ActorReference("calibration.wateraboat")
				{
					new LocationInit(center + new CVec(-1, -1)), new OwnerInit("Multi0")
				}.Save()));
			}
			if (corners)
				foreach (var piece in new[] { (Slot: 0, Cell: center + new CVec(12, 4)), (Slot: 1, Cell: center + new CVec(4, 12)), (Slot: 24, Cell: center + new CVec(12, 12)) })
				{
					var name = $"calibration.coastpiece{piece.Slot}";
					rules.AppendLine($"{name}:\n\tInherits: calibration.rockcoast0");
					sequences.AppendLine($"{name}:\n\tidle:\n\t\tFilename: {(piece.Slot == 24 ? "coast-piece-24" : "coast-abutment-" + piece.Slot)}.png");
					actors.Add(new MiniYamlNode("CoastCorner" + piece.Slot, new ActorReference(name) { new LocationInit(piece.Cell), new OwnerInit("Neutral") }.Save()));
				}
			foreach (var c in new[] { center + new CVec(-3, 0), center + new CVec(3, 0) })
				actors.Add(new MiniYamlNode("Spawn" + actors.Count, new ActorReference("mpspawn") { new LocationInit(c), new OwnerInit("Neutral") }.Save()));
			if (gallery)
			{
				actors.RemoveAll(n => n.Value.Value.StartsWith("calibration.rockcoast", StringComparison.Ordinal) || n.Value.Value.StartsWith("calibration.coastpiece", StringComparison.Ordinal));
				actors.AddRange(galleryActors);
				// Gallery must display the just-exported art, not a stale installed copy.
				foreach (var filename in images.Keys)
					sequences.AppendLine($"terrain.rubberduck.{Path.GetFileNameWithoutExtension(filename)}:\n\tidle:\n\t\tFilename: {filename}");
			}
			if (occlusionReview)
			{
				// Isolated diagnostic foreground roof; never apply this geometry
				// modification to a generated gameplay map.
				actors.RemoveAll(n => n.Key == "WaterBBoat");
				var roof = new Dictionary<CPos, PlateauSurface>();
				for (var x = 2; x <= 3; x++)
					for (var y = 2; y <= 3; y++) roof.Add(center + new CVec(x, y), new PlateauSurface(4));
				RubberduckPlateauRenderer.Apply(map, roof, actors);
			}
			map.ActorDefinitions = actors; map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "rock-coast-test"));
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString(), "rock-coast-art"));
			if (editor)
			{
				map.ActorDefinitions = actors.Where(n => n.Value.Value == "mpspawn").ToList();
				map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tRockCoastEditorProbe:\n\t\tResultPath: {result}\n\t\tAllCorners: {gallery}\nEditorWorld:\n\tRockCoastEditorProbe:\n\t\tResultPath: {result}\n\t\tAllCorners: {gallery}\n", "coast-editor"));
				map.SequenceDefinitions = new MiniYaml("");
			}
			var path = Path.Combine(output, "rubberduck-rock-coast-test.oramap");
			using (var package = ZipFileLoader.Create(path)) { foreach (var pair in images) package.Update(pair.Key, pair.Value); map.Save(package); }
			using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var reload = new Map(utility.ModData, zip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Coast fixture rules invalid.", reload.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (!map.Tiles[cell].Equals(reload.Tiles[cell]) || map.Height[cell] != reload.Height[cell] ||
					!map.Resources[cell].Equals(reload.Resources[cell]))
					throw new InvalidDataException("Coast fixture reload changed terrain, legacy frame indices or resources.");
			File.WriteAllText(Path.Combine(output, "preflight-results.txt"), $"PASS side corner sockets and unchanged source RGBA; protected cells; flat heightmap; {banks.Count} banks, {patch.Count} collision cells; export/reload.\n");
			Console.WriteLine(path);
		}
	}
}
