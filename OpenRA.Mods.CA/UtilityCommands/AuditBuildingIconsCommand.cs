using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class AuditBuildingIconsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--audit-building-icons";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT.json", "Snapshot resolved building icon bindings, gameplay and non-icon sprite sequences.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var sequences = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).ToDictionary(n => n.Key);
			var rules = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Rules, null).ToDictionary(n => n.Key.ToLowerInvariant());
			string Stable(MiniYamlNode n, string parent = "")
			{
				if ((parent == "RenderSprites" && (n.Key == "Image" || n.Key == "FactionImages")) ||
					(parent.StartsWith("Buildable", StringComparison.Ordinal) && n.Key == "Icon")) return "";
				return n.Key + ":" + n.Value.Value + "[" + string.Join("", n.Value.Nodes.Select(c => Stable(c, n.Key)).OrderBy(s => s, StringComparer.Ordinal)) + "]";
			}
			string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
			var records = new List<object>();
			foreach (var entry in mod.DefaultRules.Actors.OrderBy(a => a.Key))
			{
				var actor = entry.Value;
				var build = actor.TraitInfos<BuildableInfo>().FirstOrDefault();
				if (entry.Key.StartsWith("^", StringComparison.Ordinal) || build == null || !actor.HasTraitInfo<BuildingInfo>() || !actor.HasTraitInfo<RenderSpritesInfo>()) continue;
				var sprites = actor.TraitInfo<RenderSpritesInfo>();
				var views = new List<object>();
				foreach (var faction in new[] { "england", "france", "germany", "russia", "nod", "eagle", "chinatnk", "reaper" })
				{
					var image = sprites.GetImage(actor, faction);
					if (!sequences.TryGetValue(image, out var seq)) continue;
					var icon = seq.Value.Nodes.FirstOrDefault(n => n.Key == build.Icon);
					string Field(string name) => icon?.Value.Nodes.FirstOrDefault(n => n.Key == name)?.Value.Value;
					var body = seq.Value.Nodes.Where(n => n.Key != "icon" && !n.Key.EndsWith("-icon", StringComparison.Ordinal));
					views.Add(new { Faction = faction, Image = image, Icon = build.Icon, File = Field("Filename"), Font = Field("ButtonLabelFont"), Label = Field("ButtonLabel"), BodyHash = Hash(string.Join("", body.Select(n => Stable(n)).OrderBy(s => s, StringComparer.Ordinal))) });
				}
				records.Add(new { Actor = entry.Key, RuleKey = rules[entry.Key].Key,
					Name = actor.TraitInfos<TooltipInfo>().FirstOrDefault()?.Name,
					Queue = build.Queue,
					MakeAnimation = actor.HasTraitInfo<WithMakeAnimationInfo>(),
					Primitives = actor.TraitInfos<IRenderActorPreviewInfo>().Select(t => t.GetType().Name).ToArray(),
					Prerequisites = build.Prerequisites, GameplayHash = Hash(Stable(rules[entry.Key])), Views = views });
			}
			File.WriteAllText(args[1], JsonConvert.SerializeObject(records, Formatting.Indented));
			File.WriteAllText(args[1] + ".sequences.json", JsonConvert.SerializeObject(
				sequences.ToDictionary(s => s.Key, s => s.Value.Value.Nodes.WriteToString()), Formatting.Indented));
			Console.WriteLine("Building icon snapshot written: " + args[1]);
		}
	}
}
