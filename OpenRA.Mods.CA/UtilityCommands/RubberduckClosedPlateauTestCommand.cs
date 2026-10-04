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
	sealed class RubberduckClosedPlateauTestCommand : IUtilityCommand
	{
		static byte[] CompleteSideFace(byte[] source, byte[] donor, int slot, out int filled)
		{
			// Only the opaque background connected to the exposed side is matte.
			// Preserve isolated dark rock and all translucent contact shadows.
			bool Matte(int p) => source[p * 4 + 3] == 255 && source[p * 4] <= 8 && source[p * 4 + 1] <= 8 && source[p * 4 + 2] <= 8;
			var result = (byte[])source.Clone(); var seen = new HashSet<int>(); var queue = new Queue<int>();
			queue.Enqueue(128 * 128 + (slot == 18 ? 16 : 111)); filled = 0;
			while (queue.Count != 0)
			{
				var p = queue.Dequeue(); if (!seen.Add(p) || !Matte(p)) continue;
				var x = p % 128; var y = p / 128;
				var u = slot == 18 ? x + 0.5 : 128 - (x + 0.5);
				if ((y + 0.5 >= Math.Max(64 - u / 2, 3 * u - 160) && y + 0.5 <= 64 + 1.25 * u) && donor[p * 4 + 3] == 255)
				{ Array.Copy(donor, p * 4, result, p * 4, 4); filled++; }
				if (x > 0) queue.Enqueue(p - 1); if (x < 127) queue.Enqueue(p + 1);
				if (y > 0) queue.Enqueue(p - 128); if (y < 255) queue.Enqueue(p + 128);
			}
			if (filled == 0) throw new InvalidDataException("No connected side matte was completed.");
			for (var p = 0; p < 128 * 256; p++)
			{
				if (source[p * 4 + 3] != result[p * 4 + 3]) throw new InvalidDataException("Side underlay changed the original alpha silhouette.");
				if (!Matte(p))
					for (var c = 0; c < 4; c++)
						if (source[p * 4 + c] != result[p * 4 + c]) throw new InvalidDataException("Side underlay changed original visible art.");
			}
			return result;
		}

		static byte[] CompleteMouthFace(byte[] source, Png atlas, int slot, out int filled)
		{
			// The exposed end is bounded by the existing vertical retaining edge,
			// the authored leaning edge and the low foot. Project one triangle of
			// the opposite authored face into that plane; do not enlarge alpha.
			var donorSlot = slot == 5 ? 0 : 5;
			var result = (byte[])source.Clone(); filled = 0;
			bool Matte(int p) => source[p * 4 + 3] == 255 && source[p * 4] <= 8 && source[p * 4 + 1] <= 8 && source[p * 4 + 2] <= 8;
			var connected = new HashSet<int>(); var queue = new Queue<int>();
			queue.Enqueue(128 * 128 + (slot == 5 ? 16 : 111));
			while (queue.Count != 0)
			{
				var p = queue.Dequeue(); if (!Matte(p) || !connected.Add(p)) continue;
				if (p % 128 > 0) queue.Enqueue(p - 1); if (p % 128 < 127) queue.Enqueue(p + 1);
				if (p >= 128) queue.Enqueue(p - 128); if (p < 128 * 255) queue.Enqueue(p + 128);
			}
			var border = new HashSet<int>();
			foreach (var pixel in connected)
				foreach (var offset in new[] { -1, 1, -128, 128 })
				{
					var neighbor = pixel + offset;
					if (neighbor < 0 || neighbor >= 128 * 256 || Math.Abs(neighbor % 128 - pixel % 128) > 1 || connected.Contains(neighbor)) continue;
					var p = neighbor * 4;
					if (source[p + 3] == 255 && Math.Max(source[p], Math.Max(source[p + 1], source[p + 2])) < 48) border.Add(neighbor);
				}
			int Sample(int x, int y)
			{
				// Keep the projected strata at their original vertical coordinate.
				// Only transparent relief pixels need a nearest opaque row sample.
				for (var dy = 0; dy <= 8; dy++)
					foreach (var row in new[] { y - dy, y + dy })
					{
						if (row < 0 || row >= 256) continue;
						for (var dx = 0; dx < 128; dx++)
							foreach (var column in new[] { x - dx, x + dx })
							{
								if (column < 0 || column >= 128) continue;
								var q = ((donorSlot / 12 * 256 + row) * atlas.Width + donorSlot % 12 * 128 + column) * 4;
								if (atlas.Data[q + 3] == 255) return q;
							}
					}
				return -1;
			}
			for (var y = 0; y < 256; y++)
				for (var x = 0; x < 128; x++)
				{
					var p = (y * 128 + x) * 4;
					var u = slot == 5 ? x + 0.5 : 128 - (x + 0.5);
					if (!connected.Contains(p / 4) && !border.Contains(p / 4)) continue;
					var c = u / 64; var b = (y + 0.5 - 64 - 3 * u) / 160;
					var sx = Math.Clamp((int)(slot == 5 ? 64 * (1 - b) : 128 - 64 * (1 - b)), 0, 127);
					var sy = Math.Clamp((int)(32 + 192 * b + 224 * c), 0, 255);
					var q = Sample(sx, sy);
					if (q < 0) continue;
					if (connected.Contains(p / 4)) Array.Copy(atlas.Data, q, result, p, 3);
					else
					{
						// Reconstruct just the opaque one-pixel black-matte AA fringe.
						// Translucent authored shadows and other rock pixels are excluded.
						var background = 1 - Math.Max(source[p], Math.Max(source[p + 1], source[p + 2])) / 48.0;
						for (var channel = 0; channel < 3; channel++) result[p + channel] = (byte)Math.Min(255, source[p + channel] + atlas.Data[q + channel] * background);
					}
					filled++;
				}
			if (filled == 0) throw new InvalidDataException("Empty projected mouth face.");
			for (var p = 0; p < 128 * 256; p++)
			{
				if (result[p * 4 + 3] != source[p * 4 + 3]) throw new InvalidDataException("Mouth projection enlarged its silhouette.");
				if (!connected.Contains(p) && !border.Contains(p))
					for (var channel = 0; channel < 3; channel++)
						if (result[p * 4 + channel] != source[p * 4 + channel]) throw new InvalidDataException("Mouth projection changed visible source art.");
			}
			return result;
		}

		static void ValidateRampRotations(Map map, CPos center)
		{
			var baseline = new RubberduckClosedPlateauPlan(map, center, -1, Array.Empty<CPos>(), true);
			for (var direction = 0; direction < 4; direction++)
			{
				var rotated = new RubberduckClosedPlateauPlan(map, center, -1, Array.Empty<CPos>(), true, direction);
				CPos Rotate(CPos c)
				{
					var offset = c - center;
					return center + RubberduckClosedPlateauPlan.RampOffset(offset.X, offset.Y, direction);
				}
				if (rotated.Patch.Count != baseline.Patch.Count || rotated.Patch.Count != 52 ||
					!rotated.Roof.SetEquals(baseline.Roof.Select(Rotate)) || !rotated.Feet.SetEquals(baseline.Feet.Select(Rotate)) ||
					!rotated.Ramps.SetEquals(baseline.Ramps.Select(Rotate)) || !rotated.Landings.SetEquals(baseline.Landings.Select(Rotate)))
					throw new InvalidDataException("Rotating the ramp changed its reservation footprint.");
				foreach (var pair in baseline.Patch)
				{
					var actual = rotated.Patch[Rotate(pair.Key)];
					if (actual.Height != pair.Value.Height || actual.Blocked != pair.Value.Blocked ||
						actual.Ramp != (pair.Value.Ramp == 0 ? 0 : direction + 1))
						throw new InvalidDataException("Rotating the ramp changed its terrain semantics.");
				}
			}
		}

		string IUtilityCommand.Name => "--rubberduck-closed-plateau-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 4 && args.Length <= 8 &&
			int.TryParse(args[3], out var n) && n >= -1 && n <= 3 && args.Skip(4).All(a => a == "editor=true" || a == "shaded=true" || a == "ramp=true" || a.StartsWith("direction=", StringComparison.Ordinal) && int.TryParse(a.Substring(10), out var d) && d >= 0 && d <= 3);
		[Desc("SOURCE OUTPUT NOTCH[-1..3] [editor=true] [shaded=true] [ramp=true] [direction=0..3]", "Exercise closed original cliffs on real height-four terrain, with reserved feet.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			RubberduckClosedCliffGeometry.ValidateSideSeams();
			var output = Path.GetFullPath(args[2]); var notch = int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
			var editor = args.Contains("editor=true"); var shaded = args.Contains("shaded=true");
			var rearRamp = args.Contains("ramp=true");
			var directionArguments = args.Where(a => a.StartsWith("direction=", StringComparison.Ordinal)).ToArray();
			if (directionArguments.Length > 1 || directionArguments.Length != 0 && !rearRamp) throw new ArgumentException("Specify direction once, with ramp=true.");
			var rampDirection = directionArguments.Length == 0 ? 0 : int.Parse(directionArguments[0].Substring(10), System.Globalization.CultureInfo.InvariantCulture);
			if (rearRamp && notch != -1) throw new ArgumentException("Ramp integration requires NOTCH=-1.");
			((IUtilityCommand)new RubberduckNativeCornerLabCommand()).Run(utility, new[] { "--rubberduck-native-corner-lab", args[1], output,
				notch < 0 ? "layers=closed" : "layers=closed-" + notch });
			using var grassStream = File.OpenRead(Path.Combine(args[1], "grassland", "grass_cliff_trans.png"));
			var grass = new Png(grassStream);
			using var cliffStream = File.OpenRead(Path.Combine(args[1], "grassland", "grassland_1x1.png"));
			var cliffAtlas = new Png(cliffStream);
			var slots = RubberduckOriginalClosedReference.Walls(notch).Select(p => p.Slot).Distinct().OrderBy(s => s).ToArray();
			var mouthSlot = rearRamp && rampDirection >= 2 ? (rampDirection == 2 ? 5 : 0) : -1;
			var artSlots = slots.Concat(mouthSlot >= 0 ? new[] { mouthSlot + 100 } : Array.Empty<int>()).ToArray();
			foreach (var artSlot in artSlots)
			{
				var slot = artSlot % 100;
				using var input = File.OpenRead(Path.Combine(output, $"original-{slot}.png"));
				var source = new Png(input); var pixels = new byte[128 * 256 * 4];
				if (artSlot >= 100)
				{
					var completed = CompleteMouthFace(source.Data, cliffAtlas, slot, out var filled);
					source = new Png(completed, SpriteFrameType.Rgba32, 128, 256);
					File.WriteAllText(Path.Combine(output, $"mouth-{slot}-provenance.txt"), $"Derived end plane: {filled} opaque matte pixels sampled from original grassland_1x1.png opposite face {(slot == 5 ? 0 : 5)} using affine end-plane coordinates, nearest opaque row sampling only at relief boundaries, and one-pixel opaque matte-fringe reconstruction. Original alpha, translucent shadows and all art outside that mask unchanged. Not an untouched original end template.\n");
				}
				if (slot == 18 || slot == 22)
				{
					var donorSlot = slot == 18 ? 4 : 3;
					var donor = new byte[128 * 256 * 4];
					for (var y = 0; y < 256; y++) Array.Copy(cliffAtlas.Data, ((donorSlot / 12 * 256 + y) * cliffAtlas.Width + donorSlot % 12 * 128) * 4, donor, y * 128 * 4, 128 * 4);
					var completed = CompleteSideFace(source.Data, donor, slot, out var filled);
					source = new Png(completed, SpriteFrameType.Rgba32, 128, 256);
					File.WriteAllText(Path.Combine(output, $"side-completion-{slot}.txt"), $"Original grassland_1x1.png {slot}, underlaid from complete authored front face {donorSlot}. {filled} opaque connected-matte pixels replaced inside the front plane only; all other original pixels preserved. No color interpolation or generated rock texture.\n");
				}
				for (var x = 0; x < 128; x++)
				{
					for (var y = 0; y < 256; y++)
					{
						var dy = y + 0.5;
						var sy = (int)Math.Floor(RubberduckClosedCliffGeometry.SourceY(slot, x + 0.5, dy));
						if (sy >= 0 && sy < 256) Array.Copy(source.Data, (sy * 128 + x) * 4, pixels, (y * 128 + x) * 4, 4);
					}
				}
				var padding = 0;
				if (slot <= 7 || slot == 16 || slot == 18 || slot == 20 || slot == 22 || slot == 24)
				{
					var layered = RubberduckNativeCornerLabCommand.LayerGrassRoof(pixels, grass, output, slot, true,
						slot == 18 || slot == 20 || slot == 22 ? slot : RubberduckNativeCornerLabCommand.FootSourceSlot(slot));
					pixels = layered.Pixels; padding = layered.TopPadding;
				}
				var png = new Png(pixels, SpriteFrameType.Rgba32, 128, 256 + padding);
				png.EmbeddedData.Add("FrameSize", $"128,{256 + padding}"); png.EmbeddedData.Add("FrameAmount", "1");
				png.EmbeddedData.Add("Offset", $"0,{-64 - padding / 2}");
				png.Save(Path.Combine(output, $"height4-{artSlot}.png"));
				using var encoded = File.OpenRead(Path.Combine(output, $"height4-{artSlot}.png"));
				if (!new Png(encoded).Data.SequenceEqual(pixels)) throw new InvalidDataException("Height-four art failed pixel roundtrip.");
			}
			File.WriteAllText(Path.Combine(output, "height4-provenance.txt"),
				"Fitted derivatives, NOT unmodified original artwork. Original source hashes and slot crops are in section-scope.txt/pieces.tsv.\n" +
				"Front column drop 224->192; rear column drop 96->64. Lateral lean retained. Original roof overlays applied after fitting. No dark-pixel keying.\n" +
				"Side-contact pinning and short-family replacement were rejected after client inspection.\n" +
				"Side pieces 18/22 reconstruct only connected opaque matte inside their front plane from original complete faces 4/3 before fitting; see side-completion-*.txt. Alpha, original visible art and shared fitting are preserved.\n" +
				"Side inward pieces use two planes meeting on y=64+x (18), y=192-x (22); 256 continuity/socket checks and monotone row checks. No local contact-pinning warp.\n" +
				"Source-grid renderer retains the authored four-pixel rear lip and culls opaque rear interiors; no pixel-color keying.\n" +
				(mouthSlot >= 0 ? "Front-mouth end plane projects the opposite original face into connected opaque matte, reconstructing only its one-pixel opaque AA border. Nearest opaque row sampling preserves strata scale; alpha and translucent shadows are unchanged. This is derived art, not an unchanged authored end template. See mouth-*-provenance.txt.\n" : "") +
				(rearRamp ? $"Experimental three-wide ramp, uphill direction {rampDirection}: 12 slope cells, 8 blocked high guards, 6 clear landing/approach cells. Existing derived retaining strips, NOT authored ramp art. Ramp end contacts still require visual acceptance.\n" : "") +
				"Real height-four clear roof, height-zero blocked one-cell foot ring. Map-local art/rules; ordinary production generation unchanged.\n");
			Game.ModData = utility.ModData;
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo[shaded ? "RUBBERDUCK-TEMPERATE-SHADED" : "RUBBERDUCK-TEMPERATE"], 82, 98);
			map.Title = "Closed Height Four " + Path.GetFileName(output); map.Author = "YMCA"; map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(80, 96)); map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			foreach (var c in map.AllCells) map.Tiles[c] = new TerrainTile(1000, 0);
			var center = new MPos(40, 56).ToCPos(map);
			ValidateRampRotations(map, center);
			var plan = new RubberduckClosedPlateauPlan(map, center, notch, Array.Empty<CPos>(), rearRamp, rampDirection);
			CPos RampCell(int x, int y) => center + RubberduckClosedPlateauPlan.RampOffset(x, y, rampDirection);
			var log = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(log)); File.WriteAllText(log, ""); File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), log);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {center + new CVec(5, -2)}\n\t\tMinimumZoomScale: 0.65\n\t\tZoomScale: 0.65\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			if (editor)
				foreach (var world in new[] { "World", "EditorWorld" })
					rules.AppendLine($"{world}:\n\tClosedPlateauEditorProbe:\n\t\tCenter: {center}\n\t\tNotch: {notch}\n\t\tRearRamp: {rearRamp.ToString().ToLowerInvariant()}\n\t\tRampDirection: {rampDirection}\n\t\tResultPath: {log}");
			var sequences = new StringBuilder();
			foreach (var slot in artSlots)
			{
				rules.AppendLine($"calibration.closedcliff{slot}:\n\tInherits: ^RubberduckTerrainDecoration\n\t-WithSpriteBody:\n\tNativeCliffBody:\n\t\tSlot: {slot % 100}\n\t\tSourceGrid: true");
				sequences.AppendLine($"calibration.closedcliff{slot}:\n\tidle:\n\t\tFilename: height4-{slot}.png\n\t\tLength: 1");
			}
			var actors = new List<MiniYamlNode>();
			void Add(string type, CPos c, string owner = "Neutral") => actors.Add(new MiniYamlNode("ClosedTest" + actors.Count,
				new ActorReference(type) { new LocationInit(c), new OwnerInit(owner) }.Save()));
			if (!editor)
			{
				var action = new PlateauTerrainEditAction(map, plan.Patch, forward => map.ActorDefinitions = forward ? plan.Actors : new List<MiniYamlNode>(), "Closed source plateau");
				action.Do(); action.Undo(); action.Do(); actors.AddRange(plan.Actors);
				var roof = plan.Roof.OrderBy(c => c.X).ThenBy(c => c.Y).ToArray();
				for (var i = 0; i < (rearRamp ? 3 : 2); i++)
				{
					if (rearRamp)
					{
						var probe = "calibration.closedramp" + i;
						var unit = i == 1 ? "Light_Infantry" : "Heavy_Tank";
						rules.AppendLine($"{probe}:\n\tInherits: {unit}\n\tRenderSprites:\n\t\tImage: {(i == 1 ? "light_infantry" : "heavytank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tPlateauMovementProbe:\n\t\tTarget: {RampCell(4, i - 3)}\n\t\tRamp: {rampDirection + 1}\n\t\tCliffTop: {RampCell(4, 0)}\n\t\tCliffBottom: {RampCell(5, 0)}\n\t\tBlockedCliffFoot: true\n\t\tValidateRampGuards: true\n\t\tResultPath: {log}");
						Add(probe, RampCell(-2, i - 3), "Multi0");
						continue;
					}
					var start = i == 0 ? roof[0] : center + new CVec(-3, 2);
					var target = i == 0 ? roof[roof.Length - 1] : center + new CVec(7, 2);
					var name = "calibration.closedprobe" + i;
					rules.AppendLine($"{name}:\n\tInherits: Heavy_Tank\n\tRenderSprites:\n\t\tImage: heavytank\n\t-ArmyFollower:\n\t-Autobattler:\n\tClosedPlateauMovementProbe:\n\t\tTarget: {target}\n\t\tHeight: {(i == 0 ? 4 : 0)}\n\t\tResultPath: {log}");
					Add(name, start, "Multi0");
				}
			}
			Add("mpspawn", new MPos(8, 24).ToCPos(map)); Add("mpspawn", new MPos(72, 88).ToCPos(map));
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "closed-height-rules"));
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString(), "closed-height-sequences"));
			var path = Path.Combine(output, "closed-height-four.oramap");
			using (var package = ZipFileLoader.Create(path))
			{
				map.Save(package);
				foreach (var slot in artSlots) package.Update($"height4-{slot}.png", File.ReadAllBytes(Path.Combine(output, $"height4-{slot}.png")));
			}
			using var saved = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(saved, path); using var reload = new Map(utility.ModData, zip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Height-four rules failed reload.", reload.InvalidCustomRulesException);
			foreach (var c in map.AllCells)
				if (map.Height[c] != reload.Height[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c])) throw new InvalidDataException("Height-four terrain changed during reload.");
			Console.WriteLine(path);
		}
	}
}
