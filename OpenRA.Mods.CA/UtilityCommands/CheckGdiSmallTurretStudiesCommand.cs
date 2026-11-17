using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-gdi-small-turret-studies", "Load native AA/Sonic study sheets (run from the Game directory; does not register actors).")]
	public sealed class CheckGdiSmallTurretStudiesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-gdi-small-turret-studies";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			foreach (var study in new[] { (Folder: "gdi aa gun", Size: 192), (Folder: "gdi sonic turret", Size: 256) })
				foreach (var part in new[] { (File: "base.png", Count: 1), (File: "turret32.png", Count: 32) })
				{
					var filename = Path.Combine("art-source", study.Folder, "pass-05", "review", part.File);
					using var stream = File.OpenRead(filename);
					var frames = FrameLoader.GetFrames(stream, utility.ModData.SpriteLoaders, filename, out _);
					if (frames.Length != part.Count || frames.Any(f => f.Size.Width != study.Size || f.Size.Height != study.Size ||
						f.FrameSize != f.Size || f.Offset != float2.Zero || f.Data.Length != study.Size * study.Size * 4))
						throw new InvalidDataException(filename + ": wrong native frames or PNG metadata.");
					Console.WriteLine($"{study.Folder}/{part.File}: loaded {frames.Length} native {study.Size}px RGBA frame(s).");
				}
			Console.WriteLine("Study loaders passed. No gameplay actors installed; no live game visual/firing review.");
		}
	}
}
