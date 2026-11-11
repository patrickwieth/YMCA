using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-gdi-building-art", "Check supplied GDI buildings, isolated bibs and Research Core checkpoint restoration.")]
	public sealed class CheckGdiBuildingArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-gdi-building-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			var grid = utility.ModData.Manifest.Get<MapGrid>();
			var sequences = MiniYaml.Load(utility.ModData.DefaultFileSystem, utility.ModData.Manifest.Sequences, null)
				.ToDictionary(n => n.Key, n => n.Value);
			foreach (var entry in new[]
			{
				(Actor: "afac", Image: "afac", File: "conyard.png", Bib: "4x4", X: 3, Y: 4),
				(Actor: "nuke", Image: "nuke", File: "power plant.png", Bib: "3x3", X: 2, Y: 3),
				(Actor: "pyle", Image: "pyle", File: "barracks.png", Bib: "3x3", X: 2, Y: 3),
				(Actor: "hq", Image: "hq", File: "radar.png", Bib: "3x3", X: 2, Y: 3),
				(Actor: "rep", Image: "rep", File: "service depot.png", Bib: "3x3", X: 3, Y: 3),
				(Actor: "hpad.td", Image: "hpad2", File: "helipad.png", Bib: "3x3", X: 2, Y: 3),
				(Actor: "proc.td", Image: "proc2", File: "refinery.png", Bib: "3x3", X: 3, Y: 4),
				(Actor: "afld.gdi", Image: "afldgdi", File: "airfield.png", Bib: "4x5", X: 3, Y: 2),
				(Actor: "gtek", Image: "gtek", File: "techcenter.png", Bib: "3x3", X: 3, Y: 3),
				(Actor: "weap.td", Image: "awep", File: "weapons factory.png", Bib: "4x4", X: 3, Y: 3),
				(Actor: "eye", Image: "eye", File: "communication center.png", Bib: "3x3", X: 2, Y: 3),
				(Actor: "upgc", Image: "upgc", File: "upgrade center.png", Bib: "3x3", X: 3, Y: 3),
				(Actor: "syrd.gdi", Image: "gsyrd", File: "naval yard.png", Bib: "", X: 3, Y: 3)
			})
			{
				var actor = rules.Actors[entry.Actor];
				var image = entry.Image + ".gdi.png";
				if (actor.TraitInfo<RenderSpritesInfo>().Image != image ||
					actor.TraitInfo<BuildingInfo>().Dimensions != new CVec(entry.X, entry.Y) || actor.HasTraitInfo<WithBuildingBibInfo>())
					throw new InvalidDataException(entry.Actor + ": wrong image, changed footprint or legacy bib.");
				var selection = actor.TraitInfo<IsometricSelectableInfo>();
				var box = selection.DecorationBounds;
				if (box == null || box.Length != 4 || selection.DecorationHeight <= 30 ||
					selection.Bounds != null || selection.Height != 0)
					throw new InvalidDataException(entry.Actor + ": missing calibrated decoration or changed mouse bounds.");
				var cells = entry.Bib.Length == 0 ? new[] { entry.X, entry.Y } : entry.Bib.Split('x').Select(int.Parse).ToArray();
				var cx = box[2] * grid.TileSize.Width / grid.TileScale;
				var cy = box[3] * grid.TileSize.Height / grid.TileScale;
				var half = box[0] * grid.TileSize.Width / (2 * grid.TileScale);
				if (box[0] != box[1] || Math.Abs(cx + 2 * cy) + half > cells[0] * grid.TileSize.Width / 2 + 2 ||
					Math.Abs(-cx + 2 * cy) + half > cells[1] * grid.TileSize.Width / 2 + 2)
					throw new InvalidDataException(entry.Actor + ": visual ground box extends beyond its bib.");
				if (entry.Bib.Length == 0)
				{
					if (actor.TraitInfos<WithIdleOverlayInfo>().Any(i => i.Image != null && i.Image.Contains("bib", StringComparison.OrdinalIgnoreCase)))
						throw new InvalidDataException(entry.Actor + ": naval yard must expose water, not a bib.");
				}
				else
				{
					var bib = actor.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == $"gdibib{entry.Bib}");
					if (!bib.IsDecoration || bib.RequiresCondition?.Expression != "!build-incomplete")
						throw new InvalidDataException(entry.Actor + ": invalid foundation.");
				}
				foreach (var state in sequences[image].Nodes.Where(n => !new[] { "make", "makenod", "icon", "Defaults" }.Contains(n.Key)))
				{
					var fields = state.Value.ToDictionary();
					if (!fields.TryGetValue("Filename", out var filename))
						continue;
					// Structure overlays are inherited unchanged; only body/resource states are replaced.
					var bodyState = state.Key.StartsWith("damaged-", StringComparison.Ordinal) ? state.Key.Substring(8) : state.Key;
					if (!new[] { "idle", "dead", "build", "pdox", "active", "resources", "place" }.Contains(bodyState))
						continue;
					var reference = entry.File == "weapons factory.png" || entry.File == "communication center.png";
					var expected = state.Key.Contains("resources", StringComparison.Ordinal) ? "bits/misc/overlays/blank.png" :
						"bits/gdi/buildings/" + (reference ? "" : "color-matched/") + entry.File;
					if (filename.Value != expected || fields["Start"].Value != "0" || fields["Length"].Value != "1" ||
						fields["Scale"].Value != "1" || !utility.ModData.DefaultFileSystem.Exists("ca|" + expected))
						throw new InvalidDataException(image + "." + state.Key + ": invalid PNG binding.");
				}
			}

			if (rules.Actors["weap.td"].HasTraitInfo<WithProductionDoorOverlayCAInfo>())
				throw new InvalidDataException("Supplied factory must not retain the legacy SHP door overlay.");

			foreach (var name in new[] { "nuke.nod", "afac.nod", "hq.nod", "rep.nod", "hpad.td.nod", "proc.td.nod", "fact.colorpicker" })
			{
				if (rules.Actors[name].TraitInfos<WithIdleOverlayInfo>().Any(i => i.Image?.StartsWith("gdibib", StringComparison.Ordinal) == true))
					throw new InvalidDataException(name + ": GDI foundation leaked into unrelated actor.");
				var selection = rules.Actors[name].TraitInfo<IsometricSelectableInfo>();
				var height = name == "afac.nod" ? 190 : name == "fact.colorpicker" ? 30 : 120;
				var validBounds = name == "afac.nod" ? selection.DecorationBounds?.SequenceEqual(new[] { 4344, 4344 }) == true : selection.DecorationBounds == null;
				if (!validBounds || selection.DecorationHeight != height || selection.Bounds != null || selection.Height != 0)
					throw new InvalidDataException(name + ": GDI selection alignment leaked into unrelated actor.");
			}
			if (rules.Actors["afac.nukular"].TraitInfo<RenderSpritesInfo>().Image != "afac.gdi.png")
				throw new InvalidDataException("Nukular GDI conyard must use the GDI artwork.");

			var conyardSelection = rules.Actors["afac"].TraitInfo<IsometricSelectableInfo>();
			var nukularSelection = rules.Actors["afac.nukular"].TraitInfo<IsometricSelectableInfo>();
			if (nukularSelection.DecorationHeight != conyardSelection.DecorationHeight ||
				nukularSelection.DecorationBounds == null || !nukularSelection.DecorationBounds.SequenceEqual(conyardSelection.DecorationBounds))
				throw new InvalidDataException("Nukular GDI conyard selection must follow its art.");

			var core = rules.Actors["fcom.checkpoint"];
			var husk = rules.Actors["fcom.checkpoint.husk"];
			if (core.TraitInfo<TooltipInfo>().Name != "Research Core" || !core.HasTraitInfo<CheckpointInfo>() ||
				!core.HasTraitInfo<CheckpointExperienceInfo>() || !core.HasTraitInfo<StrategicPointInfo>() ||
				core.TraitInfo<RenderSpritesInfo>().Image != "fcom.checkpoint" ||
				core.TraitInfo<SpawnActorOnDeathInfo>().Actor.ToLowerInvariant() != "fcom.checkpoint.husk" ||
				husk.TraitInfo<TransformOnCaptureInfo>().IntoActor != "fcom.checkpoint" ||
				rules.Actors["fcom"].TraitInfo<TooltipInfo>().Name != "Forward Command")
				throw new InvalidDataException("Research Core checkpoint/ruin or ordinary Forward Command changed incorrectly.");
			foreach (var state in new[] { "idle", "damaged-idle", "dead", "make" })
			{
				var fields = sequences["fcom.checkpoint"].Nodes.Single(n => n.Key == state).Value.ToDictionary();
				if (fields["Filename"].Value != "bits/misc/buildings/research core.png" || fields["Scale"].Value != "0.25" ||
					fields["Start"].Value != "0" || fields["Length"].Value != "1")
					throw new InvalidDataException("Research Core state: " + state);
			}
			Console.WriteLine("GDI building art and Research Core checks passed (13 buildings plus Nukular variant; isolated bibs; checkpoint restoration).");
		}
	}
}
