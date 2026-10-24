using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OpenRA.FileFormats;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckCliffVisualTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-cliff-visual-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("ART_DIRECTORY", "Check deterministic shared cliff variation, corner families, exact wall masks and bounded overlays.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			using var faceFile = File.OpenRead(Path.Combine(args[1], "mountain-faces.png")); var faces = new Png(faceFile);
			using var cornerFile = File.OpenRead(Path.Combine(args[1], "mountain-corners.png")); var corners = new Png(cornerFile);
			var donor = new byte[128 * 256 * 4];
			for (var direction = 0; direction < 4; direction++)
			{
				var mask = RubberduckPlateauRenderer.Face(direction, 4, 4, donor);
				var unique = new HashSet<string>();
				for (var variant = 0; variant < 9; variant++)
				{
					var frame = new byte[128 * 320 * 4];
					for (var y = 0; y < 320; y++) Array.Copy(faces.Data, ((direction * 320 + y) * faces.Width + variant * 128) * 4, frame, y * 512, 512);
					for (var p = 3; p < frame.Length; p += 4)
						if (frame[p] != mask[p]) throw new InvalidDataException("Wall silhouette changed.");
					unique.Add(Convert.ToHexString(SHA256.HashData(frame)));
				}
				if (unique.Count != 9) throw new InvalidDataException($"Direction {direction} does not have nine distinct variants.");
				for (var end = 0; end < 2; end++)
					for (var kind = 0; kind < 2; kind++)
					{
						var frame = (direction * 2 + end) * 2 + kind; var count = 0;
						for (var y = 0; y < 320; y++)
							for (var x = 0; x < 128; x++)
							{
								var alpha = corners.Data[((frame / 4 * 320 + y) * corners.Width + frame % 4 * 128 + x) * 4 + 3];
								if (alpha != 0) count++;
								if (alpha > mask[(y * 128 + x) * 4 + 3]) throw new InvalidDataException("Corner extends beyond the existing wall.");
							}
						if (count == 0 || count > 12 * 128) throw new InvalidDataException("Corner overlay has an invalid extent.");
					}
			}
			foreach (var step in new[] { new CVec(1, 0), new CVec(0, 1), new CVec(1, 1), new CVec(1, -1) })
			{
				var used = new HashSet<int>();
				for (var i = -128; i <= 128; i++)
				{
					var cell = new CPos(i * step.X, i * step.Y);
					var variant = RubberduckCliffVisuals.Variant(cell, 9);
					if (variant != RubberduckCliffVisuals.Variant(cell, 9)) throw new InvalidDataException("Unstable cliff selection.");
					used.Add(variant);
				}
				if (used.Count != 9) throw new InvalidDataException("A ridge direction loses material variants.");
			}
			foreach (var slot in new[] { 16, 18, 20, 22, 24, 26, 28, 30, 100, 105 })
				if (RubberduckCliffVisuals.StraightSlot(slot, new CPos(-37, 92)) != slot) throw new InvalidDataException("Corner/ramp vocabulary changed.");
			Console.WriteLine("CLIFF VISUAL PASS: 36 distinct source-window faces, identical alpha masks, 16 bounded corner overlays; stable variation on axes/diagonals, explicit corners/ramps retained.");
		}
	}
}
