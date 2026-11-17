using System;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckRearRimTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-rear-rim-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 4;
		[Desc("SOURCE BEFORE AFTER", "Check source-derived rear crests without changes to front art or pixels outside the bounded rear band.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			using var source = File.OpenRead(Path.Combine(args[1], "grassland", "grassland_1x1.png")); var atlas = new Png(source);
			var changedFiles = 0; var changedPixels = 0; var files = 0;
			foreach (var file in Directory.GetFiles(args[2], "closed-cliff-*.png"))
			{
				var slot = int.Parse(Path.GetFileNameWithoutExtension(file).Split('-').Last()) % 100;
				using var beforeFile = File.OpenRead(file); var before = new Png(beforeFile);
				using var afterFile = File.OpenRead(Path.Combine(args[3], Path.GetFileName(file))); var after = new Png(afterFile);
				if (before.Width != after.Width || before.Height != after.Height || before.EmbeddedData.Count != after.EmbeddedData.Count ||
					before.EmbeddedData.Any(p => !after.EmbeddedData.TryGetValue(p.Key, out var value) || value != p.Value)) throw new InvalidDataException("Sprite geometry/metadata changed.");
				var expected = RubberduckRearRimArt.Apply(before.Data, atlas, slot, before.Height - 256);
				if (!expected.SequenceEqual(after.Data) || !RubberduckRearRimArt.Apply(before.Data, atlas, slot, before.Height - 256).SequenceEqual(expected)) throw new InvalidDataException("Unreproducible rear crest.");
				var changed = 0;
				for (var p = 0; p < before.Data.Length; p += 4)
				{
					if (Enumerable.Range(0, 4).All(channel => before.Data[p + channel] == after.Data[p + channel])) continue;
					var x = p / 4 % 128; var y = p / 512; var geometry = RubberduckClosedCliffGeometry.Column(slot, x + .5, y + .5 - (before.Height - 256));
					var depth = y + .5 - (before.Height - 256) - geometry.Roof;
					if (geometry.Drop != 64 || depth < -RubberduckRearRimArt.Width || depth >= 0 || after.Data[p + 3] != 255 ||
						Math.Max(after.Data[p], Math.Max(after.Data[p + 1], after.Data[p + 2])) <= 16) throw new InvalidDataException("Pixel change outside the rock crest or exposed black matte.");
					changed++;
				}
				if (changed != 0) changedFiles++; changedPixels += changed; files++;
			}
			if (files != 22 || changedFiles != 8 || changedPixels == 0) throw new InvalidDataException("Unexpected affected cliff vocabulary.");
			Console.WriteLine($"REAR RIM PASS {files} sprites, {changedFiles} rear/corner sprites changed, {changedPixels} bounded rock pixels; front faces, ramp mouths and sprite metadata unchanged; deterministic source sampling.");
		}
	}
}
