using System;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckRearLineTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-rear-line-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Check that the shared rear-lip cutoff excludes the opaque source-matte wire, retaining the stone lip.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			if (RubberduckClosedCliffGeometry.RearLipDepth != 3)
				throw new InvalidDataException("Rear lip must retain three source pixels, not the fourth matte row.");
			foreach (var slot in new[] { 8, 9, 10, 12, 13, 14 })
			{
				using var stream = utility.ModData.DefaultFileSystem.Open($"ca|bits/terrain/rubberduck/derived/closed-cliffs/closed-cliff-{slot}.png");
				var image = new Png(stream);
				var oldMatte = 0; var newMatte = 0; var oldStone = 0; var retainedStone = 0;
				for (var x = 0; x < 128; x++)
				{
					if (slot < 12 ? x > 65 : x < 62) continue;
					var roof = RubberduckClosedCliffGeometry.Column(slot, x + .5).Roof;
					for (var y = 0; y < image.Height; y++)
					{
						var depth = y + .5 - roof;
						if (depth < 0 || depth > 4) continue;
						var p = (y * 128 + x) * 4;
						if (image.Data[p + 3] != 255) continue;
						var matte = Math.Max(image.Data[p], Math.Max(image.Data[p + 1], image.Data[p + 2])) <= 8;
						if (matte) oldMatte++; else oldStone++;
						if (depth > RubberduckClosedCliffGeometry.RearLipDepth) continue;
						if (matte) newMatte++; else retainedStone++;
					}
				}
				// Isolated natural dark details/end pixels are not a continuous matte row.
				if (oldMatte - newMatte < 60 || newMatte > 3 || oldStone == 0 || retainedStone < oldStone * .95)
					throw new InvalidDataException($"Unexpected rear contact mask in slot {slot}: {oldMatte}/{newMatte}/{retainedStone}.");
				Console.WriteLine($"REAR LINE PASS slot={slot}: matte {oldMatte}->{newMatte}, stone retained={retainedStone}; artwork unmodified.");
			}
		}
	}
}
