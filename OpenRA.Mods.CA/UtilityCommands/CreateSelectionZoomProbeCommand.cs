using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CreateSelectionZoomProbeCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--create-selection-zoom-probe";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("Create isolated selection settings/rendering fixture.", "OUTPUT RESULTS")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			if (File.Exists(args[1])) throw new InvalidDataException("Use a fresh output path.");
			using var map = new Map(mod, (DefaultTerrain)mod.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 130, 146);
			map.Title = "Selection zoom diagnostic"; map.Author = "YMCA"; map.RequiresMod = "ca";
			map.SetBounds(new PPos(1,17), new PPos(128,144));
			map.PlayerDefinitions = new MapPlayers(map.Rules, 1).ToMiniYaml();
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(1000,0);
			var actors = new List<MiniYamlNode>();
			void Add(string name, string type, int x, int y, string owner = "Multi0")
			{
				if (!mod.DefaultRules.Actors.ContainsKey(type)) throw new InvalidDataException("Unknown actor " + type);
				var a = new ActorReference(type) { new OwnerInit(owner), new LocationInit(new CPos(110+x,-25+y)) };
				actors.Add(new MiniYamlNode(name, a.Save()));
			}
			Add("Spawn", "mpspawn", 0, 0, "Neutral");
			for (var i=0;i<6;i++) Add("Infantry"+i, i%2==0 ? "grenadier" : "mercenary", i-3, 0);
			Add("Tank", "mtnk", 0, 3); Add("Building", "barr", 4, 0);
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($@"
World:
	-SpawnStartingUnits:
	-NukularDoubleStartingUnits:
	SelectionZoomProbe:
		ResultPath: {Path.GetFullPath(args[2]).Replace(Path.DirectorySeparatorChar,'/')}
Player:
	-ConquestVictoryConditions:
", "selection-zoom-probe"));
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
			using (var package = ZipFileLoader.Create(args[1])) map.Save(package);
			using var input = File.OpenRead(args[1]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(input,args[1]);
			using var verify = new Map(mod,zip);
			if (verify.InvalidCustomRules) throw new InvalidDataException("Invalid fixture",verify.InvalidCustomRulesException);
			Console.WriteLine(args[1]);
		}
	}
}
