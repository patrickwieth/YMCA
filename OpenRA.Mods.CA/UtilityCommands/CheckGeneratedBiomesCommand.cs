using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckGeneratedBiomesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-generated-biomes";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT_DIRECTORY", "Compare generated biome maps against the same layout without cosmetic scenery.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod=Game.ModData=utility.ModData;var root=Path.GetFullPath(args[1]);Directory.CreateDirectory(root);
			foreach(var preset in new[]{"Arabia","MountainValleys","TwoSides","Continents","Migration","Archipelago"})
			foreach(var players in new[]{2,4})
			{
				var options=DebugMapGeneratorCommand.ParseOptions(new[]{"check",root,"preset="+preset,"players="+players,"seed=42","size=auto"});
				var before=new RubberduckMapExporter(mod,false).Export(MapPlanGenerator.Generate(options),options,Path.Combine(root,preset+players,"before"));
				var after=new RubberduckMapExporter(mod).Export(MapPlanGenerator.Generate(options),options,Path.Combine(root,preset+players,"after"));
				using var aStream=File.OpenRead(before);using var aZip=new ZipFileLoader.ReadOnlyZipFile(aStream,before);using var a=new Map(mod,aZip);
				using var bStream=File.OpenRead(after);using var bZip=new ZipFileLoader.ReadOnlyZipFile(bStream,after);using var b=new Map(mod,bZip);
				if(b.InvalidCustomRules)throw new InvalidDataException("Biome rules failed.",b.InvalidCustomRulesException);
				foreach(var c in a.AllCells)
				{
					if(a.Height[c]!=b.Height[c] || a.Ramp[c]!=b.Ramp[c] || a.Resources[c].Type!=b.Resources[c].Type || a.Resources[c].Index!=b.Resources[c].Index || a.GetTerrainInfo(c).Type!=b.GetTerrainInfo(c).Type)
						throw new InvalidDataException(preset+": gameplay terrain/resources changed at "+c);
				}
				var original=a.ActorDefinitions.WriteToString();
				if(original!=b.ActorDefinitions.Where(n=>!n.Key.StartsWith("BiomeTree",StringComparison.Ordinal)).WriteToString())throw new InvalidDataException(preset+": original actors changed.");
				var trees=b.ActorDefinitions.Where(n=>n.Key.StartsWith("BiomeTree",StringComparison.Ordinal)).ToArray();
				if(preset is "Arabia" or "MountainValleys" or "TwoSides")
				{
					if(trees.Length==0)throw new InvalidDataException(preset+": no trees generated.");
					foreach(var tree in trees)
					{
						var c=FieldLoader.GetValue<CPos>("Location",tree.Value.ToDictionary()["Location"].Value);
						if(b.Resources[c].Type!=0 || b.Ramp[c]!=0)throw new InvalidDataException("Tree placed on resources or ramp.");
					}
				}
				else if(trees.Length!=0 || aZip.Contents.Any(n=>!Read(aZip,n).SequenceEqual(Read(bZip,n))))throw new InvalidDataException(preset+": unrelated map changed.");
				Console.WriteLine($"PASS {preset} {players}P: {trees.Length} decorative trees; terrain behavior, heights, ramps, resources and original actors preserved.");
			}
		}
		static byte[] Read(IReadOnlyPackage package,string name)
		{
			using var stream=package.GetStream(name);using var memory=new MemoryStream();stream.CopyTo(memory);return memory.ToArray();
		}
	}
}
