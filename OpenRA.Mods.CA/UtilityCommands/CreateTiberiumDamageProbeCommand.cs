using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CreateTiberiumDamageProbeCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--create-tiberium-damage-probe";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 4 && new[] { "default", "on", "off" }.Contains(args[3]);
		[Desc("Validate the lobby option and create isolated native infantry damage checks.","OUTPUT RESULTS default|on|off")]
		void IUtilityCommand.Run(Utility utility,string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var option = mod.DefaultRules.Actors["player"].TraitInfos<LobbyPrerequisiteCheckboxInfo>().Single(i=>i.ID=="tiberiumdamage");
			if (option.Enabled || !option.Visible || option.Locked || !option.Prerequisites.Contains("global.tiberiumdamage"))
				throw new InvalidDataException("Tiberium checkbox must be visible, unlocked and default-off.");
			var damage=mod.DefaultRules.Actors["mercenary"].TraitInfos<DamagedByTerrainInfo>().Single(i=>i.Terrain.Contains("Tiberium"));
			if (damage.Damage!=100 || damage.DamageInterval!=16 || !damage.RequiresCondition.Variables.Contains("tiberium-damage-enabled"))
				throw new InvalidDataException("Existing damage must be gated, not rebalanced.");
			var symbols=new Dictionary<string,int>();
			if(damage.RequiresCondition.Evaluate(symbols)) throw new InvalidDataException("Damage enabled without the lobby prerequisite.");
			symbols["tiberium-damage-enabled"]=1;
			if(!damage.RequiresCondition.Evaluate(symbols)) throw new InvalidDataException("Enabled option does not allow damage.");
			foreach(var immunity in new[] { "hazmatsuits", "being-warped" })
			{
				symbols[immunity]=1;
				if(damage.RequiresCondition.Evaluate(symbols)) throw new InvalidDataException("Immunity lost: "+immunity);
				symbols.Remove(immunity);
			}
			if (File.Exists(args[1])) throw new InvalidDataException("Use a fresh output.");
			using var map=new Map(mod,(DefaultTerrain)mod.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"],130,146);
			map.Title="Tiberium damage "+args[3];map.Author="YMCA";map.RequiresMod="ca";
			map.SetBounds(new PPos(1,17),new PPos(128,144));
			map.PlayerDefinitions=new MapPlayers(map.Rules,1).ToMiniYaml();
			foreach(var cell in map.AllCells) map.Tiles[cell]=new TerrainTile(1000,0);
			var actors=new List<MiniYamlNode>();
			void Add(string name,string type,int x,int resource=0,string owner="Multi0")
			{
				var cell=new CPos(104+x,-25);
				var a=new ActorReference(type) { new OwnerInit(owner),new LocationInit(cell) };
				actors.Add(new MiniYamlNode(name,a.Save()));
				if(resource!=0) map.Resources[cell]=new ResourceTile((byte)resource,12);
			}
			Add("Spawn","mpspawn",0,0,"Neutral");
			Add("Green","mercenary",0,3);Add("Blue","mercenary",2,4);
			Add("Clear","mercenary",4);Add("Hazmat","mercenary",6,3);
			Add("Immune","stormtrooper",8,4);Add("Scrin","s1",10,3);Add("Vehicle","mtnk",12,3);
			map.ActorDefinitions=actors;
			map.RuleDefinitions=new MiniYaml("",MiniYaml.FromString($@"
World:
	-SpawnStartingUnits:
	-NukularDoubleStartingUnits:
	TiberiumDamageProbe:
		Mode: {args[3]}
		ResultPath: {Path.GetFullPath(args[2]).Replace(Path.DirectorySeparatorChar,'/')}
Player:
	-ConquestVictoryConditions:
", "tiberium-damage-probe"));
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
			using(var package=ZipFileLoader.Create(args[1])) map.Save(package);
			using var input=File.OpenRead(args[1]);using var zip=new ZipFileLoader.ReadOnlyZipFile(input,args[1]);using var verify=new Map(mod,zip);
			if(verify.InvalidCustomRules) throw new InvalidDataException("Invalid fixture",verify.InvalidCustomRulesException);
			Console.WriteLine("PASS default-off option and preserved damage; fixture "+args[1]);
		}
	}
}
