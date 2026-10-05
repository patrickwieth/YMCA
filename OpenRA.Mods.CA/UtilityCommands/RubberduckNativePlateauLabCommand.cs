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
	sealed class RubberduckNativePlateauLabCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-native-plateau-lab";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3 || args.Length == 4 && args[3] == "vehicles=true";

		[Desc("SOURCE OUTPUT", "Test complete native front pieces, collision aprons and four real plateau ramps.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var output = Path.GetFullPath(args[2]);
			var vehicles = args.Length == 4;
			// This command also enforces source/output separation before writing anything.
			((IUtilityCommand)new RubberduckNativeCornerLabCommand()).Run(utility, new[] { "--rubberduck-native-corner-lab", args[1], output, "feet=grass" });
			using var grassStream = File.OpenRead(Path.Combine(Path.GetFullPath(args[1]), "grassland", "grass_cliff_trans.png"));
			var grassTransitions = new Png(grassStream);
			var slots = new[] { 0, 1, 2, 5, 6, 7, 16, 24, 26, 30 };
			foreach (var slot in slots)
			{
				using var stream = File.OpenRead(Path.Combine(output, $"native-piece-{slot}.png"));
				var original = new Png(stream);
				var pixels = new byte[128 * 256 * 4];
				for (var x = 0; x < 128; x++)
				{
					var southwest = slot <= 2 || slot == 30 || slot == 24 && x < 64 || slot == 16 && x >= 64;
					var roof = southwest ? (x + 0.5) / 2 : 64 - (x + 0.5) / 2;
					for (var y = 0; y < 256; y++)
					{
						// Preserve native lateral lean and complete silhouettes. Shorten only
						// the vertical drop, with matching transforms along shared sockets.
						var destY = y + 0.5;
						var sourceY = destY <= roof ? destY : destY >= roof + 192 ? destY + 32 : roof + (destY - roof) * 224 / 192;
						var sy = (int)Math.Floor(sourceY);
						if (sy < 0 || sy >= 256) continue;
						Array.Copy(original.Data, (sy * 128 + x) * 4, pixels, (y * 128 + x) * 4, 4);
					}
				}
				var topPadding = 0;
				if (slot != 26 && slot != 30)
				{
					var layered = RubberduckNativeCornerLabCommand.LayerGrassRoof(pixels, grassTransitions, output, slot, true, RubberduckNativeCornerLabCommand.FootSourceSlot(slot));
					pixels = layered.Pixels; topPadding = layered.TopPadding;
				}
				var offset = slot <= 2 ? "-64,96" : slot <= 7 ? "64,96" : slot == 24 || slot == 16 ? $"0,{128 - topPadding / 2}" : slot == 26 ? "128,64" : "-128,64";
				var fitted = new Png(pixels, SpriteFrameType.Rgba32, 128, 256 + topPadding);
				fitted.EmbeddedData.Add("FrameSize", $"128,{256 + topPadding}");
				fitted.EmbeddedData.Add("FrameAmount", "1");
				fitted.EmbeddedData.Add("Offset", offset);
				var fittedPath = Path.Combine(output, $"plateau-native-{slot}.png");
				fitted.Save(fittedPath);
				using var roundTripStream = File.OpenRead(fittedPath);
				var roundTrip = new Png(roundTripStream);
				if (!pixels.SequenceEqual(roundTrip.Data) || roundTrip.EmbeddedData["Offset"] != offset)
					throw new InvalidDataException("Layered cliff PNG roundtrip changed pixels or its anchor.");
			}

			var footProvenance = new StringBuilder("Authored grass-foot cliff variants; no pasted props, new masks or recoloring.\n");
			foreach (var slot in slots)
			{
				var sourceSlot = RubberduckNativeCornerLabCommand.FootSourceSlot(slot);
				footProvenance.AppendLine($"Logical {slot}: original grassland_1x1.png slot {sourceSlot}, rectangle ({sourceSlot % 12 * 128},{sourceSlot / 12 * 256},128,256).");
			}
			footProvenance.AppendLine("Existing height-four fitting and logical roof/foot sockets retained. Original variants include baked-light and ground-detail differences; not pixel-identical bodies.");
			footProvenance.AppendLine("Existing opaque-matte keying retained except the outer corner's verified translucent contact shadow. Existing authored grass roof layers retained.");
			footProvenance.AppendLine("grassland_1x1.png SHA256 " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(Path.GetFullPath(args[1]), "grassland", "grassland_1x1.png")))));
			footProvenance.AppendLine("Roof-layer source grass_cliff_trans.png SHA256 " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(Path.GetFullPath(args[1]), "grassland", "grass_cliff_trans.png")))));
			footProvenance.AppendLine("See docs/rubberduck-authored-cliff-feet.md for source matching, runtime evidence and remaining junction limitations.");
			File.WriteAllText(Path.Combine(output, "authored-foot-provenance.txt"), footProvenance.ToString());

			var modData = utility.ModData;
			using var map = new Map(modData, (DefaultTerrain)modData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 98, 130);
			map.Title = "Rubberduck Native Plateau Collision Lab";
			map.Author = "YMCA terrain calibration";
			map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(96, 128));
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var center = new MPos(48, 72).ToCPos(map);
			var surfaces = PlateauTopology.CalibrationPlateau(center);
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(1000, 0);
			var actors = new List<MiniYamlNode>();
			bool NativeFace(CPos cell, int direction)
			{
				var offset = cell - center;
				return direction == 0 && offset.X == 7 && Math.Abs(offset.Y) >= 3 ||
					direction == 1 && offset.Y == 7 && Math.Abs(offset.X) >= 3;
			}
			RubberduckPlateauRenderer.Apply(map, surfaces, actors, (cell, direction) => !NativeFace(cell, direction));
			var apron = new HashSet<CPos>();
			for (var lane = -8; lane <= 8; lane++)
			{
				if (Math.Abs(lane) <= 1) continue;
				apron.Add(center + new CVec(8, lane));
				apron.Add(center + new CVec(lane, 8));
			}
			foreach (var cell in apron)
			{
				if (surfaces.ContainsKey(cell)) throw new InvalidDataException("Native foot overwrites a committed surface.");
				map.Tiles[cell] = new TerrainTile(3992, 0);
			}
			// Every original surface, including all twelve lanes across four ramps,
			// must retain its height, ramp and blocking state after adding the apron.
			foreach (var pair in surfaces)
				if (map.Height[pair.Key] != pair.Value.Height || map.Ramp[pair.Key] != pair.Value.Ramp ||
					(map.Tiles[pair.Key].Type == 3992) != pair.Value.Blocked)
					throw new InvalidDataException("Native collision apron damaged the plateau or a ramp.");
			for (var direction = 0; direction < 4; direction++)
				for (var lane = -1; lane <= 1; lane++)
				{
					var approach = center + PlateauTopology.Directions[direction] * 8 + PlateauTopology.Directions[(direction + 1) % 4] * lane;
					if (map.Tiles[approach].Type != 1000) throw new InvalidDataException("Native foot narrowed a three-wide ramp approach.");
				}

			var results = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes",
				Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(results));
			File.WriteAllText(results, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), results);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {center + (vehicles ? new CVec(6, 0) : new CVec(3, 1))}\n\t\tMinimumZoomScale: {(vehicles ? "0.1" : "0.5")}\n\t\tZoomScale: {(vehicles ? 2 : 0)}\n\t\tZoomSteps: -7\n\t\tScreenshotTicks: 900\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var sequences = new StringBuilder();
			foreach (var slot in slots)
			{
				rules.AppendLine($"calibration.nativeplateau{slot}:\n\tInherits: ^RubberduckTerrainDecoration");
				sequences.AppendLine($"calibration.nativeplateau{slot}:\n\tidle:\n\t\tFilename: plateau-native-{slot}.png\n\t\tLength: 1");
			}
			void Add(string type, CPos cell, string owner = "Neutral")
			{
				var actor = new ActorReference(type) { new LocationInit(cell), new OwnerInit(owner) };
				actors.Add(new MiniYamlNode("NativeLab" + actors.Count, actor.Save()));
			}
			for (var lane = -7; lane <= 7; lane++)
			{
				if (Math.Abs(lane) < 3) continue;
				Add("calibration.nativeplateau" + (lane + 7) % 3, center + new CVec(lane, 7));
				Add("calibration.nativeplateau" + (5 + (lane + 7) % 3), center + new CVec(7, lane));
			}
			foreach (var offset in new[] { new CVec(-3, 7), new CVec(7, -3), new CVec(7, 7) })
				Add("calibration.nativeplateau24", center + offset);
			foreach (var lane in new[] { -7, 3 })
			{
				Add("calibration.nativeplateau30", center + new CVec(lane, 7));
				Add("calibration.nativeplateau26", center + new CVec(7, lane));
			}
			for (var direction = 0; direction < 4; direction++)
				for (var kind = 0; kind < 2; kind++)
				{
					var outward = PlateauTopology.Directions[direction];
					var lateral = PlateauTopology.Directions[(direction + 1) % 4] * (kind == 0 ? -1 : 1);
					var name = $"calibration.nativeprobe{direction}-{kind}";
					rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? (vehicles ? "TRUK" : "Light_Infantry") : "Heavy_Tank")}\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? (vehicles ? "truk" : "light_infantry") : "heavytank")}\n{(vehicles && kind == 0 ? "" : "\t-ArmyFollower:\n\t-Autobattler:\n")}\tPlateauMovementProbe:\n\t\tTarget: {center + outward * 2 + lateral}\n\t\tRamp: {(direction + 2) % 4 + 1}\n\t\tCliffTop: {center + outward * 7 + lateral * 5}\n\t\tCliffBottom: {center + outward * 8 + lateral * 5}\n\t\tParkingCell: {center + outward * (6 - kind) + (direction % 2 == 0 ? new CVec(0, -1) : new CVec(-1, 0))}\n\t\tParkingHeight: {1 + kind}\n\t\tParkOnRampAfterTest: {(vehicles ? "true" : "false")}\n\t\tValidateTerrainTilt: {(vehicles ? "true" : "false")}\n\t\tExpectTerrainTilt: {(kind == 1 ? "true" : "false")}\n\t\tBlockedCliffFoot: {(direction < 2 ? "true" : "false")}\n\t\tResultPath: {results}");
					if (vehicles && kind == 0)
						rules.AppendLine("\tWithTerrainContactShadow:");
					Add(name, center + outward * 9 + lateral, "Multi0");
				}
			Add("mpspawn", new MPos(8, 24).ToCPos(map));
			Add("mpspawn", new MPos(90, 120).ToCPos(map));
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "native-plateau-rules"));
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString(), "native-plateau-sequences"));
			File.WriteAllLines(Path.Combine(output, "collision-apron.tsv"), apron.OrderBy(c => c.X).ThenBy(c => c.Y).Select(c => c + "\t0\tCliff"));
			var path = Path.Combine(output, "rubberduck-native-plateau.oramap");
			using (var package = ZipFileLoader.Create(path))
			{
				map.Save(package);
				foreach (var slot in slots) package.Update($"plateau-native-{slot}.png", File.ReadAllBytes(Path.Combine(output, $"plateau-native-{slot}.png")));
			}
			using var savedStream = File.OpenRead(path);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(savedStream, path);
			using var check = new Map(modData, zip);
			if (check.InvalidCustomRules) throw new InvalidDataException("Native plateau fixture failed reload.", check.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (check.Height[cell] != map.Height[cell] || check.Ramp[cell] != map.Ramp[cell] || check.Tiles[cell].Type != map.Tiles[cell].Type)
					throw new InvalidDataException("Native plateau terrain changed during export.");
			Console.WriteLine("Native plateau with unchanged four-level ramps and explicit low foot apron: " + path);
		}
	}
}
