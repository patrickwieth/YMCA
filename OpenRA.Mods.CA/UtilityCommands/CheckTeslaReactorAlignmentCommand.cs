using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.HitShapes;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckTeslaReactorAlignmentCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-tesla-reactor-alignment";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check the resolved Tesla Reactor body, collision and foundation registration.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var actor = utility.ModData.DefaultRules.Actors["tpwr"];
			var building = actor.TraitInfo<BuildingInfo>();
			if (building.Dimensions != new CVec(3, 3) || building.LocalCenterOffset != new WVec(0, -724, 0) ||
				building.OccupiedTiles(CPos.Zero).Count() != 4 || building.PathableTiles(CPos.Zero).Count() != 5)
				throw new InvalidDataException("Tesla Reactor must retain its active 2x2 body / 3x3 site.");
			for (var y = 0; y < 3; y++)
				for (var x = 0; x < 3; x++)
					if (building.Footprint[new CVec(x, y)] != (x < 2 && y < 2 ? FootprintCellType.Occupied : FootprintCellType.OccupiedPassable))
						throw new InvalidDataException("Tesla Reactor occupied/passable cell changed.");
			var bib = actor.TraitInfos<WithIdleOverlayInfo>().Single(b => b.Image == "sovietbib3x3");
			var worldOffset = actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(bib.Offset.Rotate(WRot.None));
			if (worldOffset + building.LocalCenterOffset != WVec.Zero)
				throw new InvalidDataException("Tesla foundation is not centered on its actual placement site.");
			if (actor.TraitInfo<HitShapeInfo>().Type is not RectangleShape shape ||
				shape.TopLeft != new int2(-1024, -1024) || shape.BottomRight != new int2(1024, 1024))
				throw new InvalidDataException("Tesla collision does not match its 2x2 occupied body.");
			if (!actor.TraitInfo<IsometricSelectableInfo>().DecorationBounds.SequenceEqual(new[] { 2896, 2896, 0, 0 }))
				throw new InvalidDataException("Tesla selection base is not aligned with its occupied body.");
			Console.WriteLine("PASS: Tesla 3x3 site, four occupied/five passable cells, centered bib, 2x2 collision and selection.");
		}
	}
}
