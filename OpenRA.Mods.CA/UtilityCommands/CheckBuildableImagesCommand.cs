using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class CheckBuildableImagesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-buildable-images";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Check resolved production icon image/sequence references, including faction images, without a GPU.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var images = MiniYaml.Load(utility.ModData.DefaultFileSystem, utility.ModData.Manifest.Sequences, null)
				.ToDictionary(n => n.Key, n => n.Value.Nodes.Select(s => s.Key).ToHashSet());
			var queues = utility.ModData.DefaultRules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>())
				.Select(q => q.Type).ToHashSet();
			var errors = new List<string>();
			var count = 0;
			foreach (var actor in utility.ModData.DefaultRules.Actors.Values.Where(a => !a.Name.StartsWith("^", StringComparison.Ordinal)))
			{
				// Internal support-power actors and prerequisite-only pseudo actors
				// do not enter production palettes. Explicitly disabled proxies do not either.
				var buildables = actor.TraitInfos<BuildableInfo>().Where(b => b.Queue.Overlaps(queues) &&
					!b.Prerequisites.Contains("~disabled")).ToArray();
				if (buildables.Length == 0) continue;
				var render = actor.TraitInfoOrDefault<RenderSpritesInfo>();
				if (render == null) { errors.Add(actor.Name + ": missing RenderSprites for production icon"); continue; }
				var names = new[] { render.GetImage(actor, null) }.Concat(render.FactionImages?.Values.Select(s => s.ToLowerInvariant()) ?? Enumerable.Empty<string>()).Distinct();
				foreach (var image in names)
					foreach (var buildable in buildables)
					{
						count++;
						if (!images.TryGetValue(image, out var sequences) || !sequences.Contains(buildable.Icon))
							errors.Add($"{actor.Name}: missing {image}/{buildable.Icon}");
					}
			}
			foreach (var error in errors) Console.WriteLine(error);
			if (errors.Count != 0) throw new InvalidDataException($"{errors.Count} invalid production icon references out of {count}.");
			Console.WriteLine($"BUILDABLE IMAGES PASS {count} resolved image/icon references, including faction overrides. Asset decoding still requires a client test.");
		}
	}
}
