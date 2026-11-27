using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Opt-in isolated building finish, faction bib and helipad traffic review.")]
	public sealed class BuildingFinishReviewProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new BuildingFinishReviewProbe();
	}

	public sealed class BuildingFinishReviewProbe : ITick, ITickRender
	{
		readonly string[] factions = { "england", "russia", "eagle", "blackh", "chinatnk", "reaper" };
		readonly string[] bibs = { "alliedbib3x3", "sovietbib3x3", "gdibib3x3", "nodbib3x3", "sovietbib3x3", "scrinbib3x3" };
		readonly string[] actors = { "hpad", "chsupply", "apwr.soviet", "barr", "atek", "rep.nod", "weap.td", "wsph", "swiss_bank", "grand_cannon" };
		readonly string[] actorFactions = { "england", "chinatnk", "russia", "russia", "england", "blackh", "eagle", "reaper", "england", "england" };
		readonly List<Actor> banks = new();
		readonly HashSet<int> captures = new();
		Actor helipad, walker, current;
		int ticks;
		CPos destination;
		bool walkPassed;
		public BuildingFinishReviewProbe()
		{
			if (Environment.GetEnvironmentVariable("YMCA_BUILDING_FINISH_REVIEW") != "1") throw new InvalidOperationException("Diagnostic opt-in required.");
		}
		Actor Spawn(World w, string type, CPos cell, string faction, bool skip)
		{
			var init = new TypeDictionary { new OwnerInit(w.LocalPlayer), new LocationInit(cell), new FactionInit(faction) };
			if (skip) init.Add(new SkipMakeAnimsInit());
			return w.CreateActor(type, init);
		}
		void ITick.Tick(Actor self)
		{
			var w = self.World; ticks++;
			if (ticks == 10) w.AddFrameEndTask(_ =>
			{
				for (var i = 0; i < factions.Length; i++) banks.Add(Spawn(w, "swiss_bank", new CPos(90 + i % 3 * 6 + i / 3 * 6, -35 - i % 3 * 6 + i / 3 * 6), factions[i], true));
				helipad = Spawn(w, "hpad", new CPos(115, -20), "england", true);
				walker = Spawn(w, "light_infantry", helipad.Location + new CVec(-2, 1), "england", false);
				destination = helipad.Location + new CVec(4, 1);
			});
			if (ticks == 20)
			{
				for (var i = 0; i < banks.Count; i++)
				{
					var enabled = banks[i].TraitsImplementing<WithIdleOverlay>().Where(b => b.Info.Image?.Contains("bib") == true && !b.IsTraitDisabled).ToArray();
					if (enabled.Length != 1 || enabled[0].Info.Image != bibs[i]) throw new InvalidOperationException("Wrong bank bib for " + factions[i]);
					var previews = banks[i].Info.TraitInfos<WithFactionBibOverlayInfo>().Where(b => b.MatchesFaction(factions[i])).ToArray();
					if (previews.Length != 1 || previews[0].Image != bibs[i]) throw new InvalidOperationException("Wrong bank preview bib for " + factions[i]);
				}
				var mobile = walker.Trait<Mobile>();
				for (var x = 0; x < 3; x++) for (var y = 0; y < 3; y++)
					if (mobile.CanEnterCell(helipad.Location + new CVec(x, y))) throw new InvalidOperationException("Walkable built helipad cell.");
				if (!mobile.CanEnterCell(destination)) throw new InvalidOperationException("Helipad exterior blocked.");
				walker.QueueActivity(mobile.MoveTo(destination));
			}
			if (ticks > 20 && walker != null && walker.Location == destination) walkPassed = true;
			if (ticks == 280)
			{
				if (!walkPassed) throw new InvalidOperationException("Helipad detour did not finish.");
				w.AddFrameEndTask(_ => { foreach (var b in banks) b.Dispose(); helipad.Dispose(); walker.Dispose(); walker = null; });
			}
			if (ticks >= 300 && (ticks - 300) % 90 == 0)
			{
				var index = (ticks - 300) / 90;
				if (index < actors.Length) w.AddFrameEndTask(_ => { current?.Dispose(); current = Spawn(w, actors[index], new CPos(110, -30), actorFactions[index], false); });
			}
			if (ticks == 300 + actors.Length * 90 + 20)
			{
				if (captures.Count != factions.Length + actors.Length * 3) throw new InvalidOperationException("Missing native captures.");
				File.WriteAllText(Path.Combine(Platform.SupportDir, "building-finish-review.json"), JsonConvert.SerializeObject(new { Passed = true, BankFactions = factions, BankBibs = bibs, HelipadBlockedCells = 9, HelipadDetour = walkPassed, ConstructionActors = actors, Screenshots = captures.Count }, Formatting.Indented));
			}
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			Actor target = null;
			if (ticks >= 30 && ticks <= 80 && (ticks - 30) % 10 == 0) target = banks[(ticks - 30) / 10];
			if (ticks >= 300 && ticks < 300 + actors.Length * 90 && ((ticks - 300) % 90 == 5 || (ticks - 300) % 90 == 20 || (ticks - 300) % 90 == 55)) target = current;
			if (target == null || !captures.Add(ticks)) return;
			wr.Viewport.UnlockMinimumZoom(.1f);
			wr.Viewport.AdjustZoom((float)Math.Log(1f / wr.Viewport.Zoom));
			wr.Viewport.Center(target.CenterPosition);
			Game.TakeScreenshot();
		}
	}
}
