using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Isolated review-only full-map reveal timing; never enabled on production maps.")]
	public sealed class MapRevealPerformanceProbeInfo : TraitInfo
	{
		public readonly bool DebugReveal = true;
		public override object Create(ActorInitializer init) => new MapRevealPerformanceProbe(this);
	}
	public sealed class MapRevealPerformanceProbe : ITick, ITickRender
	{
		readonly MapRevealPerformanceProbeInfo info;
		readonly List<double> before = new(), after = new();
		long previous;
		int ticks;
		bool revealed, saved;
		double revealCallMs, maxLightMs;
		CPos fogCell;
		(string Type, int Density) remembered;
		bool fogMemoryPassed;
		public MapRevealPerformanceProbe(MapRevealPerformanceProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			ticks++;
			if (ticks == 25) CheckBatch(self);
			if (ticks == 150)
			{
				var clock = Stopwatch.StartNew();
				var shroud = self.World.RenderPlayer.Shroud;
				if (info.DebugReveal) shroud.Disabled = true;
				else shroud.AddSource(this, Shroud.SourceType.PassiveVisibility, self.World.Map.ProjectedCells);
				revealCallMs = clock.Elapsed.TotalMilliseconds;
				revealed = true;
			}
			if (ticks == 450)
			{
				self.World.RenderPlayer.Shroud.Disabled = false;
				if (!info.DebugReveal) self.World.RenderPlayer.Shroud.RemoveSource(this);
			}
			if (ticks == 470)
			{
				var resources = self.Trait<IResourceLayer>();
				fogCell = self.World.Map.AllCells.First(c => !resources.IsVisible(c) && resources.GetResource(c).Type == "Tiberium");
				remembered = self.Trait<TiberiumTerrainLight>().KnownResource(fogCell);
				if (remembered.Density <= 0) throw new InvalidOperationException("Fog fixture light never became known.");
				resources.ClearResources(fogCell);
			}
			if (ticks == 490)
			{
				if (self.Trait<TiberiumTerrainLight>().KnownResource(fogCell) != remembered)
					throw new InvalidOperationException("Hidden harvesting leaked through deferred light updates.");
				self.World.RenderPlayer.Shroud.Disabled = true;
			}
			if (ticks == 510)
			{
				if (self.Trait<TiberiumTerrainLight>().KnownResource(fogCell).Density != 0)
					throw new InvalidOperationException("Revealed depletion did not update lighting.");
				fogMemoryPassed = true;
			}
		}
		static void CheckBatch(Actor self)
		{
			var lighting = self.Trait<TerrainLighting>();
			var field = self.Trait<ITerrainLighting>();
			var position = self.World.Map.CenterOfCell(self.World.Map.ChooseClosestEdgeCell(self.World.RenderPlayer.HomeLocation));
			var before = field.TintAt(position);
			var notifications = new List<MPos>();
			lighting.CellChanged += notifications.Add;
			int token;
			float3 expected;
			try
			{
				using (lighting.BatchUpdates())
				{
					token = lighting.AddLightSource(position, new WDist(1536), .1f, float3.Ones);
					using (lighting.BatchUpdates())
					{
						var removed = lighting.AddLightSource(position, new WDist(1536), .2f, float3.Ones);
						lighting.RemoveLightSource(removed);
					}
					if (notifications.Count != 0) throw new InvalidOperationException("Nested light batch dispatched early.");
					expected = field.TintAt(position);
				}
				if (notifications.Count == 0 || notifications.Count != notifications.Distinct().Count() || field.TintAt(position) != expected)
					throw new InvalidOperationException("Light batch changed tint or duplicated cell notifications.");
				lighting.RemoveLightSource(token);
				if (field.TintAt(position) != before) throw new InvalidOperationException("Light removal failed to restore tint.");
			}
			finally { lighting.CellChanged -= notifications.Add; }
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			var now = Stopwatch.GetTimestamp();
			if (previous != 0 && ticks >= 75 && ticks < 450)
				(revealed ? after : before).Add((now - previous) * 1000.0 / Stopwatch.Frequency);
			previous = now;
			var lights = self.TraitOrDefault<TiberiumTerrainLight>();
			if (revealed && lights != null) maxLightMs = Math.Max(maxLightMs, lights.LastUpdateMilliseconds);
			if (ticks < 511 || saved) return;
			saved = true;
			object Stats(List<double> values) => new { Frames = values.Count, MaxMs = values.Max(), P95Ms = values.OrderBy(x => x).ElementAt((int)(values.Count * .95)), MeanMs = values.Average() };
			File.WriteAllText(Path.Combine(Platform.SupportDir, "handcrafted-runtime.json"), JsonConvert.SerializeObject(new {
				Passed = true, Probe = "Map reveal frame intervals (includes frame cap)", Mode = info.DebugReveal ? "debug" : "satellite visibility source",
				Map = self.World.Map.Title, RevealCallMs = revealCallMs, Before = Stats(before), After = Stats(after), MaxLightUpdateMs = maxLightMs, PendingLightCells = lights?.PendingCells, FogMemoryPassed = fogMemoryPassed
			}, Formatting.Indented));
		}
	}
}
