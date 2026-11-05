using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ContinuousCoastEditorProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new ContinuousCoastEditorProbe(this);
	}
	public class ContinuousCoastEditorProbe : IPostWorldLoaded, ITickRender
	{
		readonly ContinuousCoastEditorProbeInfo info;
		int frames;
		public ContinuousCoastEditorProbe(ContinuousCoastEditorProbeInfo info) { this.info = info; }
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames != 40) return;
			var map = self.World.Map;
			var layer = self.Trait<EditorActorLayer>();
			var history = self.Trait<EditorActionManager>();
			var before = PlateauEditorProbe.Snapshot(map, layer);
			var banks = map.AllCells.Where(map.Contains).Where(c => map.Tiles[c].Type == 14011 || map.Tiles[c].Type == 14012).ToArray();
			var seen = new System.Collections.Generic.HashSet<CPos>();
			var components = 0;
			foreach (var cell in banks)
			{
				if (seen.Contains(cell)) continue;
				var removal = RubberduckCoastRemovalPlan.Create(map, layer, cell);
				seen.UnionWith(removal.Patch.Keys);
				history.Add(new PlateauTerrainEditAction(map, removal.Patch,
					PlateauActorEdit.Replace(layer, removal.Actors, Array.Empty<MiniYamlNode>()), "Continuous coast diagnostic"));
				var removed = PlateauEditorProbe.Snapshot(map, layer);
				history.Undo();
				if (PlateauEditorProbe.Snapshot(map, layer) != before) throw new InvalidOperationException("Continuous coast Undo mismatch.");
				history.Redo();
				if (PlateauEditorProbe.Snapshot(map, layer) != removed) throw new InvalidOperationException("Continuous coast Redo mismatch.");
				history.Undo(); components++;
			}
			if (components == 0 || PlateauEditorProbe.Snapshot(map, layer) != before) throw new InvalidOperationException("No intact coast restored.");
			map.ActorDefinitions = layer.Save();
			var path = Path.ChangeExtension(info.ResultPath, ".oramap");
			PlateauEditorProbe.SaveTerrainCopy(map, path);
			using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var reload = new Map(Game.ModData, zip);
			foreach (var cell in map.AllCells)
				if (!map.Tiles[cell].Equals(reload.Tiles[cell]) || map.Height[cell] != reload.Height[cell] || !map.Resources[cell].Equals(reload.Resources[cell]))
					throw new InvalidOperationException("Continuous coast save/reload mismatch.");
			if (map.ActorDefinitions.WriteToString() != reload.ActorDefinitions.WriteToString()) throw new InvalidOperationException("Coast actors changed on reload.");
			PlateauMovementProbe.WriteResult(info.ResultPath, $"CONTINUOUS COAST EDITOR PASS {components} components; exact removal/Undo/Redo and save/reload.");
			wr.Viewport.Center(map.CenterOfCell(banks[banks.Length / 2]));
			Game.TakeScreenshot();
		}
	}
}
