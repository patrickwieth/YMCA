using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckPaletteCopyTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-palette-copy-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Compare bulk pulse-palette updates with the original per-color loop, including protected entries.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var checks = 0;
			for (var phase = 0; phase < 26; phase++)
				foreach (var shadow in new int?[] { null, -1, 0, 4, 255, 256 })
				{
					var source = new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => unchecked((uint)(i + phase * 256) * 0x174A0F1Bu)));
					var seed = new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => unchecked((uint)(i * 32771 + phase) * 0x1234567u)));
					var expected = new MutablePalette(seed);
					var actual = new MutablePalette(seed);
					for (var i = 1; i < Palette.Size; i++)
						if (shadow == null || shadow != i) expected.SetColor(i, source.GetColor(i));
					PhasingPaletteEffect.CopyPulse(actual, source, shadow);
					for (var i = 0; i < Palette.Size; i++)
					{
						if (expected[i] != actual[i]) throw new InvalidOperationException("Pulse palette copy changed color or a protected entry.");
						checks++;
					}
				}
			foreach (var info in utility.ModData.DefaultRules.Actors["world"].TraitInfos<TAStealthTankCloakPaletteEffectInfo>())
			{
				var effect = (IPaletteModifier)new TAStealthTankCloakPaletteEffect(info);
				var palettes = new Dictionary<string, MutablePalette>();
				for (var pass = 0; pass < 4; pass++)
				{
					var seed = new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => unchecked((uint)(i + pass) * 0x174A0F1Bu)));
					if (pass % 2 == 0) palettes[info.AffectedPalette + pass] = new MutablePalette(seed);
					palettes["unrelated"] = new MutablePalette(seed);
					foreach (var name in palettes.Keys.ToArray()) palettes[name] = new MutablePalette(seed);
					var expected = palettes.ToDictionary(p => p.Key, p => new MutablePalette(p.Value));
					((IPaletteModifier)new TAStealthTankCloakPaletteEffect(info)).AdjustPalette(expected);
					effect.AdjustPalette(palettes);
					foreach (var entry in palettes)
						for (var i = 0; i < Palette.Size; i++)
							if (entry.Value[i] != expected[entry.Key][i]) throw new InvalidOperationException("Cloak palette cache did not follow append/replacement.");
				}
			}
			Console.WriteLine($"PASS: {checks} exact ARGB pulse comparisons; cloak palette append/replacement parity.");
		}
	}
}
