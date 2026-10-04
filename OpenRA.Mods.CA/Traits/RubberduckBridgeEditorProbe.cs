using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class RubberduckBridgeEditorProbeInfo : TraitInfo
	{
		public readonly CPos Cell;
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new RubberduckBridgeEditorProbe(this);
	}
	public class RubberduckBridgeEditorProbe : IPostWorldLoaded, ITickRender
	{
		sealed class RemoveBridge : IEditorAction
		{
			readonly EditorActorLayer layer; readonly EditorActorPreview actor;
			public RemoveBridge(EditorActorLayer layer, EditorActorPreview actor) { this.layer = layer; this.actor = actor; }
			public string Text => "Diagnostic remove bridge";
			public void Execute() => Do();
			public void Do() => layer.Remove(actor);
			public void Undo() => layer.Add(actor);
		}
		sealed class PaintBank : IEditorAction
		{
			readonly Map map; readonly Dictionary<CPos, TerrainTile> original = new();
			public PaintBank(Map map, CPos bank)
			{
				this.map = map;
				for (var x = -2; x <= 2; x++)
					for (var y = -2; y <= 2; y++)
					{
						var c = bank + new CVec(x, y);
						if (map.Contains(c) && (map.Tiles[c].Type == 1000 || map.Tiles[c].Type == 1030)) original.Add(c, map.Tiles[c]);
					}
			}
			public string Text => "Diagnostic bank occlusion";
			public void Execute() => Do();
			public void Do() { foreach (var c in original.Keys) map.Tiles[c] = new TerrainTile(1050, 0); }
			public void Undo() { foreach (var p in original) map.Tiles[p.Key] = p.Value; }
		}
		readonly RubberduckBridgeEditorProbeInfo info;
		int frames;
		string original, removed;
		EditorActorPreview bridge;
		public RubberduckBridgeEditorProbe(RubberduckBridgeEditorProbeInfo info) { this.info = info; }
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 150) return;
			var map = self.World.Map; var layer = self.Trait<EditorActorLayer>(); var history = self.Trait<EditorActionManager>();
			void Check(string expected, string label)
			{
				if (PlateauEditorProbe.Snapshot(map, layer) != expected) throw new InvalidOperationException("Bridge editor snapshot mismatch: " + label);
				PlateauMovementProbe.WriteResult(info.ResultPath, "BRIDGE EDITOR PASS " + label);
			}
			if (frames == 20)
			{
				bridge = layer.PreviewsAtCell(info.Cell).Single(a => a.Info.HasTraitInfo<RubberduckBridgeBodyInfo>());
				var preview = bridge.Render().ToArray();
				var runtime = new RubberduckBridgeBody(bridge.Info.TraitInfo<RubberduckBridgeBodyInfo>()).Render(wr, map.CenterOfCell(info.Cell)).ToArray();
				if (preview.Length != runtime.Length || preview.Where((r, i) => r.Pos != runtime[i].Pos || r.ZOffset != runtime[i].ZOffset).Any())
					throw new InvalidOperationException("Bridge editor/runtime layer anchors differ.");
				wr.Viewport.UnlockMinimumZoom(.1f); wr.Viewport.AdjustZoom((float)Math.Log(.65f / wr.Viewport.Zoom)); wr.Viewport.Center(bridge.CenterPosition);
				original = PlateauEditorProbe.Snapshot(map, layer); Check(original, "runtime/preview anchors and layers");
			}
			if (frames == 25)
			{
				string Clips() => string.Join("|", bridge.Render().OfType<BridgeDeckRenderable>().Select(r => r.ClipSignature));
				var before = Clips(); history.Add(new PaintBank(map, info.Cell)); var changed = Clips();
				if (before == changed) throw new InvalidOperationException("Bridge bank cache did not respond to terrain edit.");
				history.Undo(); if (Clips() != before) throw new InvalidOperationException("Bank clip undo was stale.");
				history.Redo(); if (Clips() != changed) throw new InvalidOperationException("Bank clip redo was stale.");
				history.Undo(); Check(original, "bank clipping invalidation and exact undo/redo");
			}
			if (frames == 30) { history.Add(new RemoveBridge(layer, bridge)); removed = PlateauEditorProbe.Snapshot(map, layer); if (removed == original) throw new InvalidOperationException("Bridge removal did nothing."); }
			if (frames == 60) { history.Undo(); Check(original, "exact undo"); }
			if (frames == 90) { history.Redo(); Check(removed, "exact redo"); }
			if (frames == 120)
			{
				history.Undo(); Check(original, "restored before save"); map.ActorDefinitions = layer.Save();
				var path = Path.ChangeExtension(info.ResultPath, ".oramap"); PlateauEditorProbe.SaveTerrainCopy(map, path);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var reload = new Map(Game.ModData, zip);
				if (reload.InvalidCustomRules || map.RuleDefinitions.Nodes.WriteToString() != reload.RuleDefinitions.Nodes.WriteToString() ||
					map.ActorDefinitions.OrderBy(n => n.Key).WriteToString() != reload.ActorDefinitions.OrderBy(n => n.Key).WriteToString())
					throw new InvalidOperationException("Bridge rules/actors failed save/reload.");
				foreach (var c in map.AllCells)
					if (!map.Tiles[c].Equals(reload.Tiles[c]) || map.Height[c] != reload.Height[c] || !map.Resources[c].Equals(reload.Resources[c]))
						throw new InvalidOperationException("Bridge editor changed terrain/resources during save.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "BRIDGE EDITOR PASS exact terrain/actors/rules save/reload");
			}
			if (frames == 150) Game.TakeScreenshot();
		}
	}
}
