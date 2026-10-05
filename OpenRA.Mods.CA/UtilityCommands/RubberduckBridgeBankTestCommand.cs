using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckBridgeBankTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-bridge-bank-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Check bank clipping coverage, intact deck pixels, both axes and no detached lower pier fragments.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 64, 96);
			map.SetBounds(new PPos(1, 1), new PPos(62, 94));
			var start = map.CenterOfCell(new MPos(30, 35).ToCPos(map));
			var cases = 0;
			foreach (var tile in new ushort[] { 1000, 1030, 1050, 3992 })
				foreach (var alongY in new[] { false, true })
					foreach (var scale in new[] { .8f, 1f, 1.2f })
						foreach (var repeat in new[] { 0, 2 })
						{
							foreach (var c in map.AllCells) map.Tiles[c] = new TerrainTile(tile, 0);
							var body = new RubberduckBridgeBody(new RubberduckBridgeBodyInfo(9, alongY));
							var coverage = new int[256 * 256];
							foreach (var r in body.BankClip(map, start, repeat, scale))
								for (var y = r.Top; y < r.Bottom; y++)
									for (var x = r.Left; x < r.Right; x++) coverage[y * 256 + x]++;
							if (coverage.Any(n => n > 1)) throw new InvalidDataException("Clipped deck rectangles overlap.");
							for (var x = 0; x < 256; x++)
							{
								var hidden = false;
								for (var y = 0; y < 256; y++)
								{
									var visible = coverage[y * 256 + x] == 1;
									if (y <= x * .5 + 112 && !visible) throw new InvalidDataException("Bank clipping removed road/rail pixels.");
									if (hidden && visible) throw new InvalidDataException("Clipped pier has a detached lower fragment.");
									hidden |= !visible;
								}
							}
							if ((tile == 1050 || tile == 3992) && coverage.Any(n => n != 1))
								throw new InvalidDataException("Water/blocked ground falsely hid a pier.");
							if ((tile == 1000 || tile == 1030) && coverage.All(n => n == 1))
								throw new InvalidDataException("Dry bank did not clip any lower pier pixels.");
							cases++;
						}
			Console.WriteLine($"BRIDGE BANK PASS {cases} masks; intact roads, exact rectangle coverage and no detached pier feet.");
		}
	}
}
