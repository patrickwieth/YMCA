using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// A native-pixel connection fixture, deliberately separate from gameplay terrain.
	sealed class RubberduckNativeCornerLabCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-native-corner-lab";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3 || args.Length == 4 && (args[3] == "layers=bare" || args[3] == "layers=grass" || args[3] == "feet=grass" || args[3] == "layers=section" || args[3] == "layers=roof" || args[3] == "layers=closed" || Enumerable.Range(0, 4).Any(n => args[3] == "layers=closed-" + n));

		[Desc("SOURCE OUTPUT [layers=bare|layers=grass|feet=grass|layers=section|layers=roof|layers=closed|layers=closed-0..3]", "Compose complete original cliff corner/end pieces without texture warping.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var source = Path.GetFullPath(args[1]);
			var output = Path.GetFullPath(args[2]);
			var relative = Path.GetRelativePath(source, output);
			if (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
				throw new ArgumentException("Output must be outside the original source directory.");
			Directory.CreateDirectory(output);
			using var input = File.OpenRead(Path.Combine(source, "grassland", "grassland_1x1.png"));
			var sheet = new Png(input);
			if (sheet.Type != SpriteFrameType.Rgba32 || sheet.Width != 1536 || sheet.Height != 3840)
				throw new InvalidDataException("Expected the original 1536x3840 RGBA grassland sheet.");
			var closed = args.Length == 4 && args[3].StartsWith("layers=closed", StringComparison.Ordinal);
			var notch = closed && args[3] != "layers=closed" ? int.Parse(args[3].Substring("layers=closed-".Length), System.Globalization.CultureInfo.InvariantCulture) : -1;
			var roofReference = closed || args.Length == 4 && args[3] == "layers=roof";
			var section = roofReference || args.Length == 4 && args[3] == "layers=section";
			var authoredFeet = section || args.Length == 4 && args[3] == "feet=grass";
			var layerReview = args.Length == 4 && (section || !authoredFeet);
			var grassLayer = section || layerReview && args[3] == "layers=grass";
			Png grassTransitions = null;
			if (grassLayer)
			{
				using var grassStream = File.OpenRead(Path.Combine(source, "grassland", "grass_cliff_trans.png"));
				grassTransitions = new Png(grassStream);
			}
			var slots = closed ? new[] { 0, 1, 2, 5, 6, 7, 8, 9, 10, 12, 13, 14, 16, 18, 20, 22, 24, 26, 28, 30 } : new[] { 0, 1, 2, 5, 6, 7, 16, 24, 26, 30 };
			var pieces = new Dictionary<int, Png>();
			var audit = new StringBuilder("slot\tsource-slot\tsource-x\tsource-y\tkeyed-matte-pixels\n");
			foreach (var slot in slots)
			{
				var sourceSlot = authoredFeet && !RubberduckOriginalClosedReference.Rear(slot) && slot != 18 && slot != 22 ? FootSourceSlot(slot) : slot;
				var data = new byte[128 * 256 * 4];
				for (var y = 0; y < 256; y++)
					Array.Copy(sheet.Data, ((sourceSlot / 12 * 256 + y) * sheet.Width + sourceSlot % 12 * 128) * 4, data, y * 128 * 4, 128 * 4);
				if (sourceSlot != slot)
					for (var y = 0; y < 96; y++)
						for (var x = 0; x < 128; x++)
						{
							var baseline = ((slot / 12 * 256 + y) * sheet.Width + slot % 12 * 128 + x) * 4;
							// Authored variants have small baked-light differences. Their
							// roof/socket contour must retain its anchors (one alpha LSB tolerance).
							if (Math.Abs(data[(y * 128 + x) * 4 + 3] - sheet.Data[baseline + 3]) > 1)
								throw new InvalidDataException($"Authored foot variant {sourceSlot} moved the roof/socket contour of slot {slot}.");
						}
				new Png((byte[])data.Clone(), SpriteFrameType.Rgba32, 128, 256).Save(Path.Combine(output, $"original-{slot}.png"));
				var keyed = 0;
				for (var p = 0; p < data.Length; p += 4)
					// Slot 24 has no opaque black matte: its dark pixels are the
					// artist's translucent contact shadow, entirely at the low foot.
					if (!layerReview && slot != 24 && data[p + 3] != 0 && Math.Max(data[p], Math.Max(data[p + 1], data[p + 2])) <= 8)
					{
						data[p + 3] = 0;
						keyed++;
					}
				if (slot == 24)
					for (var p = 0; p < data.Length; p += 4)
						if (data[p + 3] != 0 && Math.Max(data[p], Math.Max(data[p + 1], data[p + 2])) <= 8 &&
							(data[p + 3] > (sourceSlot == 44 ? 157 : 91) || p / (128 * 4) < 192))
							throw new InvalidDataException("Outer-corner dark pixels no longer match the original translucent foot shadow.");
				var topPadding = 0;
				if (grassLayer && slot != 26 && slot != 30 && (slot == 20 || !RubberduckOriginalClosedReference.Rear(slot)))
				{
					var layered = LayerGrassRoof(data, grassTransitions, output, slot, sourceSlot: sourceSlot);
					data = layered.Pixels; topPadding = layered.TopPadding;
				}
				var png = new Png(data, SpriteFrameType.Rgba32, 128, 256 + topPadding);
				png.EmbeddedData.Add("FrameSize", $"128,{256 + topPadding}");
				png.EmbeddedData.Add("FrameAmount", "1");
				// Actor position is the source rectangle's top-left, not a claimed terrain foot.
				png.EmbeddedData.Add("Offset", $"64,{128 - topPadding / 2}");
				png.Save(Path.Combine(output, $"native-piece-{slot}.png"));
				using (var encoded = File.OpenRead(Path.Combine(output, $"native-piece-{slot}.png")))
				{
					var decoded = new Png(encoded);
					if (!decoded.Data.SequenceEqual(data) || decoded.EmbeddedData["Offset"] != png.EmbeddedData["Offset"])
						throw new InvalidDataException("Original piece changed pixels or anchor during PNG export.");
				}
				pieces.Add(slot, png);
				audit.AppendLine($"{slot}\t{sourceSlot}\t{sourceSlot % 12 * 128}\t{sourceSlot / 12 * 256}\t{keyed}");
			}
			File.WriteAllText(Path.Combine(output, "pieces.tsv"), audit.ToString());

			for (var length = 1; length <= 8; length++) RubberduckNativeCliffComposition.CreateSamples(length);
			var scenes = RubberduckNativeCliffComposition.CreateSamples(3);
			// A one-cell displacement must fail even if the same source slots are used.
			var broken = scenes["convex-south"].ToList();
			broken[broken.Count - 1] = (24, new CVec(2, 0));
			var wrongWedge = scenes["concave-north"].ToList();
			wrongWedge[wrongWedge.Count - 1] = (24, CVec.Zero);
			foreach (var invalid in new[] { broken, wrongWedge, scenes["concave-north"].Where(p => p.Slot != 16).ToList() })
			{
				var rejected = false;
				try { RubberduckNativeCliffComposition.ValidateChain(invalid); }
				catch (ArgumentException) { rejected = true; }
				if (!rejected) throw new InvalidDataException("Native connector check accepted a displaced, wrong or missing corner.");
			}
			File.WriteAllText(Path.Combine(output, "connection-results.txt"),
				"PASS 32 roof-and-foot socket chains (4 assemblies, lengths 1..8).\nPASS 3 negative cases: displaced, wrong and missing corner.\n" +
				"Geometric source connections only; not movement, heightmap or collision certification.\n");

			if (section)
			{
				var wall = new List<(int Slot, CVec Offset)>
				{
					(30, new CVec(-1, 0)), (0, CVec.Zero), (1, new CVec(1, 0)),
					(24, new CVec(2, 0)), (5, new CVec(2, -1)), (6, new CVec(2, -2)),
					(7, new CVec(2, -3)), (16, new CVec(2, -4)), (2, new CVec(3, -4)),
					(0, new CVec(4, -4)), (24, new CVec(5, -4)), (5, new CVec(5, -5)),
					(6, new CVec(5, -6)), (26, new CVec(5, -7)),
				};
				RubberduckNativeCliffComposition.ValidateChain(wall);
				scenes = new Dictionary<string, List<(int Slot, CVec Offset)>> { { "original-connected-section", wall } };
				File.WriteAllText(Path.Combine(output, "section-scope.txt"),
					"One connected 14-piece original contour: both end orientations, two convex turns, one concave turn, all six straight variants. Both roof and foot sockets checked.\n" +
					"Authored grass-foot source variants plus original roof layers, no fitting, recoloring, matte removal or fallback walls. Original opaque dark pixels deliberately retained.\n" +
					"Reference wall contour only: no inferred gameplay height, collision, ramp or complete plateau roof fill. Not a production replacement.\n" +
					"grassland_1x1.png SHA256 " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(source, "grassland", "grassland_1x1.png")))) + "\n" +
					"grass_cliff_trans.png SHA256 " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(source, "grassland", "grass_cliff_trans.png")))) + "\n");
			}
			else if (layerReview)
			{
				var wall = scenes["concave-north"];
				wall.Add((24, new CVec(4, 0)));
				wall.Add((24, new CVec(0, 4)));
				RubberduckNativeCliffComposition.ValidateChain(wall);
				scenes = new Dictionary<string, List<(int Slot, CVec Offset)>> { { "original-layered-wall", wall } };
			}
			if (closed)
			{
				scenes = new Dictionary<string, List<(int Slot, CVec Offset)>> { { "original-closed-section", RubberduckOriginalClosedReference.Walls(notch) } };
				File.WriteAllText(Path.Combine(output, "section-scope.txt"),
					$"Closed original reference, notch {notch}: {RubberduckOriginalClosedReference.Roof(notch).Length} full roof tiles.\n" +
					"PASS complete 20-edge roof boundary and all convex corner vertices; PASS four negative closure tests.\n" +
					"Rear slots 8..10,12..14,28 unchanged, including opaque dark pixels. Roof tiles must cover the rear-face interior.\n" +
					"Concave slots 16/18/20/22 use original grass overlays 12/14/16/18 respectively, at (0,32).\n" +
					"Original-pixel visual reference, not gameplay heights, low-foot collision or production acceptance. No ramps.\n");
			}
			if (closed)
				foreach (var file in new[] { "grassland_1x1.png", "grass_cliff_trans.png" })
					File.AppendAllText(Path.Combine(output, "section-scope.txt"), file + " SHA256 " +
						Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(source, "grassland", file)))) + "\n");
			Png roof = null;
			var roofCells = closed ? RubberduckOriginalClosedReference.Roof(notch) : roofReference ? SectionRoof(scenes.Single().Value) : Array.Empty<CVec>();
			if (roofReference)
			{
				var roofPath = Path.Combine(source, "grassland", "grass_tiles.png");
				using var roofStream = File.OpenRead(roofPath);
				var ground = new Png(roofStream);
				if (ground.Type != SpriteFrameType.Rgba32 || ground.Width != 1024 || ground.Height != 1536)
					throw new InvalidDataException("Unexpected original grass ground sheet.");
				var pixels = new byte[128 * 64 * 4];
				for (var y = 0; y < 64; y++) Array.Copy(ground.Data, y * ground.Width * 4, pixels, y * 128 * 4, 128 * 4);
				roof = new Png(pixels, SpriteFrameType.Rgba32, 128, 64);
				roof.EmbeddedData.Add("FrameSize", "128,64"); roof.EmbeddedData.Add("FrameAmount", "1");
				roof.EmbeddedData.Add("Offset", "64,64");
				roof.Save(Path.Combine(output, "original-roof.png"));
				using var check = File.OpenRead(Path.Combine(output, "original-roof.png"));
				var decodedRoof = new Png(check);
				if (!decodedRoof.Data.SequenceEqual(pixels) || decodedRoof.EmbeddedData["Offset"] != "64,64")
					throw new InvalidDataException("Reference roof changed source pixels or its anchor.");
				File.WriteAllText(Path.Combine(output, "roof-provenance.txt"),
					"Original grass_tiles.png rectangle (0,0,128,64), unchanged RGBA; repeated at exact isometric offsets. No generated masks, fitting or recoloring.\n" +
					"SHA256 " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(roofPath))) + "\n" +
					(closed ? $"{roofCells.Length} full roof diamonds; complete 20-edge perimeter matched to original walls, all outer corner vertices checked.\n" :
					$"{roofCells.Length} connected full roof diamonds; all eleven front contour edges covered. Rear/side boundary remains a reference cutoff, not a matched cliff closure.\n") +
					"Flat diagnostic map and decorative sprites only; no buildability, collision or gameplay elevation claim.\n");
				File.WriteAllText(Path.Combine(output, "roof-placements.tsv"), "cell-x\tcell-y\tpixel-left\tpixel-top\n" +
					string.Join("\n", roofCells.Select(c => $"{c.X}\t{c.Y}\t{64 * (c.X - c.Y)}\t{32 * (c.X + c.Y) + 32}")));
			}
			var placements = new StringBuilder("scene\tslot\tcell-x\tcell-y\tpixel-x\tpixel-y\n");
			foreach (var scene in scenes)
			{
				var width = section ? 1280 : 768;
				var imageHeight = closed ? 768 : 512;
				var imageOriginY = closed ? 256 : 128;
				var image = new byte[width * imageHeight * 4];
				for (var i = 0; i < image.Length; i += 4)
				{
					var value = ((i / 4 % width / 16 + i / 4 / width / 16) % 2 == 0) ? (byte)70 : (byte)90;
					image[i] = value; image[i + 1] = value; image[i + 2] = value; image[i + 3] = 255;
				}
				void DrawWall((int Slot, CVec Offset) piece)
				{
					var px = 64 * (piece.Offset.X - piece.Offset.Y);
					var py = 32 * (piece.Offset.X + piece.Offset.Y);
					placements.AppendLine($"{scene.Key}\t{piece.Slot}\t{piece.Offset.X}\t{piece.Offset.Y}\t{px}\t{py}");
					var png = pieces[piece.Slot];
					for (var y = 0; y < png.Height; y++)
						for (var x = 0; x < 128; x++)
						{
							var dx = px + x + 288; var dy = py + y + imageOriginY - (png.Height - 256);
							if (dx < 0 || dx >= width || dy < 0 || dy >= imageHeight) throw new InvalidDataException("Clipped native assembly fixture.");
							var s = (y * 128 + x) * 4; var d = (dy * width + dx) * 4;
							var alpha = png.Data[s + 3];
							for (var c = 0; c < 3; c++) image[d + c] = (byte)((png.Data[s + c] * alpha + image[d + c] * (255 - alpha) + 127) / 255);
						}
				}
				void DrawRoof(CVec cell)
				{
					for (var y = 0; y < 64; y++)
						for (var x = 0; x < 128; x++)
						{
							var dx = 64 * (cell.X - cell.Y) + x + 288;
							var dy = 32 * (cell.X + cell.Y) + y + imageOriginY + 32;
							if (dx < 0 || dx >= width || dy < 0 || dy >= imageHeight) throw new InvalidDataException("Clipped original roof fixture.");
							var s = (y * 128 + x) * 4; var d = (dy * width + dx) * 4;
							var alpha = roof.Data[s + 3];
							for (var channel = 0; channel < 3; channel++) image[d + channel] = (byte)((roof.Data[s + channel] * alpha + image[d + channel] * (255 - alpha) + 127) / 255);
						}
				}
				if (closed)
				{
					// Mirror the client actor painter order. Inward side pieces contain
					// both a visible face and an interior that later roof tiles cover.
					var drawOrder = roofCells.Select(c => (Slot: -1, Offset: c)).Concat(scene.Value)
						.OrderBy(p => p.Offset.X + p.Offset.Y);
					foreach (var piece in drawOrder)
						if (piece.Slot == -1) DrawRoof(piece.Offset); else DrawWall(piece);
				}
				else
				{
					foreach (var cell in roofCells) DrawRoof(cell);
					foreach (var piece in scene.Value.OrderBy(p => p.Offset.X + p.Offset.Y)) DrawWall(piece);
				}
				new Png(image, SpriteFrameType.Rgba32, width, imageHeight).Save(Path.Combine(output, scene.Key + ".png"));
			}
			File.WriteAllText(Path.Combine(output, "assemblies.tsv"), placements.ToString());
			ExportMap(utility.ModData, output, slots, scenes.Values.ToArray(), layerReview, section, roofCells, closed);
			Console.WriteLine("Native corner and end connection fixture: " + output);
		}

		static CVec[] SectionRoof(IReadOnlyList<(int Slot, CVec Offset)> wall)
		{
			// Derive the roof boundary from the actual wall placements, including
			// both original inward-cap edges. Do not maintain an independent silhouette.
			var edges = new List<(int AX, int AY, int BX, int BY)>();
			foreach (var piece in wall)
			{
				var x = 64 * (piece.Offset.X - piece.Offset.Y);
				var y = 32 * (piece.Offset.X + piece.Offset.Y);
				if (piece.Slot <= 2) edges.Add((x + 64, y + 32, x + 128, y + 64));
				else if (piece.Slot >= 5 && piece.Slot <= 7) edges.Add((x, y + 64, x + 64, y + 32));
				else if (piece.Slot == 16)
				{
					edges.Add((x, y + 64, x + 64, y + 32));
					edges.Add((x + 64, y + 32, x + 128, y + 64));
				}
				else if (piece.Slot != 24 && piece.Slot != 26 && piece.Slot != 30)
					throw new InvalidDataException("Unmatched roof reference piece.");
			}
			edges = edges.OrderBy(e => e.AX).ToList();
			if (edges.Count == 0) throw new InvalidDataException("Missing roof reference edges.");
			for (var i = 0; i < edges.Count; i++)
				if (edges[i].BX - edges[i].AX != 64 || i > 0 && (edges[i - 1].BX != edges[i].AX || edges[i - 1].BY != edges[i].AY))
					throw new InvalidDataException("Roof contour is not a continuous source-grid chain.");
			var left = edges[0].AX; var right = edges[edges.Count - 1].BX;
			var frontY = edges.Select(e => e.AY).Concat(new[] { edges[edges.Count - 1].BY }).ToArray();
			// Full authored diamonds only: no polygon clipping or invented edge pixels.
			bool Inside(int x, int y) => x >= left && x <= right && y >= -128 && y <= frontY[(x - left) / 64];
			var corners = new[] { (X: 0, Y: -32), (X: 64, Y: 0), (X: 0, Y: 32), (X: -64, Y: 0) };
			var cells = new List<CVec>();
			for (var x = -12; x <= 12; x++)
				for (var y = -12; y <= 12; y++)
				{
					var px = 64 + 64 * (x - y); var py = 64 + 32 * (x + y);
					if (corners.All(c => Inside(px + c.X, py + c.Y))) cells.Add(new CVec(x, y));
				}
			for (var edge = 0; edge < frontY.Length - 1; edge++)
			{
				var a = (X: left + edge * 64, Y: frontY[edge]);
				var b = (X: left + 64 + edge * 64, Y: frontY[edge + 1]);
				bool Has(CVec cell, (int X, int Y) p) => corners.Any(c =>
					64 + 64 * (cell.X - cell.Y) + c.X == p.X && 64 + 32 * (cell.X + cell.Y) + c.Y == p.Y);
				if (!cells.Any(c => Has(c, a) && Has(c, b))) throw new InvalidDataException("Original roof misses a front contour edge.");
			}
			var visited = new HashSet<CVec>(); var pending = new Stack<CVec>(); pending.Push(cells.First());
			while (pending.Count != 0)
			{
				var cell = pending.Pop(); if (!visited.Add(cell)) continue;
				foreach (var direction in PlateauTopology.Directions)
					if (cells.Contains(cell + direction) && !visited.Contains(cell + direction)) pending.Push(cell + direction);
			}
			if (visited.Count != cells.Count) throw new InvalidDataException("Original roof diamonds are disconnected.");
			return cells.OrderBy(c => c.X + c.Y).ThenBy(c => c.X).ToArray();
		}

		internal static int FootSourceSlot(int slot) => slot >= 0 && slot <= 2 || slot >= 5 && slot <= 7 ? slot + 36 :
			slot == 24 ? 44 : slot == 26 ? 46 : slot == 30 ? 48 : slot == 16 ? 16 :
			throw new ArgumentOutOfRangeException(nameof(slot));

		internal static (byte[] Pixels, int TopPadding) LayerGrassRoof(byte[] cliff, Png grass, string output, int slot, bool fitted = false, int? sourceSlot = null)
		{
			if (cliff.Length != 128 * 256 * 4) throw new InvalidDataException("Expected one complete 128x256 cliff piece.");
			if (grass.Type != SpriteFrameType.Rgba32 || grass.Width != 1024 || grass.Height != 1024)
				throw new InvalidDataException("Unexpected original grass transition atlas.");
			// The outer wedge has three authored antialias rows above its socket.
			// Pad the canvas rather than clipping them or shifting the artwork.
			var topPadding = slot == 24 ? 4 : 0;
			var baseline = new byte[128 * (256 + topPadding) * 4];
			Array.Copy(cliff, 0, baseline, topPadding * 128 * 4, cliff.Length);
			var result = (byte[])baseline.Clone();
			var changed = 0;
			var outside = 0;
			// Match authored edge endpoints, not the rectangular image bounds.
			// Straight faces and the inner wedge meet the cliff roof at (64,32).
			var frame = slot >= 0 && slot <= 2 ? 0 : slot >= 5 && slot <= 7 ? 2 : slot == 16 ? 12 : slot == 18 ? 14 : slot == 20 || slot == 24 ? 16 : slot == 22 ? 18 : -1;
			if (frame < 0) throw new ArgumentOutOfRangeException(nameof(slot), "Unmatched original grass transition.");
			var offsetY = slot == 24 ? -32 : 32;
			var sourceX = frame % 8 * 128;
			var sourceY = frame / 8 * 64;
			for (var y = 0; y < 64; y++)
				for (var x = 0; x < 128; x++)
				{
					var source = ((sourceY + y) * grass.Width + sourceX + x) * 4;
					var sa = grass.Data[source + 3];
					if (sa == 0) continue;
					if (offsetY + topPadding + y < 0 || offsetY + y >= 96) throw new InvalidDataException("Authored grass extends beyond the roof layer.");
					var dest = ((offsetY + topPadding + y) * 128 + x) * 4;
					var da = baseline[dest + 3];
					if (da == 0) outside++;
					var alpha = sa * 255 + da * (255 - sa);
					for (var channel = 0; channel < 3; channel++)
						result[dest + channel] = (byte)((grass.Data[source + channel] * sa * 255 + baseline[dest + channel] * da * (255 - sa) + alpha / 2) / alpha);
					result[dest + 3] = (byte)((alpha + 127) / 255);
					changed++;
				}
			if (changed == 0) throw new InvalidDataException("The original grass cap layer is empty.");
			for (var y = 0; y < 256 + topPadding; y++)
				if (y < Math.Max(0, offsetY + topPadding) || y >= offsetY + topPadding + 64)
					for (var x = 0; x < 128 * 4; x++)
						if (result[y * 128 * 4 + x] != baseline[y * 128 * 4 + x]) throw new InvalidDataException("Grass cap changed a wall or foot pixel.");
			var originSlot = sourceSlot ?? slot;
			File.WriteAllText(Path.Combine(output, $"original-layer-{slot}-provenance.txt"),
				(fitted ? $"Existing height-four fitting, sourced from grassland_1x1.png slot {originSlot}, logical connector {slot}.\n" : $"Original grassland_1x1.png slot {originSlot}, rectangle ({originSlot % 12 * 128},{originSlot / 12 * 256},128,256).\n") +
				$"Over: original grass_cliff_trans.png frame {frame}, rectangle ({sourceX},{sourceY},128,64), at (0,{offsetY}).\n" +
				$"Straight-alpha source-over only; no tint, resampling, matte removal or generated masks. {changed} source-covered pixels; {outside} over previously transparent cliff pixels.\n" +
				$"All other wall/foot pixels unchanged. Canvas top padding: {topPadding}px, compensated at render time; neither layer resampled during composition.\n" +
				(slot == 24 ? "The original outer-corner translucent contact shadow is retained, not removed as black matte. Other slots still use their existing matte treatment.\n" : "") +
				(fitted ? "Production candidate: existing cliff fitting retained; validate on actual height-four terrain before installation.\n" : "Visual assembly only: flat diagnostic map, not gameplay height/collision approval.\n"));
			return (result, topPadding);
		}

		static void ExportMap(ModData modData, string output, int[] slots, List<(int Slot, CVec Offset)>[] scenes, bool layerReview, bool section, CVec[] roofCells, bool closed)
		{
			using var map = new Map(modData, (DefaultTerrain)modData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 66, 82);
			map.Title = layerReview ? "Rubberduck Original Layer Assembly " + Path.GetFileName(output) : "Rubberduck Native Corner Connections";
			map.Author = "YMCA source calibration";
			map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(64, 80));
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var center = new MPos(32, 48).ToCPos(map);
			var camera = center + (closed ? new CVec(7, -2) : section ? new CVec(8, -3) : layerReview ? new CVec(3, 2) : new CVec(3, 0));
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {camera}\n\t\tMinimumZoomScale: 0.65\n\t\tZoomScale: {(section ? "0.65" : "0")}\n\t\tZoomSteps: -6\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n^NativeCornerPiece:\n\tAlwaysVisible:\n\tImmobile:\n\t\tOccupiesSpace: false\n\tRenderSprites:\n\tWithSpriteBody:\n\tBodyOrientation:\n\t\tQuantizedFacings: 1\n");
			var sequences = new StringBuilder();
			foreach (var slot in slots)
			{
				rules.AppendLine($"native.cornerpiece{slot}:\n\tInherits: ^NativeCornerPiece");
				sequences.AppendLine($"native.cornerpiece{slot}:\n\tidle:\n\t\tFilename: native-piece-{slot}.png\n\t\tLength: 1");
			}
			if (roofCells.Length != 0)
			{
				rules.AppendLine("native.roofpiece:\n\tInherits: ^NativeCornerPiece");
				sequences.AppendLine("native.roofpiece:\n\tidle:\n\t\tFilename: original-roof.png\n\t\tLength: 1");
			}
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "native-corner-rules"));
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString(), "native-corner-sequences"));
			// A contrasting diagnostic floor makes the original roof diamonds visible;
			// matching grass on the flat background would conceal alignment errors.
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(roofCells.Length != 0 ? (ushort)1020 : (ushort)1000, 0);
			var actors = new List<MiniYamlNode>();
			foreach (var cell in roofCells)
			{
				var actor = new ActorReference("native.roofpiece") { new LocationInit(center + cell), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("OriginalRoof" + actors.Count, actor.Save()));
			}
			var origins = layerReview ? new[] { CVec.Zero } : new[] { new CVec(-7, -1), new CVec(-1, -6), new CVec(0, 6), new CVec(6, 1) };
			for (var i = 0; i < scenes.Length; i++)
				foreach (var piece in scenes[i])
				{
					var actor = new ActorReference("native.cornerpiece" + piece.Slot)
					{ new LocationInit(center + origins[i] + piece.Offset), new OwnerInit("Neutral") };
					actors.Add(new MiniYamlNode("NativePiece" + actors.Count, actor.Save()));
				}
			foreach (var uv in new[] { new MPos(8, 24), new MPos(58, 72) })
			{
				var actor = new ActorReference("mpspawn") { new LocationInit(uv.ToCPos(map)), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("Spawn" + actors.Count, actor.Save()));
			}
			map.ActorDefinitions = actors;
			var path = Path.Combine(output, "rubberduck-native-corners.oramap");
			using (var package = ZipFileLoader.Create(path))
			{
				map.Save(package);
				if (roofCells.Length != 0) package.Update("original-roof.png", File.ReadAllBytes(Path.Combine(output, "original-roof.png")));
				foreach (var slot in slots) package.Update($"native-piece-{slot}.png", File.ReadAllBytes(Path.Combine(output, $"native-piece-{slot}.png")));
			}
			using var stream = File.OpenRead(path);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var check = new Map(modData, zip);
			if (check.InvalidCustomRules) throw new InvalidDataException("Native corner fixture failed reload.", check.InvalidCustomRulesException);
		}
	}
}
