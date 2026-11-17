using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-nod-laser-turret-study", "Load the native Nod laser study exports; run from Game. Does not register actors.")]
	public sealed class CheckNodLaserTurretStudyCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-nod-laser-turret-study";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			foreach (var part in new[] { (File: "base.png", Count: 1), (File: "turret32.png", Count: 32) })
			{
				var filename = Path.Combine("art-source", "nod laser turret", "pass-04", "review", part.File);
				using var stream = File.OpenRead(filename);
				var frames = FrameLoader.GetFrames(stream, utility.ModData.SpriteLoaders, filename, out _);
				if (frames.Length != part.Count || frames.Any(f => f.Size.Width != 256 || f.Size.Height != 256 ||
					f.FrameSize != f.Size || f.Offset != float2.Zero || f.Data.Length != 256 * 256 * 4))
					throw new InvalidDataException(filename + ": incorrect native PNG frames.");
				Console.WriteLine($"Nod laser {part.File}: {frames.Length} native 256px RGBA frame(s).");
			}
			Console.WriteLine("Study loaders passed. No actor binding or live firing/visual validation.");
		}
	}
}
