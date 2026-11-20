using System;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckEliteCyborgIconsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-elite-cyborg-icons";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("Check resolved Elite Cyborg unit/promotion icon bindings and export both palette interpretations.","OUTPUTDIR")]
		void IUtilityCommand.Run(Utility utility,string[] args)
		{
			var mod=Game.ModData=utility.ModData;
			var nodes=MiniYaml.Load(mod.DefaultFileSystem,mod.Manifest.Sequences,null).ToDictionary(n=>n.Key);
			string file=null;
			foreach(var name in new[] { "cyborgelite", "cyborgelite.range", "promotion.elite_cyborg", "promotion.elite_cyborg.range" })
			{
				var actor=mod.DefaultRules.Actors[name];
				var build=actor.TraitInfo<BuildableInfo>();
				var image=actor.TraitInfo<RenderSpritesInfo>().GetImage(actor,"nod");
				var icon=nodes[image].Value.Nodes.Single(n=>n.Key==build.Icon);
				var filename=icon.Value.Nodes.Single(n=>n.Key=="Filename").Value.Value;
				if(build.IconPalette!="chrometd" || filename!="rmbocicnh.shp") throw new InvalidDataException("Wrong resolved cameo: "+name+" "+filename+" "+build.IconPalette);
				file=filename;
				Console.WriteLine(name+": "+image+"/"+build.Icon+" -> "+filename+" palette="+build.IconPalette);
			}
			Directory.CreateDirectory(args[1]);
			using var stream=mod.DefaultFileSystem.Open(file);
			var frame=FrameLoader.GetFrames(stream,mod.SpriteLoaders,file,out _)[0];
			foreach(var name in new[] { "chrome", "chrometd" })
			{
				var info=mod.DefaultRules.Actors["world"].TraitInfos<IProvidesCursorPaletteInfo>()
					.Single(p=>(string)p.GetType().GetField("Name")?.GetValue(p)==name);
				var palette=info.ReadPalette(mod.DefaultFileSystem);
				var colors=Enumerable.Range(0,256).Select(palette.GetColor).ToArray();
				new Png(frame.Data,SpriteFrameType.Indexed8,frame.Size.Width,frame.Size.Height,colors).Save(Path.Combine(args[1],name+".png"));
			}
			Console.WriteLine("PASS: production and promotion variants use the same intended TD cameo palette.");
		}
	}
}
