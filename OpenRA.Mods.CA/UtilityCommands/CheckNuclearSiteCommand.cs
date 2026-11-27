using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckNuclearSiteCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-nuclear-site";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length==1;
		[Desc("Verify the nuclear site's resolved 4x3 grid, selection and actual bib sprite registration.")]
		void IUtilityCommand.Run(Utility utility,string[] args)
		{
			var mod=Game.ModData=utility.ModData;var actor=mod.DefaultRules.Actors["apwr.soviet"];
			var building=actor.TraitInfo<BuildingInfo>();var selection=actor.TraitInfo<IsometricSelectableInfo>();
			if(building.Dimensions!=new CVec(4,3) || building.LocalCenterOffset!=WVec.Zero || selection.DecorationBounds!=null || selection.DecorationFootprint!=new CVec(4,3))throw new InvalidDataException("Nuclear site/selection mismatch.");
			var bib=actor.TraitInfos<WithIdleOverlayInfo>().Single(b=>b.Image=="sovietbib4x3");
			var worldOffset=actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(bib.Offset);
			var node=MiniYaml.Load(mod.DefaultFileSystem,mod.Manifest.Sequences,null).Single(n=>n.Key==bib.Image);
			using var cache=new SpriteCache(mod.DefaultFileSystem,mod.SpriteLoaders,mod.SpriteSequenceLoader.BgraSheetSize,mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences=mod.SpriteSequenceLoader.ParseSequences(mod,"TEMPERAT",cache,node);cache.LoadReservations(mod);
			foreach(var name in new[]{"grid","remap"})
			{
				var sequence=sequences[name];sequence.ResolveSprites(cache);var s=sequence.GetSprite(0);
				var x=worldOffset.X*128f/1448+s.Offset.X*sequence.Scale;
				var y=worldOffset.Y*64f/1448+s.Offset.Y*sequence.Scale;
				if(Math.Abs(x)>.1 || Math.Abs(y)>.1)throw new InvalidDataException(name+": bib is not centered on its cells: "+x+","+y);
				Console.WriteLine(name+": actual ground offset "+x+","+y+"; 4x3 bib and cell-axis selection aligned.");
			}
		}
	}
}
