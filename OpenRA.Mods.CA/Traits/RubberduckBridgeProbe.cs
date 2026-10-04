using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Graphics;
using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class RubberduckBridgeProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new RubberduckBridgeProbe(this);
	}
	public class RubberduckBridgeProbe : ITick, IPostWorldLoaded
	{
		sealed class Test
		{
			public Actor Bridge, Tank, Infantry, Ship;
			public CPos Start, End, Middle, ShipStart, ShipEnd;
			public CVec Step;
			public int Stage, Started, DestroyedAt;
			public bool Ordered, LayersChecked;
		}
		readonly RubberduckBridgeProbeInfo info;
		readonly List<Test> tests = new();
		bool complete;
		WorldRenderer renderer;
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr) { renderer = wr; }
		public RubberduckBridgeProbe(RubberduckBridgeProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			var world = self.World;
			if (complete || world.LocalPlayer == null) return;
			Actor Unit(string name, CPos cell) => world.CreateActor(name, new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(cell) });
			if (world.WorldTick == 20)
				world.AddFrameEndTask(w =>
				{
					foreach (var bridge in w.Actors.Where(a => a.Info.HasTraitInfo<RubberduckBridgeBodyInfo>()).ToArray())
					{
						var body = bridge.Info.TraitInfo<RubberduckBridgeBodyInfo>();
						var step = body.AlongY ? new CVec(0, 1) : new CVec(1, 0);
						var side = body.AlongY ? new CVec(1, 0) : new CVec(0, 1);
						var t = new Test { Bridge = bridge, Start = bridge.Location, End = bridge.Location + step * (body.Length - 1),
							Middle = bridge.Location + step * (body.Length / 2), Step = step, Started = w.WorldTick };
						t.ShipStart = t.Middle + side * 2; t.ShipEnd = t.Middle - side * 2;
						t.Tank = Unit("challenger_tank", t.Start); t.Infantry = Unit("light_infantry", t.End + step);
						t.Ship = Unit("dd", t.ShipStart);
						if (t.Ship.Trait<IPositionable>().CanExistInCell(t.Middle) || !t.Tank.Trait<IPositionable>().CanExistInCell(t.Middle))
							throw new InvalidOperationException("Intact ground bridge did not separate naval and land traffic.");
						tests.Add(t);
					}
					if (tests.Count == 0) throw new InvalidOperationException("Bridge fixture has no bridges.");
				});
			foreach (var t in tests)
			{
				if (t.Stage == 8) continue;
				if (world.WorldTick - t.Started > 4500) throw new InvalidOperationException($"Bridge probe timed out: {t.Bridge.Info.Name} stage {t.Stage}.");
				if (t.Stage == 5)
				{
					if (!t.LayersChecked)
					{
						var renderables = ((IRender)t.Bridge.Trait<RubberduckBridgeBody>()).Render(t.Bridge, renderer).ToArray();
						var count = renderables.Length / 2;
						var nearest = Enumerable.Range(0, count).OrderBy(i => (renderables[i].Pos - t.Tank.CenterPosition).LengthSquared).First();
						var deck = WorldRenderer.RenderableZPositionComparisonKey(renderables[nearest]);
						var frame = WorldRenderer.RenderableZPositionComparisonKey(renderables[count + nearest]);
						var tank = t.Tank.Render(renderer).Where(r => !r.IsDecoration).Select(WorldRenderer.RenderableZPositionComparisonKey).ToArray();
						if (tank.Length == 0 || tank.Any(z => z <= deck || z >= frame))
							throw new InvalidOperationException("Bridge draw order is not deck < real vehicle < frame.");
						t.LayersChecked = true;
						var sourceSprite = world.Map.Sequences.GetSequence("rubberduck.bridge", "deck-left").GetSprite(0);
						PlateauMovementProbe.WriteResult(info.ResultPath, $"BRIDGE SPRITE size={sourceSprite.Size} offset={sourceSprite.Offset}; start={renderer.ScreenPxPosition(world.Map.CenterOfCell(t.Start))} unit={renderer.ScreenPxPosition(t.Tank.CenterPosition)}");
						PlateauMovementProbe.WriteResult(info.ResultPath, "BRIDGE LAYERS PASS " + t.Bridge.Info.Name);
					}
					if (world.WorldTick < 1500) continue;
					t.Stage = 6; t.DestroyedAt = world.WorldTick;
					// Shorten the diagnostic, not production HP: leave the final 1000
					// hit points for actual tank weapons/projectiles to destroy.
					var health = t.Bridge.Trait<IHealth>();
					t.Bridge.InflictDamage(t.Tank, new Damage(Math.Max(0, health.HP - 1000)));
					foreach (var attack in t.Tank.TraitsImplementing<AttackBase>())
						attack.AttackTarget(Target.FromActor(t.Bridge), AttackSource.Default, false, false, true);
					continue;
				}
				if (t.Stage == 6)
				{
					if (t.Bridge.IsInWorld) { t.DestroyedAt = world.WorldTick; continue; }
					if (world.WorldTick < t.DestroyedAt + 20) continue;
					if (!t.Tank.IsDead || world.Map.GetTerrainInfo(t.Middle).Type != "Water")
						throw new InvalidOperationException("Destroyed bridge retained its deck or failed to remove stranded traffic.");
					PlateauMovementProbe.WriteResult(info.ResultPath, "BRIDGE COLLAPSE PASS " + t.Bridge.Info.Name);
					if (!t.Ship.Trait<IPositionable>().CanExistInCell(t.Middle))
						throw new InvalidOperationException("Collapsed bridge still blocks naval terrain entry.");
					t.Stage = 7;
					continue;
				}
				var actor = t.Stage <= 1 || t.Stage == 4 ? t.Tank : t.Stage <= 3 ? t.Infantry : t.Ship;
				if (actor == null) continue;
				var target = t.Stage == 0 ? t.End : t.Stage == 1 ? t.Start - t.Step : t.Stage == 2 ? t.Start : t.Stage == 3 ? t.End : t.Stage == 4 ? t.Middle : t.ShipEnd;
				if (!t.Ordered)
				{
					var mobile = actor.Trait<Mobile>();
					var path = mobile.PathFinder.FindPathToTargetCell(actor, new[] { actor.Location }, target, BlockedByActor.Immovable);
					if (path.Count == 0 || !path.Contains(t.Middle))
						throw new InvalidOperationException($"Bridge path does not cross the requested span: {t.Bridge.Info.Name} stage={t.Stage} path={path.Count} terrain={world.Map.GetTerrainInfo(t.Middle).Type} occupants={string.Join(",", world.ActorMap.GetActorsAt(t.Middle).Select(a => a.Info.Name))}.");
					actor.QueueActivity(false, mobile.MoveTo(target)); t.Ordered = true;
				}
				if (actor.Location == target && actor.IsIdle)
				{
					PlateauMovementProbe.WriteResult(info.ResultPath, $"BRIDGE MOVE PASS {t.Bridge.Info.Name} stage={t.Stage}");
					t.Stage++; t.Ordered = false;
				}
			}
			if (tests.Count != 0 && tests.All(t => t.Stage == 8))
			{
				complete = true;
				PlateauMovementProbe.WriteResult(info.ResultPath, $"BRIDGE COMPLETE spans={tests.Count}");
			}
		}
	}
}
