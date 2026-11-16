using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-soviet-helipad-art", "Load the supplied native Soviet helipad and check actor dependencies.")]
	public sealed class CheckSovietHelipadArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-soviet-helipad-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["hpad.soviet"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.TraitInfo<RenderSpritesInfo>().Image != "hpad.soviet")
				throw new InvalidDataException("Incorrect Soviet helipad image namespace.");
			const string Path = "bits/soviet/buildings/helipad.png";
			using var stream = mod.DefaultFileSystem.Open(Path);
			var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, Path, out _);
			if (frames.Length != 1 || frames[0].Size.Width != 194 || frames[0].Size.Height != 135 || frames[0].FrameSize != frames[0].Size)
				throw new InvalidDataException("Soviet helipad must load as one native194x135 frame.");
			Console.WriteLine("Soviet helipad: native194x135 PNG and actor dependencies valid. Live landing/reload check pending.");
		}
	}
}
