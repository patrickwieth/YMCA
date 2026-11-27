using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Isolated native factory/airship regression probe, never a production-map trait.")]
	public sealed class FactoryAirshipProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new FactoryAirshipProbe();
	}
	public sealed class FactoryAirshipProbe : ITick
	{
		readonly List<(string Faction, Actor Airship, Actor Target, int Initial)> attacks = new();
		readonly List<(Actor Actor, CPos Rally)> factories = new();
		readonly List<object> production = new();
		int successfulProduction;
		readonly HashSet<uint> initial = new();
		bool done;
		void ITick.Tick(Actor self)
		{
			var w = self.World; var t = w.WorldTick;
			if (done) return;
			if (t == 10) w.AddFrameEndTask(_ => Start(w));
			if (t == 40)
				foreach (var item in attacks)
					item.Airship.QueueActivity(item.Airship.Trait<AttackAircraft>().GetAttackActivity(item.Airship,
						AttackSource.Default, Target.FromActor(item.Target), true, true));
			if (t == 50 || t == 150 || t == 250)
				foreach (var f in factories)
				{
					var type = f.Actor.Info.Name == "weap.nod" ? t == 50 ? "bggy" : t == 150 ? "bike" : "ltnk" : "mtnk";
					var ok = f.Actor.Trait<Production>().Produce(f.Actor, w.Map.Rules.Actors[type], "Vehicle", new TypeDictionary { new OwnerInit(f.Actor.Owner), new FactionInit(f.Actor.Trait<Production>().Faction) }, 0);
					if (ok) successfulProduction++;
					var exit = f.Actor.Location + f.Actor.Info.TraitInfo<ExitInfo>().ExitCell;
					production.Add(new { Factory = f.Actor.Info.Name, Unit = type, Tick = t, Produced = ok,
						HasRally = f.Actor.Trait<RallyPoint>().Path.Count != 0,
						Paused = f.Actor.Trait<Production>().IsTraitPaused, Disabled = f.Actor.Trait<Production>().IsTraitDisabled,
						Exit = exit.ToString(), Terrain = w.Map.GetTerrainInfo(exit).Type,
						Blockers = w.ActorMap.GetActorsAt(exit).Select(a => a.Info.Name).ToArray() });
				}
			if (t < 600) return;
			done = true;
			var vehicles = w.Actors.Where(a => !initial.Contains(a.ActorID) && a.TraitOrDefault<Mobile>() != null).ToArray();
			var passed = attacks.Count == 4 && attacks.All(a => !a.Target.IsInWorld || a.Target.Trait<Health>().HP < a.Initial) &&
				successfulProduction == 9 && vehicles.Length == 9 && vehicles.All(a => factories.Any(f => (f.Rally - a.Location).LengthSquared <= 9));
			File.WriteAllText(Path.Combine(Platform.SupportDir, "handcrafted-runtime.json"), JsonConvert.SerializeObject(new
			{
				Passed = passed, Probe = "Native Kirov attacks and real factory exit/rally regression",
				Attacks = attacks.Select(a => new { a.Faction, Damage = a.Initial - (a.Target.IsInWorld ? a.Target.Trait<Health>().HP : 0),
					Position = a.Airship.CenterPosition.ToString(), Activity = a.Airship.CurrentActivity?.GetType().Name }),
				Production = production,
				Vehicles = vehicles.Select(a => new { Type = a.Info.Name, Location = a.Location.ToString(),
						AtRally = factories.Any(f => (f.Rally - a.Location).LengthSquared <= 9) })
			}, Formatting.Indented));
		}
		void Start(World w)
		{
			var origin = new CPos(w.Map.CellBounds.X, w.Map.CellBounds.Y);
			var owner = w.LocalPlayer; var neutral = w.Players.First(p => p.InternalName == "Neutral");
			var index = 0;
			foreach (var faction in new[] { "russia", "nkorea", "iraq", "ukraine" })
			{
				var c = origin + new CVec(12 + 12 * index++, 10);
				var target = w.CreateActor("fcom", new TypeDictionary { new OwnerInit(neutral), new LocationInit(c) });
				var position = w.Map.CenterOfCell(c + new CVec(0, 3)) + new WVec(0, 0, 2048);
				var airship = w.CreateActor("kirov", new TypeDictionary { new OwnerInit(owner), new FactionInit(faction),
					new LocationInit(c + new CVec(0, 3)), new CenterPositionInit(position) });
				attacks.Add((faction, airship, target, target.Trait<Health>().HP));
			}
			foreach (var fixture in new[] { (Name: "weap.nod", Offset: new CVec(20, 32), Rally: true),
				(Name: "weap.td", Offset: new CVec(45, 32), Rally: true), (Name: "weap.nod", Offset: new CVec(20, 42), Rally: false) })
			{
				var name = fixture.Name;
				var ai = w.Map.Rules.Actors[name]; var bi = ai.TraitInfo<BuildingInfo>();
				var c = w.Map.FindTilesInCircle(origin + fixture.Offset, 8)
					.First(p => w.CanPlaceBuilding(p, ai, bi, null));
				var factory = w.CreateActor(name, new TypeDictionary { new OwnerInit(owner), new LocationInit(c) });
				var rally = c + (fixture.Rally ? new CVec(0, 8) : ai.TraitInfo<ExitInfo>().ExitCell);
				if (fixture.Rally) factory.Trait<RallyPoint>().Path.Add(rally);
				factories.Add((factory, rally));
			}
			foreach (var a in w.Actors) initial.Add(a.ActorID);
		}
	}
}
