using System;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CreateMissileHeightProbeCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--create-missile-height-probe";
		bool IUtilityCommand.ValidateArguments(string[] args) => (args.Length == 3 || args.Length == 4 && args[3] == "water" && args[2] == "0") && byte.TryParse(args[2], out var height) && height <= 12;
		[Desc("OUTPUT HEIGHT [water]",  "Create an isolated uniform-height missile physics capture map.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			if (File.Exists(args[1])) throw new InvalidDataException("Use a fresh output path.");
			var height = byte.Parse(args[2]);
			var water = args.Length == 4;
			using var map = new Map(mod, (DefaultTerrain)mod.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 226, 226);
			map.SetBounds(new PPos(16, 16), new PPos(209, 209));
			map.Title = "[REVIEW ONLY] Missile height " + height; map.Author = "YMCA"; map.RequiresMod = "ca";
			map.PlayerDefinitions = new MapPlayers(map.Rules, 1).ToMiniYaml();
			foreach (var cell in map.AllCells) { map.Tiles[cell] = new TerrainTile((ushort)(water ? 1050 : 1000), 0); map.Height[cell] = height; }
			map.ActorDefinitions = new[] { new MiniYamlNode("Spawn", new ActorReference("mpspawn") { new OwnerInit("Neutral"), new LocationInit(new CPos(112, 0)) }.Save()) };
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($@"
World:
	-SpawnStartingUnits:
	-NukularDoubleStartingUnits:
	MissileHeightProbe:
		Water: {water}
	TerrainCalibrationView:
		Center: 112,0
		ZoomScale: 0.2
		MinimumZoomScale: 0.04
		ScreenshotTicks: 60
Player:
	-ConquestVictoryConditions:
	Shroud:
		FogCheckboxEnabled: false
		FogCheckboxLocked: true
		ExploredMapCheckboxEnabled: true
		ExploredMapCheckboxLocked: true
", "missile-height-probe"));
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
			using var output = ZipFileLoader.Create(args[1]); map.Save(output);
		}
	}
}
