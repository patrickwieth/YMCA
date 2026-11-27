using System.Collections.Generic;
using System.Diagnostics;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Updates local Tiberium lighting from visible resource changes, retaining last-seen light under fog.")]
	public class TiberiumTerrainLightInfo : TraitInfo, Requires<IResourceLayerInfo>, Requires<TerrainLightingInfo>
	{
		public readonly WDist Range = new WDist(1536);
		public readonly float Intensity = 0.025f;
		// Cosmetic light only; visibility, units and simulation are never deferred.
		public readonly int MaxUpdatesPerTick = 128;
		public readonly int MaxUpdateMilliseconds = 2;
		public override object Create(ActorInitializer init) { return new TiberiumTerrainLight(init.Self, this); }
	}

	// Render-local only: no actors, synchronized state or simulation ticks are added.
	public class TiberiumTerrainLight : IWorldLoaded, ITickRender, INotifyActorDisposing
	{
		readonly TiberiumTerrainLightInfo info;
		readonly IResourceLayer resources;
		readonly TerrainLighting lighting;
		readonly World world;
		readonly bool enabled;
		readonly HashSet<CPos> dirty = new();
		readonly List<CPos> processed = new();
		readonly Dictionary<CPos, (int Token, string Type, int Density)> lights = new();

		public TiberiumTerrainLight(Actor self, TiberiumTerrainLightInfo info)
		{
			this.info = info;
			world = self.World;
			resources = self.Trait<IResourceLayer>();
			lighting = self.Trait<TerrainLighting>();
			enabled = world.Map.Tileset == "RUBBERDUCK-TEMPERATE" || world.Map.Tileset == "RUBBERDUCK-TEMPERATE-SHADED";
			if (enabled) resources.CellChanged += Changed;
		}

		void Changed(CPos cell, string type) { dirty.Add(cell); }

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			if (!enabled) return;
			foreach (var cell in w.Map.AllCells)
				if (resources.GetResource(cell).Type != null) dirty.Add(cell);
		}

		internal (string Type, int Density) KnownResource(CPos cell) => lights.TryGetValue(cell, out var light) ? (light.Type, light.Density) : (null, 0);

		internal double LastUpdateMilliseconds { get; private set; }
		internal int PendingCells => dirty.Count;
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			var clock = Stopwatch.StartNew();
			using (lighting.BatchUpdates())
			foreach (var cell in dirty)
			{
				if (info.MaxUpdatesPerTick > 0 && processed.Count >= info.MaxUpdatesPerTick ||
					info.MaxUpdateMilliseconds > 0 && processed.Count > 0 && clock.ElapsedMilliseconds >= info.MaxUpdateMilliseconds) break;
				// Do not query current density until visible. Hidden harvesting/growth must
				// neither change remembered light nor illuminate a previously unknown field.
				if (!resources.IsVisible(cell)) continue;
				processed.Add(cell);
				var resource = resources.GetResource(cell);
				var type = resource.Type;
				var density = type == "Tiberium" || type == "BlueTiberium" ? resource.Density : 0;
				if (lights.TryGetValue(cell, out var old))
				{
					if (old.Type == type && old.Density == density) continue;
					lighting.RemoveLightSource(old.Token);
					lights.Remove(cell);
				}

				if (density <= 0) continue;
				var strength = density / (float)resources.GetMaxDensity(type);
				var tint = type == "Tiberium" ? new float3(0, 0.075f, 0.025f) : new float3(0, 0.04f, 0.09f);
				var token = lighting.AddLightSource(world.Map.CenterOfCell(cell), info.Range, info.Intensity * strength, new float3(tint.X * strength, tint.Y * strength, tint.Z * strength));
				lights.Add(cell, (token, type, density));
			}

			foreach (var cell in processed) dirty.Remove(cell);
			processed.Clear();
			LastUpdateMilliseconds = clock.Elapsed.TotalMilliseconds;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (enabled) resources.CellChanged -= Changed;
			using (lighting.BatchUpdates())
				foreach (var light in lights.Values) lighting.RemoveLightSource(light.Token);
			lights.Clear();
			dirty.Clear();
		}
	}
}
