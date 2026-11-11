using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-guard-tower-art", "Load the supplied native GDI Guard Tower and validate its actor binding.")]
	public sealed class CheckGuardTowerArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-guard-tower-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["gtwr"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.TraitInfo<RenderSpritesInfo>().Image != "gtwr.gdi.png" ||
				actor.TraitInfo<TurretedInfo>().Offset != new WVec(0, 0, 2007))
				throw new InvalidDataException("Guard Tower image/muzzle-height binding mismatch.");
			var bib = actor.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == "gdibib1x1");
			if (!bib.IsDecoration || bib.RequiresCondition?.Expression != "!build-incomplete")
				throw new InvalidDataException("Guard Tower requires its decorative 1x1 bib.");
			using (var bibStream = mod.DefaultFileSystem.Open("bits/gdi/buildings/bib1x1.png"))
			{
				var bibFrames = FrameLoader.GetFrames(bibStream, mod.SpriteLoaders, "bits/gdi/buildings/bib1x1.png", out _);
				if (bibFrames.Length != 1 || bibFrames[0].Size.Width != 128 || bibFrames[0].Size.Height != 64)
					throw new InvalidDataException("Invalid native Guard Tower bib.");
			}
			const string Filename = "bits/gdi/buildings/color-matched/guard tower.png";
			using var stream = mod.DefaultFileSystem.Open(Filename);
			var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, Filename, out _);
			if (frames.Length != 1 || frames[0].Size.Width != 128 || frames[0].Size.Height != 150 ||
				frames[0].Offset != float2.Zero || frames[0].Data.Length != 128 * 150 * 4)
				throw new InvalidDataException("Invalid native Guard Tower PNG frame.");
			Console.WriteLine("GTWR: engine loaded the color-matched native 128x150 PNG; actor construction passed. Live appearance/firing pending.");
		}
	}
}
