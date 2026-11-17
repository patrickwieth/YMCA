using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	public class RockCoastProbeInfo : TraitInfo
	{
		public readonly CPos Beach = CPos.Zero;
		public readonly CPos Rock = CPos.Zero;
		public readonly CPos Land = CPos.Zero;
		public readonly CPos Boarder = CPos.Zero;
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new RockCoastProbe(this);
	}
	public class RockCoastProbe : ITick
	{
		readonly RockCoastProbeInfo info;
		Actor[] passengers;
		Actor blockedBoarder;
		bool[] reached;
		int ticks;
		int stage;
		public RockCoastProbe(RockCoastProbeInfo info) { this.info = info; }
		void Pass(Actor self, string text) => PlateauMovementProbe.WriteResult(info.ResultPath, "COAST PASS " + self.Info.Name + ": " + text);
		public void Tick(Actor self)
		{
			if (++ticks < 20 || stage == 6) return;
			if (ticks > 2200) throw new InvalidOperationException("Coast probe timed out at stage " + stage + ": " + self.Info.Name + "; passengers=" + string.Join(";", passengers?.Select(p => p.Info.Name + "@" + p.Location + " idle=" + p.IsIdle) ?? Array.Empty<string>()) + "; land=" + info.Land + "; boarder=" + info.Boarder);
			var cargo = self.Trait<Cargo>(); var mobile = self.Trait<Mobile>();
			if (stage == 0)
			{
				passengers = cargo.Passengers.ToArray();
				if (self.Info.Name.EndsWith("boat0", StringComparison.Ordinal))
				{
					var corners = self.World.Actors.Where(a => a.Info.Name.StartsWith("terrain.rubberduck.coast-piece-", StringComparison.Ordinal)).ToArray();
					foreach (var corner in corners)
					{
						var slot = int.Parse(corner.Info.Name.Substring(corner.Info.Name.LastIndexOf('-') + 1), System.Globalization.CultureInfo.InvariantCulture);
						var d = slot < 26 ? 0 : slot < 28 ? 3 : slot < 30 ? 2 : 1;
						var a = MapGeneration.PlateauTopology.Directions[d]; var b = MapGeneration.PlateauTopology.Directions[(d + 1) % 4];
						var banks = slot % 2 == 0 ? new[] { corner.Location } : new[] { corner.Location + a, corner.Location + b };
						foreach (var c in banks.Concat(new[] { corner.Location + a + b }))
							if (mobile.CanEnterCell(c) || passengers.Any(p => p.Trait<Mobile>().CanEnterCell(c))) throw new InvalidOperationException("Coast corner has a collision gap: " + corner.Info.Name + " at " + c);
					}
					if (corners.Length != 0) Pass(self, "all " + corners.Length + " corner banks and water sockets reject craft and passengers");
					var caps = self.World.Actors.Where(a => a.Info.Name.StartsWith("terrain.rubberduck.coast-abutment-", StringComparison.Ordinal)).ToArray();
					foreach (var cap in caps)
					{
						var direction = int.Parse(cap.Info.Name.Substring(cap.Info.Name.LastIndexOf('-') + 1), System.Globalization.CultureInfo.InvariantCulture);
						var outward = MapGeneration.PlateauTopology.Directions[direction];
						var tangent = direction % 2 == 0 ? new CVec(0, 1) : new CVec(1, 0);
						for (var lane = 0; lane <= (direction < 2 ? 1 : 0); lane++)
							for (var depth = 1; depth <= 2; depth++)
							{
								var foot = cap.Location + tangent * lane + outward * depth;
								if (mobile.CanEnterCell(foot) || passengers.Any(p => p.Trait<Mobile>().CanEnterCell(foot)))
									throw new InvalidOperationException("Coast end piece has an unblocked foot: " + foot);
							}
					}
					if (caps.Length != 0) Pass(self, "all " + caps.Length + " end-piece water footprints reject craft and passengers");
				}
				if (passengers.Length != 2 || cargo.CanUnload() || mobile.CanEnterCell(info.Rock))
					throw new InvalidOperationException("Rock coast admits unloading or a landing craft.");
				foreach (var p in passengers)
					if (p.Trait<Mobile>().CanEnterCell(info.Rock)) throw new InvalidOperationException("Ground passenger can enter rock coast.");
				blockedBoarder = self.World.CreateActor("calibration.coastinfantry", new TypeDictionary { new OwnerInit(self.Owner), new LocationInit(info.Boarder) });
				foreach (var resolver in blockedBoarder.TraitsImplementing<IResolveOrder>())
					resolver.ResolveOrder(blockedBoarder, new Order("EnterTransport", blockedBoarder, Target.FromActor(self), false));
				cargo.ResolveOrder(self, new Order("Unload", self, false)); stage = 1;
			}
			else if (stage == 1 && ticks >= 120)
			{
				if (cargo.Passengers.Count() != 2 || passengers.Any(p => p.IsInWorld) || !blockedBoarder.IsInWorld) throw new InvalidOperationException("Unload leaked through rock coast.");
				blockedBoarder.CancelActivity();
				// The failed-entry probe may have approached the shore. Remove only
				// this diagnostic actor before testing independent landing targets.
				self.World.AddFrameEndTask(_ => blockedBoarder.Dispose());
				Pass(self, "rock blocks craft, infantry, tank, unload and boarding orders");
				self.QueueActivity(false, mobile.MoveTo(info.Beach)); stage = 2;
			}
			else if (stage == 2 && self.Location == info.Beach && self.IsIdle)
			{
				if (!cargo.CanUnload()) throw new InvalidOperationException("Beach refuses cargo unloading.");
				cargo.ResolveOrder(self, new Order("Unload", self, false)); stage = 3;
			}
			else if (stage == 3 && cargo.IsEmpty() && passengers.All(p => p.IsInWorld && p.IsIdle))
			{
				for (var i = 0; i < passengers.Length; i++)
				{
					var p = passengers[i];
					var movement = p.Trait<Mobile>();
					var target = info.Land + new CVec(2 * i, -2 * i);
					if (movement.PathFinder.FindPathToTargetCell(p, new[] { p.Location }, target, BlockedByActor.Immovable).Count == 0 ||
						self.World.Map.Height[p.Location] != 0) throw new InvalidOperationException("Unloaded passenger cannot reach flat inland terrain.");
					p.QueueActivity(false, movement.MoveTo(target));
				}
				reached = new bool[passengers.Length];
				Pass(self, "craft reached beach and unloaded real infantry/tank onto connected height-zero land"); stage = 4;
			}
			else if (stage == 4)
			{
				// Record arrivals separately: a later vehicle may legitimately nudge
				// an infantryman after he has already reached his target.
				for (var i = 0; i < passengers.Length; i++) reached[i] |= passengers[i].Location == info.Land + new CVec(2 * i, -2 * i);
				if (!reached.All(value => value)) return;
				Pass(self, "infantry and tank actually reached inland targets");
				foreach (var p in passengers)
					foreach (var resolver in p.TraitsImplementing<IResolveOrder>())
						resolver.ResolveOrder(p, new Order("EnterTransport", p, Target.FromActor(self), false));
				stage = 5;
			}
			else if (stage == 5 && passengers.All(p => !p.IsInWorld) && cargo.Passengers.Count() == 2)
			{
				Pass(self, "infantry and tank boarded again through the beach"); stage = 6;
			}
		}
	}
}
