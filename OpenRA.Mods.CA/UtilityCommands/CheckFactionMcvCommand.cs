using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckFactionMcvCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-faction-mcvs";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check faction MCV deployment/redeployment, starts, recovery, hard Airship roster gates and requested ground masks.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actors = mod.DefaultRules.Actors;
			foreach (var (mcv, yard) in new[] { ("amcv", "afac"), ("amcv.nod", "afac.nod"), ("amcv.nukular", "afac.nukular"), ("amcv.nod.nukular", "afac.nod.nukular") })
			{
				_ = actors[mcv].TraitsInConstructOrder().ToArray();
				_ = actors[yard].TraitsInConstructOrder().ToArray();
				if (actors[mcv].TraitInfo<TransformsInfo>().IntoActor != yard || actors[yard].TraitInfo<TransformsInfo>().IntoActor != mcv)
					throw new InvalidDataException(mcv + ": incorrect faction deploy/redeploy round trip.");
			}
			foreach (var (faction, mcv) in new[] { ("gdi", "amcv"), ("talon", "amcv"), ("zocom", "amcv"), ("eagle", "amcv"), ("nod", "amcv.nod"), ("blackh", "amcv.nod"), ("marked", "amcv.nod"), ("legion", "amcv.nod") })
			{
				var starts = actors["world"].TraitInfos<StartingUnitsInfo>().Where(s => s.Factions.Contains(faction)).ToArray();
				if (starts.Length < 4 || starts.Any(s => s.BaseActor != mcv || (s.Class == "doubled" && !s.SupportActors.Contains(mcv))))
					throw new InvalidDataException(faction + ": incorrect MCV starting preset.");
			}
			if (!actors["amcv"].TraitInfo<BuildableInfo>().Prerequisites.Contains("~gdi") || !actors["amcv.nod"].TraitInfo<BuildableInfo>().Prerequisites.Contains("~nod"))
				throw new InvalidDataException("MCV production faction gates are not separate.");
			foreach (var ai in actors["player"].TraitInfos<McvManagerBotModuleInfo>())
				if (!ai.McvTypes.Contains("amcv.nod") || !ai.McvTypes.Contains("amcv.nod.nukular") || !ai.ConstructionYardTypes.Contains("afac.nod.nukular"))
					throw new InvalidDataException("AI does not recognize Nod MCV/yard variants.");
			foreach (var (husk, mcv) in new[] { ("amcv.husk", "amcv"), ("amcv.nod.husk", "amcv.nod") })
			{
				_ = actors[husk].TraitsInConstructOrder().ToArray();
				if (actors[husk].TraitInfo<TransformOnCaptureInfo>().IntoActor != mcv) throw new InvalidDataException(husk + ": wrong recovered faction.");
			}
			foreach (var name in new[] { "blimp", "promotion.airship" })
			{
				var b = actors[name].TraitInfo<BuildableInfo>();
				foreach (var mode in new[] { "Tactical", "Operational", "Strategic", "Conquest" })
					foreach (var faction in new[] { "soviet", "russia", "nkorea", "iraq", "scrin", "reaper", "traveler", "harvester", "gdi", "nod" })
					{
						var expected = mode == "Operational" && new[] { "soviet", "russia", "nkorea", "iraq" }.Contains(faction);
						if (b.IsAvailableInContext(new[] { mode }, faction) != expected) throw new InvalidDataException(name + ": hard mode/faction roster gate failed.");
					}
			}
			foreach (var name in new[] { "upgc", "nerv", "srep", "port" })
			{
				var b = actors[name].TraitInfo<BuildingInfo>();
				var solid = b.Footprint.Count(x => x.Value == FootprintCellType.Occupied);
				if (solid != (name == "port" ? 2 : name == "srep" ? 3 : 4) || b.Tiles(CPos.Zero).Count() != (name == "port" ? 4 : 9))
					throw new InvalidDataException(name + ": requested body/bib cell counts differ.");
				if (name == "srep" && !b.TransitOnlyTiles(CPos.Zero).SequenceEqual(new[] { new CPos(0, 1) }))
					throw new InvalidDataException("Scrin repair zone must be exactly the left-middle tile0,1.");
			}
			Console.WriteLine("GDI/Nod MCV round trips, all faction starts, recovery and production gates; Airship hard mode/faction restrictions; UPGC/PORT/NERV/SREP masks and exact repair zone passed.");
		}
	}
}
