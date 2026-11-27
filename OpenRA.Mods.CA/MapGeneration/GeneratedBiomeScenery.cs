using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Cosmetic-only biome treatment. Do not consume the layout/resource RNG.
	static class GeneratedBiomeScenery
	{
		public static void Apply(ModData mod, Map map, MapPlan plan, MapGenerationOptions options, List<MiniYamlNode> actors, Func<int, int, CPos> cell)
		{
			var desert = options.Preset == MapGenerationPreset.Arabia;
			var barren = options.Preset == MapGenerationPreset.MountainValleys;
			if (!desert && !barren && options.Preset != MapGenerationPreset.TwoSides) return;
			// Custom configurations keep their explicitly chosen terrain behavior.
			if (options.Custom != null) return;
			if (desert || barren)
			{
				var overrides = map.BaseTerrainOverrides?.Nodes.ToList() ?? new List<MiniYamlNode>();
				foreach (var c in map.AllCells)
					if (map.Tiles[c].Type >= 14001 && map.Tiles[c].Type <= 14004)
						map.Tiles[c] = new TerrainTile((ushort)(map.Tiles[c].Type + (desert ? 200 : 100)), 0);
					else if (map.Tiles[c].Type == 1000 || map.Tiles[c].Type == 1010)
					{
						// Preserve buildability/speeds: sand/dirt here are a visual biome,
						// not a change from the original Clear gameplay terrain.
						overrides.Add(new MiniYamlNode(c.ToString(), new MiniYaml(map.GetTerrainInfo(c).Type)));
						var tile = (ushort)(desert ? 1020 : 1030);
						var count = ((DefaultTerrain)map.Rules.TerrainInfo).Templates[tile].TilesCount;
						map.Tiles[c] = new TerrainTile(tile, (byte)(map.Tiles[c].Index % count));
					}
				map.BaseTerrainOverrides = new MiniYaml("", overrides);
			}

			var images = desert ? new[] { "t04", "t08", "t09", "t18" } : barren ? new[] { "t01", "t03", "t05", "t16" } : HandcraftedGrassTrees.Images;
			var biome = desert ? "DESERT" : barren ? "BARREN" : "RUBBERDUCK-TEMPERATE";
			uint Hash(int x, int y) => unchecked((uint)(x * 73856093 ^ y * 19349663 ^ plan.Seed) * 1274126177);
			bool Eligible(int x, int y)
			{
				if (plan.Spawns.Any(s => Math.Max(Math.Abs(s.X-x),Math.Abs(s.Y-y)) < 12)) return false;
				for (var dx = -3; dx <= 3; dx++) for (var dy = -3; dy <= 3; dy++)
				{
					var nx=x+dx; var ny=y+dy;
					if (!plan.ContainsPlayable(nx,ny) || plan.HasFeature(nx,ny,PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding | PlannedFeature.Bottleneck | PlannedFeature.Entrance | PlannedFeature.DestructibleBridge)) return false;
					var glitter = options.Resources == ResourceFieldLayout.Glitter;
					var c=cell(nx,ny);
					if ((!glitter || Math.Abs(dx)<=1 && Math.Abs(dy)<=1) && (plan.TerrainAt(nx,ny) != PlannedTerrain.Land || map.Ramp[c] != 0)) return false;
					// Glitter covers most land: use existing resource-free gaps without
					// clearing any resource cell or imposing an impossible 7x7 empty patch.
					if ((!glitter || dx==0 && dy==0) && map.Resources[c].Type != 0) return false;
				}
				return true;
			}
			var candidates = new List<PlanPoint>();
			for (var y=3;y<plan.Height-3;y++) for (var x=3;x<plan.Width-3;x++) if (Eligible(x,y)) candidates.Add(new PlanPoint(x,y));
			var anchors = new List<PlanPoint>(); var used = new HashSet<CPos>();
			var limit = Math.Clamp(plan.Width*plan.Height/(desert ? 1800 : 1400),4,16);
			foreach (var p in candidates.OrderBy(p=>Hash(p.X,p.Y)))
			{
				if (anchors.Any(a=>Math.Max(Math.Abs(a.X-p.X),Math.Abs(a.Y-p.Y))<14)) continue;
				anchors.Add(p);
				foreach (var d in desert ? new[] { CVec.Zero, new CVec(2,1) } : new[] { CVec.Zero,new CVec(2,1),new CVec(-1,2) })
				{
					var x=p.X+d.X;var y=p.Y+d.Y;var c=cell(x,y);
					if (!Eligible(x,y) || !used.Add(c)) continue;
					actors.Add(new MiniYamlNode("BiomeTree"+used.Count,new ActorReference("terrain.generated.biometree"+(Hash(x,y)%images.Length)) { new LocationInit(c),new OwnerInit("Neutral") }.Save()));
				}
				if (anchors.Count>=limit) break;
			}

			using var stream=mod.DefaultFileSystem.Open("ca|sequences/decorations.yaml");
			var sources=MiniYaml.FromStream(stream,"decorations").ToDictionary(n=>n.Key,n=>n.Value);
			var rules=new StringBuilder();var sequences=new StringBuilder();
			if (desert || barren)
			{
				var folder = desert ? "desert" : "barren";
				sequences.Append($"terrain.rubberduck.cliff17:\n\tidle:\n\t\tFilename: ca|bits/terrain/rubberduck/derived/biomes/{folder}/plateau-roof.png\n\t\tTilesetFilenames:\n\t\t\tRUBBERDUCK-TEMPERATE-SHADED: ca|bits/terrain/rubberduck/derived/biomes/{folder}/plateau-roof-shaded.png\n");
				using var cliffs = mod.DefaultFileSystem.Open("ca|sequences/rubberduck-closed-cliffs.yaml");
				foreach (var node in MiniYaml.FromStream(cliffs,"biome-cliffs"))
				{
					var slot=node.Key.Substring("terrain.rubberduck.closedcliff".Length);
					sequences.Append($"{node.Key}:\n\tidle:\n\t\tFilename: ca|bits/terrain/rubberduck/derived/biomes/{folder}/closed-cliff-{slot}.png\n");
				}
			}
			for (var i=0;i<images.Length;i++)
			{
				var image=images[i];var defaults=sources[image].ToDictionary()["Defaults"].ToDictionary();
				var files=defaults["TilesetFilenames"].ToDictionary();
				var filename=files.TryGetValue(biome,out var f) ? f.Value : files["RUBBERDUCK-TEMPERATE"].Value;
				var name="terrain.generated.biometree"+i;
				rules.Append($"{name}:\n\tInherits: ^RubberduckTerrainDecoration\n\tRenderSprites:\n\t\tPalette: generated-biome-trees\n");
				sequences.Append($"{name}:\n\tInherits: {image}\n\tDefaults:\n\t\tScale: 1.8\n\t\tTilesetFilenames:\n\t\t\tRUBBERDUCK-TEMPERATE: {filename}\n");
			}
			var nodes=map.RuleDefinitions?.Nodes.ToList() ?? new List<MiniYamlNode>();nodes.AddRange(MiniYaml.FromString(rules.ToString(),"biome-trees"));
			var palette=desert ? "ca|bits/desert/desert.pal" : barren ? "ca|bits/barren/barren.pal" : "ca|bits/palettes/temperat.pal";
			var extra=MiniYaml.FromString($"PaletteFromFile@GENERATEDBIOMETREES:\n\tName: generated-biome-trees\n\tFilename: {palette}\n\tShadowIndex: 4\n","biome-palette");
			var index=nodes.FindIndex(n=>n.Key.Equals("World",StringComparison.OrdinalIgnoreCase));
			if(index<0)nodes.Add(new MiniYamlNode("World",new MiniYaml(null,extra)));
			else nodes[index]=new MiniYamlNode(nodes[index].Key,new MiniYaml(nodes[index].Value.Value,nodes[index].Value.Nodes.Concat(extra).ToArray()));
			map.RuleDefinitions=new MiniYaml(map.RuleDefinitions?.Value,nodes);
			map.SequenceDefinitions=new MiniYaml(map.SequenceDefinitions?.Value,(map.SequenceDefinitions?.Nodes.ToArray() ?? Array.Empty<MiniYamlNode>()).Concat(MiniYaml.FromString(sequences.ToString(),"biome-sequences")).ToArray());
		}
	}
}
