using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Visual-only, cell-contained material edge blending for flat Rubberduck terrain.")]
	public class RubberduckMaterialTransitionsInfo : TraitInfo
	{
		public readonly bool NativeWater = true;
		public readonly bool AuthoredShores = true;
		public override object Create(ActorInitializer init) => new RubberduckMaterialTransitions(NativeWater, AuthoredShores);
	}

	public sealed class RubberduckMaterialTransitions : IWorldLoaded, IPostWorldLoaded, IRenderOverlay, IRender, INotifyActorDisposing
	{
		Map map;
		LegacyCliffAbutments legacyAbutments;
		internal bool LegacyAbutmentAt(CPos cell) => legacyAbutments?.At(cell) == true;
		SheetBuilder sheet;
		RubberduckMaterialLayer layer, waterLayer, secondaryWaterLayer, apronLayer;
		readonly Dictionary<int, Sprite> apronSprites = new();
		readonly Dictionary<CPos, int> apronKeys = new();
		readonly HashSet<CPos> apronCandidates = new();
		bool apronDirty;
		internal int ApronTransitionKey(CPos cell) => apronLayer?.IsFullyClipped(cell) == true ? 0 : apronKeys.TryGetValue(cell, out var key) ? key : 0;
		internal bool ApronOccluded(CPos cell) => apronLayer?.IsClipped(cell) ?? false;
		World world;
		WorldRenderer renderer;
		Sprite empty;
		readonly bool nativeWater;
		readonly bool authoredShores;
		RubberduckAuthoredShoreLayer shoreLayer;
		internal string ValidateAuthoredShoreCache() => shoreLayer?.ValidateCache() ?? "SHORE CACHE disabled";
		internal string AuthoredShoreSnapshot => shoreLayer?.Snapshot() ?? "disabled";
		internal bool AuthoredShoreAt(CPos cell) => shoreLayer?.GroundExclusion(cell) != null;
		byte[][] waterPhases, secondaryWaterPhases;
		readonly Dictionary<int, Sprite> waterSprites = new();
		public RubberduckMaterialTransitions(bool nativeWater = true, bool authoredShores = true)
		{
			this.nativeWater = nativeWater;
			this.authoredShores = authoredShores;
		}
		byte[] grass, grassB, sand, dirt, water;
		internal int CachedSpriteCount => sprites.Count;
		internal int CachedWaterSpriteCount => waterSprites.Count;
		internal bool SecondaryWaterLoaded => secondaryWaterLayer != null;
		internal bool WaterOccluded(CPos cell, bool secondary) => (secondary ? secondaryWaterLayer : waterLayer)?.IsClipped(cell) ?? false;
		readonly Dictionary<int, Sprite> sprites = new();
		readonly Dictionary<CPos, int> cellKeys = new();
		internal int TransitionKey(CPos cell) => cellKeys.TryGetValue(cell, out var key) ? key : 0;

		internal string ValidateMixedWater()
		{
			var checkedCells = 0;
			foreach (var cell in map.AllCells)
			{
				if (!map.Contains(cell) || Material(map.Tiles[cell].Type) != 3 || map.Ramp[cell] != 0) continue;
				var expected = 0;
				for (var d = 0; d < 4; d++)
				{
					var neighbor = cell + PlateauTopology.Directions[d];
					if (map.Contains(neighbor) && map.Height[cell] == map.Height[neighbor] && map.Ramp[neighbor] == 0 && Material(map.Tiles[neighbor].Type) == 7)
						expected |= 1 << d;
				}
				if ((TransitionKey(cell) >> 20 & 15) != expected)
					throw new InvalidDataException("Mixed water overlay is stale at " + cell);
				if (expected != 0) checkedCells++;
			}
			if (checkedCells == 0 || !SecondaryWaterLoaded) throw new InvalidDataException("Mixed-water fixture did not exercise a texture join.");
			return $"MIXED WATER PASS {checkedCells} source-family joins, {CachedWaterSpriteCount} cached water sprites.";
		}

		internal static void ValidateMasks()
		{
			foreach (var width in new[] { 0.16, 0.24, 0.4 })
				for (var mask = 0; mask < 16; mask++)
					for (var x = 0; x <= 32; x++)
						for (var y = 0; y <= 32; y++)
						{
							var u = x / 32.0; var v = y / 32.0;
							var a = Alpha(mask, u, v, width);
							var rotated = ((mask << 1) | (mask >> 3)) & 15;
							if (a < 0 || a > 1 || Math.Abs(a - Alpha(rotated, 1 - v, u, width)) > 0.000001 || Alpha(mask, 0.5, 0.5, width) != 0)
								throw new InvalidDataException("Material blend violates rotation, range or cell-center protection.");
						}
			for (var neighborhood = 0; neighborhood < 256; neighborhood++)
			{
				var masks = new int[3];
				for (var d = 0; d < 4; d++)
				{
					var kind = neighborhood >> (2 * d) & 3;
					if (kind != 0) masks[kind - 1] |= 1 << d;
				}
				for (var x = 0; x <= 8; x++)
					for (var y = 0; y <= 8; y++)
					{
						var w = Alpha(masks[0], x / 8.0, y / 8.0, 0.24);
						var g = Alpha(masks[1], x / 8.0, y / 8.0, 0.4) * (1 - w);
						var s = Alpha(masks[2], x / 8.0, y / 8.0, 0.4) * (1 - w - g);
						if (s < 0 || w + g + s > 1.000001) throw new InvalidDataException("Material junction weights exceed their coverage.");
					}
			}
		}

		internal static void ValidateVariantMasks()
		{
			for (var neighborhood = 0; neighborhood < 2401; neighborhood++)
			{
				var masks = new int[6];
				var value = neighborhood;
				for (var direction = 0; direction < 4; direction++, value /= 7)
					if (value % 7 != 0) masks[value % 7 - 1] |= 1 << direction;
				for (var x = 0; x <= 8; x++)
					for (var y = 0; y <= 8; y++)
					{
						var remaining = 1.0;
						for (var material = 0; material < masks.Length; material++)
							remaining *= 1 - Alpha(masks[material], x / 8.0, y / 8.0, material == 0 || material == 5 ? 0.24 : 0.4);
						if (remaining < 0 || remaining > 1 || x == 4 && y == 4 && remaining != 1)
							throw new InvalidDataException("Variant material masks changed coverage or the cell center.");
					}
			}
		}

		// Dirt is ordinary passable terrain. Do not classify the dirt-looking
		// cliff template 3992 as dirt: blending it would conceal blocked feet.
		// Legacy water keeps its saved tile/index IDs, but shares spatial phases
		// and shore masks with the corresponding marker family.
		static int Material(ushort tile) => tile == 1000 || tile == 14012 ? 1 : tile == 1020 || tile == 14011 ? 2 : tile == 1050 || tile == 1070 || tile == 14010 ? 3 : tile == 1030 ? 4 : tile == 1010 ? 5 : tile == 1040 ? 6 : tile == 1060 || tile == 1080 ? 7 : 0;
		static byte[] Frame(World world, string name)
		{
			var path = "ca|bits/terrain/rubberduck/" + name + ".png";
			using var stream = world.Map.Open(path);
			if (!new PngSheetLoader().TryParseSprite(stream, path, out var frames, out _) || frames.Length == 0 ||
				frames[0].Type != SpriteFrameType.Rgba32 || frames[0].Size.Width != 128 || frames[0].Size.Height != 64)
				throw new InvalidDataException("Expected 128x64 RGBA material frame: " + path);
			return frames[0].Data;
		}

		void IPostWorldLoaded.PostWorldLoaded(World loadedWorld, WorldRenderer wr)
		{
			if (map == null) return;
			legacyAbutments = new LegacyCliffAbutments(loadedWorld);
			legacyAbutments.Prepare();
			if (loadedWorld.Type != WorldType.Regular) return;
			// Cold clipping on the first visible frame stalled camera pans by hundreds
			// of milliseconds. Prepare the same revision-aware slices during loading;
			// editor previews and later terrain changes still invalidate normally.
			var timer = System.Diagnostics.Stopwatch.StartNew();
			var sheets = new HashSet<Sheet>();
			var count = 0;
			foreach (var actor in loadedWorld.Actors)
			{
				if (actor.OccupiesSpace == null || !map.Contains(actor.Location)) continue;
				foreach (var body in actor.TraitsImplementing<NativeCliffBody>())
					foreach (var sprite in body.Prepare(actor)) sheets.Add(sprite.Sheet);
				foreach (var body in actor.TraitsImplementing<PlateauFaceBody>())
					foreach (var sprite in body.Prepare(actor)) sheets.Add(sprite.Sheet);
				if (++count % 128 == 0) Game.ModData.LoadScreen.Display();
			}
			foreach (var atlas in sheets) atlas.GetTexture();
			Log.Write("debug", $"Rubberduck cliff preparation: {count} actors, {sheets.Count} sheets, {timer.ElapsedMilliseconds} ms.");
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			if (!world.Map.Rules.TerrainInfo.Id.StartsWith("RUBBERDUCK-", StringComparison.Ordinal)) return;
			map = world.Map;
			this.world = world;
			renderer = wr;
			grass = Frame(world, "grass_a_base" + (map.Rules.TerrainInfo.Id.EndsWith("-SHADED", StringComparison.Ordinal) ? "_shaded" : ""));
			grassB = Frame(world, "grass_b_base" + (map.Rules.TerrainInfo.Id.EndsWith("-SHADED", StringComparison.Ordinal) ? "_shaded" : ""));
			dirt = Frame(world, "dirt_a_base" + (map.Rules.TerrainInfo.Id.EndsWith("-SHADED", StringComparison.Ordinal) ? "_shaded" : ""));
			sand = Frame(world, "sand_base" + (map.Rules.TerrainInfo.Id.EndsWith("-SHADED", StringComparison.Ordinal) ? "_shaded" : ""));
			water = Frame(world, "water_marker_v01");
			if (nativeWater) waterPhases = LoadWaterPhases(world, "water_v01");
			sheet = new SheetBuilder(SheetType.BGRA, 2048);
			empty = sheet.Add(new byte[128 * 64 * 4], SpriteFrameType.Rgba32, new Size(128, 64));
			layer = new RubberduckMaterialLayer(world, wr, empty, BlendMode.Alpha, world.Type != WorldType.Editor, cell => shoreLayer?.GroundExclusion(cell));
			if (nativeWater) waterLayer = new RubberduckMaterialLayer(world, wr, empty, BlendMode.Alpha, world.Type != WorldType.Editor);
			if (nativeWater && authoredShores) shoreLayer = new RubberduckAuthoredShoreLayer(world, wr);
			foreach (var c in map.AllCells) Update(c);
			map.Tiles.CellEntryChanged += Changed;
			map.Height.CellEntryChanged += Changed;
		}

		static byte[][] LoadWaterPhases(World world, string name)
		{
			using var stream = world.Map.Open("ca|bits/terrain/rubberduck/" + name + ".png");
			var source = new OpenRA.FileFormats.Png(stream);
			if (source.Width != 768 || source.Height != 384 || source.Type != SpriteFrameType.Rgba32)
				throw new InvalidDataException("Native water must be the original 6x6 RGBA patch.");
			var phases = new byte[36][];
			var coverage = new byte[768 * 384];
			for (var py = 0; py < 6; py++)
				for (var px = 0; px < 6; px++)
				{
					var pixels = new byte[128 * 64 * 4];
					for (var y = 0; y < 64; y++)
						for (var x = 0; x < 128; x++)
							if (Math.Abs(x + 0.5 - 64) / 64 + Math.Abs(y + 0.5 - 32) / 32 <= 1)
							{
								var sourcePixel = ((py + px) * 32 + y) * 768 + 320 + 64 * (px - py) + x;
								coverage[sourcePixel]++;
								Array.Copy(source.Data, sourcePixel * 4, pixels, (y * 128 + x) * 4, 4);
							}
					phases[px + py * 6] = pixels;
				}
			// The authored patch has an antialiased outer fringe beyond its
			// geometric diamond. Crop that fringe, not the interior texture.
			for (var pixel = 0; pixel < coverage.Length; pixel++)
			{
				var inside = Math.Abs(pixel % 768 + 0.5 - 384) / 384 + Math.Abs(pixel / 768 + 0.5 - 192) / 192 <= 1;
				if (coverage[pixel] != (inside ? 1 : 0))
					throw new InvalidDataException("Native water phases leave a gap or overlap in the geometric source patch.");
			}
			return phases;
		}

		void EnsureSecondaryWater()
		{
			if (secondaryWaterLayer != null) return;
			secondaryWaterPhases = LoadWaterPhases(world, "water_v02");
			secondaryWaterLayer = new RubberduckMaterialLayer(world, renderer, empty, BlendMode.Alpha, world.Type != WorldType.Editor);
		}

		void Changed(CPos c)
		{
			apronDirty = true;
			Update(c);
			// Apron qualification depends on an elevated cell behind its blocked
			// donor. Convex feet depend on a diagonal roof: include the (2,1)
			// and (1,2) offsets so newly eligible receivers are discovered too.
			for (var direction = 0; direction < 4; direction++)
			{
				var d = PlateauTopology.Directions[direction];
				Update(c + d);
				Update(c + d * 2);
				var lateral = PlateauTopology.Directions[(direction + 1) % 4];
				Update(c + d + lateral);
				Update(c + d * 2 + lateral);
				Update(c + d + lateral * 2);
			}
		}

		void Update(CPos c)
		{
			if (!map.Tiles.Contains(c)) return;
			var shoreWater = shoreLayer?.Update(c) ?? 0;
			if (!map.Contains(c))
			{
				// A height edit can move a previously visible cell beyond the
				// projected bounds. Remove its old overlay rather than leaving it stale.
				cellKeys.Remove(c);
				layer.Update(c, (Sprite)null, null);
				waterLayer?.Update(c, (Sprite)null, null);
				secondaryWaterLayer?.Update(c, (Sprite)null, null);
				apronKeys.Remove(c);
				apronCandidates.Remove(c);
				apronLayer?.Update(c, (Sprite)null, null);
				return;
			}
			UpdateApron(c);
			var material = Material(map.Tiles[c].Type);
			var waterMask = 0; var secondaryWaterMask = 0; var grassMask = 0; var sandMask = 0; var grassBMask = 0; var dirtMask = 0;
			if ((material == 1 || material == 2 || material == 4 || material == 5 || material == 6) && map.Ramp[c] == 0)
				for (var d = 0; d < 4; d++)
				{
					var n = c + PlateauTopology.Directions[d];
					if (!map.Contains(n) || map.Height[n] != map.Height[c] || map.Ramp[n] != 0) continue;
					var neighbor = Material(map.Tiles[n].Type);
					// Rock banks meet their own native face, not a painted water gutter.
					if (neighbor == 3 && map.Tiles[c].Type < 14010 && (map.Tiles[n].Type == 1050 || map.Tiles[n].Type == 1070)) waterMask |= 1 << d;
					if (nativeWater && neighbor == 7 && map.Tiles[c].Type < 14010) secondaryWaterMask |= 1 << d;
					if (material != 1 && neighbor == 1) grassMask |= 1 << d;
					if ((material == 2 || material == 4 || material == 6) && neighbor == 5) grassBMask |= 1 << d;
					if ((material == 4 || material == 6) && neighbor == 2) sandMask |= 1 << d;
					if (material == 6 && neighbor == 4) dirtMask |= 1 << d;
				}
			// Water B also feathers into Water A. Previously only land receivers
			// were considered, leaving a hard seam between the two spatial textures.
			// This is a visual overlay only, including legacy IDs and blocked banks.
			if (nativeWater && material == 3 && map.Ramp[c] == 0)
				for (var d = 0; d < 4; d++)
				{
					var neighbor = c + PlateauTopology.Directions[d];
					if (map.Contains(neighbor) && map.Height[neighbor] == map.Height[c] && map.Ramp[neighbor] == 0 && Material(map.Tiles[neighbor].Type) == 7)
						secondaryWaterMask |= 1 << d;
				}
			// One source per neighbor: seven choices including no overlay.
			// Separate water layers avoid multiplying ground sprites by water phases.
			var key = waterMask | grassMask << 4 | sandMask << 8 | grassBMask << 12 | dirtMask << 16 | secondaryWaterMask << 20;
			if (nativeWater)
			{
				UpdateNativeWater(c, material == 3 || shoreWater == 1, waterMask);
				if (material == 7 || secondaryWaterMask != 0 || shoreWater == 2) EnsureSecondaryWater();
				if (secondaryWaterLayer != null) UpdateNativeWater(c, material == 7 || shoreWater == 2, secondaryWaterMask, true);
			}
			if (key == 0) { cellKeys.Remove(c); layer.Update(c, (Sprite)null, null); return; }
			cellKeys[c] = key;
			var groundKey = nativeWater ? key & 0xFFFF0 : key;
			layer.Update(c, groundKey == 0 ? null : SpriteFor(groundKey), null);
		}

		internal static bool IsApronDonor(Map map, CPos donor)
		{
			bool LowBlocked(CPos c) => map.Contains(c) && map.Tiles[c].Type == 3992 && map.Height[c] == 0 && map.Ramp[c] == 0;
			if (!LowBlocked(donor)) return false;
			foreach (var inward in PlateauTopology.Directions)
			{
				var raised = donor + inward;
				if (map.Contains(raised) && map.Height[raised] > 0) return true;
			}

			// The planner also completes convex feet diagonally, but only when
			// both cardinal flanks are blocked. Do not propagate through arbitrary
			// dirt patches, across water, or around a protected ramp mouth.
			for (var direction = 0; direction < 4; direction++)
			{
				var a = PlateauTopology.Directions[direction];
				var b = PlateauTopology.Directions[(direction + 1) % 4];
				if (!LowBlocked(donor + a) || !LowBlocked(donor + b)) continue;
				var raised = donor + a + b;
				if (map.Contains(raised) && map.Height[raised] == 4 && map.Ramp[raised] == 0) return true;
			}
			return false;
		}

		void UpdateApron(CPos cell)
		{
			var mask = 0;
			var tile = map.Tiles[cell].Type;
			// Dust extends onto ordinary low grass only. The blocked dirt donor
			// itself, roofs, ramps, water and rock banks are never painted over.
			if ((tile == 1000 || tile == 1010) && map.Height[cell] == 0 && map.Ramp[cell] == 0)
				for (var direction = 0; direction < 4; direction++)
				{
					var donor = cell + PlateauTopology.Directions[direction];
					if (IsApronDonor(map, donor)) mask |= 1 << direction;
				}
			if (mask == 0) apronCandidates.Remove(cell);
			else apronCandidates.Add(cell);
			// Keep visible fragments beside nearer roofs. The material layer clips
			// only covered geometry, instead of dropping the entire fringe cell.
			if (mask == 0)
			{
				apronKeys.Remove(cell);
				apronLayer?.Update(cell, (Sprite)null, null);
				return;
			}
			if (apronLayer == null) apronLayer = new RubberduckMaterialLayer(world, renderer, empty, BlendMode.Alpha, world.Type != WorldType.Editor);
			apronKeys[cell] = mask;
			apronLayer.Update(cell, ApronSpriteFor(mask), null);
		}

		// Historical conservative broad-phase oracle, retained for diagnostics only.
		bool ApronMayBeOccluded(CPos cell, bool referenceScan = false)
		{
			var reach = map.Grid.MaximumTerrainHeight + 2;
			// Only three diagonals can overlap a 128px-wide ground diamond.
			// Keep the old square scan as a diagnostic oracle, not a render path.
			for (var dx = -1; dx <= reach; dx++)
				for (var dy = referenceScan ? -1 : Math.Max(-1, dx - 1);
					dy <= (referenceScan ? reach : Math.Min(reach, dx + 1)); dy++)
				{
					if (dx + dy <= 0 || Math.Abs(dx - dy) >= 2) continue;
					var other = cell + new CVec(dx, dy);
					if (!map.Contains(other) || map.Ramp[other] > 4 || map.Height[other] == 0 && map.Ramp[other] == 0) continue;
					var surface = new PlateauSurface(map.Height[other], map.Ramp[other]);
					var minY = int.MaxValue; var maxY = int.MinValue;
					for (var corner = 0; corner < 4; corner++)
					{
						var y = 32 * (dx + dy - surface.CornerHeight(corner)) + (corner == 0 ? -32 : corner == 2 ? 32 : 0);
						minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
					}
					if (minY < 32 && maxY > -32) return true;
				}
			return false;
		}

		internal string ValidateApronOcclusion()
		{
			var cells = new List<CPos>();
			foreach (var cell in map.AllCells)
				if (map.Contains(cell))
				{
					if (ApronMayBeOccluded(cell) != ApronMayBeOccluded(cell, true))
						throw new InvalidDataException("Apron occlusion scan changed coverage at " + cell);
					cells.Add(cell);
				}
			// Both paths have already run (including JIT). Use identical cells and
			// alternate order to avoid attributing all first-pass cost to one path.
			long bandTicks = 0, squareTicks = 0;
			var watch = new System.Diagnostics.Stopwatch();
			for (var pass = 0; pass < 4; pass++)
				for (var order = 0; order < 2; order++)
				{
					var reference = (pass + order) % 2 == 0;
					watch.Restart();
					foreach (var cell in cells) ApronMayBeOccluded(cell, reference);
					watch.Stop();
					if (reference) squareTicks += watch.ElapsedTicks;
					else bandTicks += watch.ElapsedTicks;
				}
			var scale = 1000.0 / System.Diagnostics.Stopwatch.Frequency / 4;
			return FormattableString.Invariant($"APRON OCCLUSION PASS {cells.Count} cells; {apronCandidates.Count} candidates; three-band {bandTicks * scale:F2} ms; square-reference {squareTicks * scale:F2} ms per full scan (CPU only)");
		}

		internal void RefreshAprons()
		{
			if (!apronDirty) return;
			apronDirty = false;
			foreach (var cell in new List<CPos>(apronCandidates)) UpdateApron(cell);
		}

		Sprite ApronSpriteFor(int mask)
		{
			if (!apronSprites.TryGetValue(mask, out var sprite))
			{
				var pixels = Blend(0, 0, 0, 0, mask);
				for (var y = 0; y < 64; y++)
					for (var x = 0; x < 128; x++)
					{
						var at = (y * 128 + x) * 4 + 3;
						if (pixels[at] == 0) continue;
						var u = (x + 0.5) / 128 + (y + 0.5) / 64 - 0.5;
						var v = (y + 0.5) / 64 - (x + 0.5) / 128 + 0.5;
						pixels[at] = (byte)Math.Round(255 * Alpha(mask, u, v, 0.16));
					}
				sprite = sheet.Add(pixels, SpriteFrameType.Rgba32, new Size(128, 64), 1, float3.Zero);
				apronSprites.Add(mask, sprite);
				if (apronSprites.Count > 15) throw new InvalidDataException("Apron cache exceeded its mask bound.");
			}
			return sprite;
		}

		internal static int WaterPhase(CPos cell) => ((cell.X % 6 + 6) % 6) + 6 * ((cell.Y % 6 + 6) % 6);

		void UpdateNativeWater(CPos cell, bool full, int mask, bool secondary = false)
		{
			var target = secondary ? secondaryWaterLayer : waterLayer;
			if (!full && mask == 0) { target.Update(cell, (Sprite)null, null); return; }
			target.Update(cell, WaterSpriteFor(WaterPhase(cell), full ? 16 : mask, secondary), null);
		}

		Sprite WaterSpriteFor(int phase, int coverage, bool secondary = false)
		{
			var key = (phase + (secondary ? 36 : 0)) * 17 + coverage;
			if (!waterSprites.TryGetValue(key, out var sprite))
			{
				var pixels = (byte[])(secondary ? secondaryWaterPhases : waterPhases)[phase].Clone();
				if (coverage != 16)
					for (var y = 0; y < 64; y++)
						for (var x = 0; x < 128; x++)
						{
							var u = (x + 0.5) / 128 + (y + 0.5) / 64 - 0.5;
							var v = (y + 0.5) / 64 - (x + 0.5) / 128 + 0.5;
							pixels[(y * 128 + x) * 4 + 3] = (byte)Math.Round(pixels[(y * 128 + x) * 4 + 3] * Alpha(coverage, u, v, 0.24));
						}
				sprite = sheet.Add(pixels, SpriteFrameType.Rgba32, new Size(128, 64), 1, float3.Zero);
				waterSprites.Add(key, sprite);
			}
			if (waterSprites.Count > (secondaryWaterLayer == null ? 576 : 1152)) throw new InvalidDataException("Native water cache exceeded its bound.");
			return sprite;
		}

		Sprite SpriteFor(int key)
		{
			if (!sprites.TryGetValue(key, out var sprite))
			{
				// Match the depth slope of DefaultTileCache's flat terrain sprites.
				sprite = sheet.Add(Blend(key & 15, key >> 4 & 15, key >> 8 & 15, key >> 12 & 15, key >> 16 & 15), SpriteFrameType.Rgba32, new Size(128, 64), 1, float3.Zero);
				sprites.Add(key, sprite);
				if (sprites.Count > (nativeWater ? 624 : 1295)) throw new InvalidDataException("Material cache exceeded its neighborhood bound.");
			}
			return sprite;
		}

		internal string ProfileCache(bool includeSecondaryWater = false)
		{
			var timer = System.Diagnostics.Stopwatch.StartNew();
			var allocated = GC.GetAllocatedBytesForCurrentThread();
			if (nativeWater && includeSecondaryWater) EnsureSecondaryWater();
			var radix = nativeWater ? 5 : 6;
			for (var neighborhood = 1; neighborhood < (nativeWater ? 625 : 1296); neighborhood++)
			{
				var value = neighborhood; var key = 0;
				for (var direction = 0; direction < 4; direction++, value /= radix)
					if (value % radix != 0) key |= 1 << ((value % radix - (nativeWater ? 0 : 1)) * 4 + direction);
				SpriteFor(key);
			}
			if (nativeWater)
				for (var phase = 0; phase < 36; phase++)
					for (var coverage = 1; coverage <= 16; coverage++)
					{
						WaterSpriteFor(phase, coverage);
						if (secondaryWaterLayer != null) WaterSpriteFor(phase, coverage, true);
					}
			for (var mask = 1; mask < 16; mask++) ApronSpriteFor(mask);
			var sheets = 0;
			foreach (var unused in sheet.AllSheets) sheets++;
			var expectedWaterSprites = !nativeWater ? 0 : secondaryWaterLayer == null ? 576 : 1152;
			if (sprites.Count != (nativeWater ? 624 : 1295) || waterSprites.Count != expectedWaterSprites || apronSprites.Count != 15 || sheets > 4)
				throw new InvalidDataException("Material atlas packing exceeded its bound.");
			var cacheMilliseconds = timer.ElapsedMilliseconds;
			var cacheAllocations = GC.GetAllocatedBytesForCurrentThread() - allocated;
			timer.Restart();
			var cells = 0;
			foreach (var cell in map.AllCells) { Update(cell); cells++; }
			return $"{sprites.Count} ground sprites + {waterSprites.Count} water sprites + {apronSprites.Count} apron sprites; {sheets} sheets; atlas capacity {sheets * 16} MiB; precache {cacheMilliseconds} ms; thread allocations {cacheAllocations} bytes; refresh {cells} cells in {timer.ElapsedMilliseconds} ms";
		}

		static double Alpha(int mask, double u, double v, double width)
		{
			var distance = 1.0;
			if ((mask & 1) != 0) distance = Math.Min(distance, 1 - u);
			if ((mask & 2) != 0) distance = Math.Min(distance, 1 - v);
			if ((mask & 4) != 0) distance = Math.Min(distance, u);
			if ((mask & 8) != 0) distance = Math.Min(distance, v);
			// Narrow blend inside the destination diamond: cell centers and all
			// water cells remain untouched, so no decorative land bridges appear.
			var t = Math.Clamp(1 - distance / width, 0, 1);
			return t * t * (3 - 2 * t);
		}

		byte[] Blend(int waterMask, int grassMask, int sandMask, int grassBMask, int dirtMask)
		{
			var data = new byte[128 * 64 * 4];
			for (var y = 0; y < 64; y++)
				for (var x = 0; x < 128; x++)
				{
					var u = (x + 0.5) / 128 + (y + 0.5) / 64 - 0.5;
					var v = (y + 0.5) / 64 - (x + 0.5) / 128 + 0.5;
					var i = (y * 128 + x) * 4;
					// Keep the source diamond's antialiased fringe too. Clipping it at
					// the mathematical edge exposes a one-pixel line of the old terrain.
					if (grass[i + 3] == 0 && water[i + 3] == 0 && sand[i + 3] == 0 && grassB[i + 3] == 0 && dirt[i + 3] == 0) continue;
					var w = water[i + 3] == 0 ? 0 : Alpha(waterMask, u, v, 0.24);
					var g = grass[i + 3] == 0 ? 0 : Alpha(grassMask, u, v, 0.4) * (1 - w);
					var gb = grassB[i + 3] == 0 ? 0 : Alpha(grassBMask, u, v, 0.4) * Math.Max(0, 1 - w - g);
					var s = sand[i + 3] == 0 ? 0 : Alpha(sandMask, u, v, 0.4) * Math.Max(0, 1 - w - g - gb);
					var d = dirt[i + 3] == 0 ? 0 : Alpha(dirtMask, u, v, 0.4) * Math.Max(0, 1 - w - g - gb - s);
					var alpha = w + g + gb + s + d;
					if (alpha == 0) continue;
					// Source fringe RGB can contain a light matte. Extend nearby opaque
					// texture into those edge pixels instead of amplifying matte specks.
					var su = Math.Clamp(u, 0.06, 0.94); var sv = Math.Clamp(v, 0.06, 0.94);
					var sx = Math.Clamp((int)(64 * (su - sv) + 64), 0, 127);
					var sy = Math.Clamp((int)(32 * (su + sv)), 0, 63);
					var interior = (sy * 128 + sx) * 4;
					var wi = water[i + 3] < 255 ? interior : i;
					var gi = grass[i + 3] < 255 ? interior : i;
					var si = sand[i + 3] < 255 ? interior : i;
					var gbi = grassB[i + 3] < 255 ? interior : i;
					var di = dirt[i + 3] < 255 ? interior : i;
					for (var channel = 0; channel < 3; channel++) data[i + channel] = (byte)Math.Round((water[wi + channel] * w + grass[gi + channel] * g + grassB[gbi + channel] * gb + sand[si + channel] * s + dirt[di + channel] * d) / alpha);
					data[i + 3] = (byte)Math.Round(alpha * 255);
				}
			return data;
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			RefreshAprons();
			if (shoreLayer == null) layer?.Draw(wr.Viewport);
			apronLayer?.Draw(wr.Viewport);
			waterLayer?.Draw(wr.Viewport);
			secondaryWaterLayer?.Draw(wr.Viewport);
			shoreLayer?.Draw(wr.Viewport);
			if (shoreLayer != null) layer?.Draw(wr.Viewport);
		}
		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (legacyAbutments != null)
				foreach (var piece in legacyAbutments.Render(wr)) yield return piece;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr) => Array.Empty<Rectangle>();

		void INotifyActorDisposing.Disposing(Actor self)
		{
			legacyAbutments?.Dispose(); legacyAbutments = null;
			shoreLayer?.Dispose(); shoreLayer = null;
			if (map != null)
			{
				map.Tiles.CellEntryChanged -= Changed;
				map.Height.CellEntryChanged -= Changed;
			}
			layer?.Dispose(); waterLayer?.Dispose(); secondaryWaterLayer?.Dispose(); apronLayer?.Dispose(); sheet?.Dispose();
			layer = null; waterLayer = null; secondaryWaterLayer = null; apronLayer = null; sheet = null; map = null;
			world = null; renderer = null; empty = null;
			sprites.Clear(); waterSprites.Clear(); apronSprites.Clear(); apronKeys.Clear(); apronCandidates.Clear(); apronDirty = false; cellKeys.Clear(); waterPhases = null; secondaryWaterPhases = null;
		}
	}
}
