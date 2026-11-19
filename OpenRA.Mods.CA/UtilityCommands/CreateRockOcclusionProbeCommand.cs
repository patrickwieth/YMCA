using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CreateRockOcclusionProbeCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--create-rock-occlusion-probe";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 4;
		[Desc("Create an isolated rock/resource occlusion fixture.","OUTPUT RESULT_FILE baseline|fixed")]
		void IUtilityCommand.Run(Utility utility,string[] args)
		{
			var mod=Game.ModData=utility.ModData;
			if (File.Exists(args[1])) throw new InvalidDataException("Use a fresh fixture path.");
			using var map=new Map(mod,(DefaultTerrain)mod.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"],130,146);
			map.Title="Rock occlusion diagnostic "+args[3]; map.Author="YMCA"; map.RequiresMod="ca";
			map.SetBounds(new PPos(1,17),new PPos(128,144));
			var players=new MapPlayers(map.Rules,2);
			players.Players["Multi0"].Faction="gdi";
			players.Players["Multi0"].Enemies=new[] { "Multi1" };
			players.Players["Multi1"].Enemies=new[] { "Multi0" };
			players.Players["Multi1"].Playable=false; players.Players["Multi1"].Faction="nod";
			map.PlayerDefinitions=players.ToMiniYaml();
			foreach (var cell in map.AllCells) map.Tiles[cell]=new TerrainTile(1000,0);
			var center=new CPos(110,-25);
			var slopes=args[3].StartsWith("slope",StringComparison.Ordinal);
			var surfaces=slopes ? PlateauTopology.CalibrationPlateau(center) : new Dictionary<CPos,PlateauSurface>();
			if (!slopes) for (var x=-4;x<=4;x++) for (var y=-4;y<=4;y++) surfaces[center+new CVec(x,y)]=new PlateauSurface(4,0);
			var actors=new List<MiniYamlNode>();
			RubberduckPlateauRenderer.ComposeNative(map,surfaces,actors,new HashSet<(CPos,int)>(),Array.Empty<(CPos,int)>(),"Rock",true);
			void Add(string id,string type,int x,int y,string owner="Multi0")
			{
				var a=new ActorReference(type) { new OwnerInit(owner),new LocationInit(center+new CVec(x,y)) };
				if (mod.DefaultRules.Actors[type].HasTraitInfo<AutoTargetInfo>()) a.Add(new StanceInit(mod.DefaultRules.Actors[type].TraitInfo<AutoTargetInfo>(),UnitStance.HoldFire));
				actors.Add(new MiniYamlNode(id,a.Save()));
			}
			if (slopes)
			{
				Add("Spawn","mpspawn",-12,-12,"Neutral");
				Add("FullyHidden","mtnk",-8,-8);
				Add("PartlyHidden","mtnk",-9,-9);
				Add("Unobstructed","ltnk",-11,-11);
				Add("FrontTank","mtnk",10,10);
				Add("RoofTank","mtnk",0,0);
				for (var direction=0;direction<4;direction++)
					foreach (var distance in new[] { 6,4 })
					{
						var offset=PlateauTopology.Directions[direction]*distance;
						Add("Ramp"+direction+"Level"+distance,"mtnk",offset.X,offset.Y);
					}
			}
			else
			{
			Add("Spawn","mpspawn",-8,-8,"Neutral");
			Add("RearTank","mtnk",-6,-6); Add("RearVehicle","2tnk",-7,-4);
			Add("Infantry","mercenary",-5,2);
			Add("SideTank","ltnk",-5,0); Add("FrontTank","mtnk",6,6);
			Add("RoofTank","mtnk",0,0); Add("CloakedEnemy","stnk",-4,-7,"Multi1");
			Add("FogEnemy","ltnk",-20,-20,"Multi1");
			for (var x=-9;x<=-5;x++) for (var y=-9;y<=-5;y++)
				map.Resources[center+new CVec(x,y)]=new ResourceTile((byte)((x+y)%2==0 ? 3 : 4),12);
			}
			map.ActorDefinitions=actors;
			var enabled=!args[3].EndsWith("baseline",StringComparison.Ordinal) ? "true" : "false";
			map.RuleDefinitions=new MiniYaml("",MiniYaml.FromString($@"
World:
	-SpawnStartingUnits:
	-NukularDoubleStartingUnits:
	RockOcclusionProbe:
		ResultPath: {Path.GetFullPath(args[2]).Replace('\\','/')}
		Baseline: {enabled == "false"}
		SlopeCases: {slopes}
	OccludedResourceRenderer:
		ClipToRaisedTerrain: {enabled}
Player:
	-ConquestVictoryConditions:
	Shroud:
		ExploredMapCheckboxEnabled: true
		FogCheckboxEnabled: true
^SelectableCombatUnit:
	WithRockOcclusion:
		Enabled: {enabled}
^SelectableSupportUnit:
	WithRockOcclusion:
		Enabled: {enabled}
^SelectableEconomicUnit:
	WithRockOcclusion:
		Enabled: {enabled}
", "rock-occlusion-probe"));
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
			using (var package=ZipFileLoader.Create(args[1])) map.Save(package);
			using var input=File.OpenRead(args[1]);
			using var zip=new ZipFileLoader.ReadOnlyZipFile(input,args[1]);
			using var verify=new Map(mod,zip);
			if (verify.InvalidCustomRules) throw new InvalidDataException("Invalid fixture rules.",verify.InvalidCustomRulesException);
			Console.WriteLine(args[1]);
		}
	}
}
