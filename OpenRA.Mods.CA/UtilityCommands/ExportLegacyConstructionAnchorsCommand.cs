using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class ExportLegacyConstructionAnchorsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--export-legacy-construction-anchors";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT.json", "Export native legacy sprite ground anchors for shared construction registration.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var records = new List<object>();
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			foreach (var node in MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null))
			{
				var idle = node.Value.Nodes.FirstOrDefault(n => n.Key == "idle");
				var construction = node.Value.Nodes.FirstOrDefault(n => n.Key == "construction");
				if (idle == null || construction == null) continue;
				var filename = idle.Value.Nodes.FirstOrDefault(n => n.Key == "Filename")?.Value.Value;
				if (filename == null || filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
				var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => n.Key == "Defaults" || n.Key == "idle" || n.Key == "construction").ToArray()));
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
				cache.LoadReservations(mod);
				foreach (var s in sequences.Values) s.ResolveSprites(cache);
				var body = sequences["idle"]; var sprite = body.GetSprite(0);
				var target = new[] { sprite.Offset.X * body.Scale, (sprite.Offset.Y + sprite.Size.Y / 2 - sprite.Size.X / 4) * body.Scale };
				var make = sequences["construction"].GetSprite(0);
				// Subtract its current offset to recover the shared stage's unregistered anchor.
				var oldOffset = construction.Value.Nodes.FirstOrDefault(n => n.Key == "Offset")?.Value.Value ?? "0,0";
				var offset = FieldLoader.GetValue<float3>("Offset", oldOffset);
				var basis = new[] { make.Offset.X - offset.X, make.Offset.Y - offset.Y + make.Size.Y / 2 - make.Size.X / 4 };
				records.Add(new { image = node.Key, source = filename, target, basis, offset = new[] { target[0] - basis[0], target[1] - basis[1] }, method = "native legacy sprite" });
			}
			File.WriteAllText(args[1], JsonConvert.SerializeObject(records, Formatting.Indented));
			Console.WriteLine("Legacy construction anchors: " + records.Count);
		}
	}
}
