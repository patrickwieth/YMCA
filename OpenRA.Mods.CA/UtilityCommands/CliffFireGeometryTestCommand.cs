using System;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CliffFireGeometryTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--test-cliff-fire-geometry";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check native cliff/ramp collision symmetry and candidate edge tolerances.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 130, 146);
			map.SetBounds(new PPos(1, 17), new PPos(128, 144));
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(1000, 0);
			var center = new CPos(90, -20);
			for (var x = 0; x <= 8; x++) for (var y = -4; y <= 4; y++) map.Height[center + new CVec(x, y)] = 4;
			var low = map.CenterOfCell(center + new CVec(-8, 0));
			var edge = map.CenterOfCell(center);
			var deep = map.CenterOfCell(center + new CVec(3, 0));
			foreach (var tolerance in new[] { 0, 181, 362 })
			{
				foreach (var end in new[] { edge, deep })
				{
					var up = CliffFireControl.Trace(map, low, end, tolerance, out _);
					var down = CliffFireControl.Trace(map, end, low, tolerance, out _);
					if (up != down) throw new InvalidOperationException("Asymmetric cliff collision.");
					Console.WriteLine($"tolerance={tolerance} target={(end == edge ? "edge" : "deep")} blocked={up}");
				}
			}
			if (CliffFireControl.Trace(map, low, edge, 362, out _) || !CliffFireControl.Trace(map, low, deep, 362, out _))
				throw new InvalidOperationException("Edge/deep distinction failed.");
			foreach (var direction in new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1), new CVec(1, 1), new CVec(-1, 1), new CVec(1, -1), new CVec(-1, -1) })
			{
				var middle = center + new CVec(4, 0);
				var a = map.CenterOfCell(middle + direction * 12);
				var b = map.CenterOfCell(middle + direction * 4);
				var c = map.CenterOfCell(middle);
				if (CliffFireControl.Trace(map, a, b, 362, out _) || CliffFireControl.Trace(map, b, a, 362, out _) ||
					!CliffFireControl.Trace(map, a, c, 362, out _) || !CliffFireControl.Trace(map, c, a, 362, out _))
					throw new InvalidOperationException("Directional edge/deep test failed: " + direction);
			}
			var previous = low;
			for (var i = 1; i <= 256; i++)
			{
				var next = WPos.LerpQuadratic(low, deep, new WAngle(192), i, 256);
				if (CliffFireControl.Trace(map, previous, next, 362, out _)) throw new InvalidOperationException("High arc incorrectly blocked.");
				previous = next;
			}
			for (byte ramp = 1; ramp <= 4; ramp++)
			{
				var cell = center + new CVec(-12, 0);
				map.Ramp[cell] = ramp;
				WPos Surface(WVec offset)
				{
					var p = map.CenterOfCell(cell) + offset;
					return p - new WVec(0, 0, map.DistanceAboveTerrain(p).Length);
				}
				var a = Surface(new WVec(-128, -128, 0));
				var b = Surface(new WVec(128, 128, 0));
				if (CliffFireControl.Trace(map, a, b, 4, out _) || CliffFireControl.Trace(map, b, a, 4, out _))
					throw new InvalidOperationException("Planar ramp surface blocked.");
			}
			Console.WriteLine("PASS: native edge/deep reciprocity, high arc, four ramp orientations.");
		}
	}
}
