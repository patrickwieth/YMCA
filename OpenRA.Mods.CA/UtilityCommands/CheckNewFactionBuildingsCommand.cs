using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckNewFactionBuildingsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-new-faction-buildings";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || (args.Length == 2 && args[1] == "--inspect-smoke");
		[Desc("Validate supplied China/Scrin buildings, variants, repair states and registered effects (workspace tool).")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			if (args.Length == 2)
			{
				using var input = mod.DefaultFileSystem.Open("plantsmoke.shp");
				var frames = FrameLoader.GetFrames(input, mod.SpriteLoaders, "plantsmoke.shp", out _);
				var points = frames.Take(15).SelectMany(f => Enumerable.Range(0, f.Data?.Length ?? 0)
					.Where(i => f.Data[i] != 0).Select(i => new[] { i % f.Size.Width + .5 - f.Size.Width / 2.0 + f.Offset.X,
						i / f.Size.Width + .5 - f.Size.Height / 2.0 + f.Offset.Y })).ToArray();
				var bottom = points.Max(p => p[1]);
				var center = points.Where(p => p[1] >= bottom - 1).Average(p => p[0]);
				File.WriteAllText("art-source/new-faction-buildings/smoke-registration.json", JsonConvert.SerializeObject(new {
					frame_count = frames.Length, emission_pixel = new[] { center, bottom },
					sequence_offset = new[] { (int)Math.Round(-center), (int)Math.Round(-bottom) },
					bounds = new[] { points.Min(p => p[0]), points.Min(p => p[1]), points.Max(p => p[0]), bottom }
				}, Formatting.Indented));
				return;
			}
			var buildableBarriers = mod.DefaultRules.Actors.Where(a => a.Value.HasTraitInfo<BuildableInfo>() &&
				a.Value.HasTraitInfo<LineBuildInfo>()).Select(a => a.Key).ToArray();
			if (buildableBarriers.Length != 0)
				throw new InvalidDataException("Buildable walls/fences remain: " + string.Join(",", buildableBarriers));
			Console.WriteLine("All factions: no buildable wall/fence/laser-fence actors; map actors retained.");
			var binding = JObject.Parse(File.ReadAllText("art-source/new-faction-buildings/binding.json"));
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			foreach (var entry in binding.Properties())
			{
				var b = entry.Value;
				var actor = mod.DefaultRules.Actors[entry.Name.ToLowerInvariant()];
				_ = actor.TraitsInConstructOrder().ToArray();
				var image = (string)b["image"];
				if (actor.TraitInfo<RenderSpritesInfo>().Image != image)
					throw new InvalidDataException(entry.Name + ": image override lost.");
				var node = nodes.Single(n => n.Key == image);
				var states = b["states"].Values<string>().ToArray();
				var special = b["extra_sequences"].Values<string>().ToArray();
				var selectedNames = states.Concat(special).Concat(new[] { "Defaults", "construction", "icon", "iconadv" }).ToArray();
				var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => selectedNames.Contains(n.Key)).ToArray()));
				using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
				cache.LoadReservations(mod);
				foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
				foreach (var state in states)
				{
					var filename = node.Value.Nodes.Single(n => n.Key == state).Value.Nodes.Single(n => n.Key == "Filename").Value.Value;
					var sequence = sequences[state];
					if (filename != (string)b["filename"] || sequence.Scale != (float)b["scale"] || sequence.Length != 1 ||
						sequence.GetSprite(0).Offset.X != (int)b["offset"][0] ||
						sequence.GetSprite(0).Offset.Y != (int)b["offset"][1] - (int)b["size"] / 4)
						throw new InvalidDataException(entry.Name + "/" + state + ": direct file/placement mismatch.");
				}
				using (var input = mod.DefaultFileSystem.Open((string)b["filename"]))
				using (var raw = mod.DefaultFileSystem.Open((string)b["filename"]))
				{
					var png = new Png(raw);
					var frames = FrameLoader.GetFrames(input, mod.SpriteLoaders, (string)b["filename"], out _);
					if (frames.Length != 1 || frames[0].Type == SpriteFrameType.Indexed8 || frames[0].Type != png.Type || !frames[0].Data.SequenceEqual(png.Data))
						throw new InvalidDataException(entry.Name + ": source RGBA pixels changed.");
				}
				var bibName = (string)b["bib"];
				if (bibName != null)
				{
					if (actor.TraitInfos<WithIdleOverlayInfo>().Count(o => o.Image == bibName) != 1)
						throw new InvalidDataException(entry.Name + ": Soviet bib missing/duplicated.");
					var bibNode = nodes.Single(n => n.Key == bibName);
					var bibSequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, bibNode);
					cache.LoadReservations(mod);
					bibSequences["idle"].ResolveSprites(cache);
					if (bibSequences["idle"].Scale != 1)
						throw new InvalidDataException(entry.Name + ": Soviet bib must retain native scale.");
				}
				if (entry.Name == "SREP" && (!actor.HasTraitInfo<RepairsUnitsInfo>() || !actor.HasTraitInfo<WithResupplyAnimationInfo>()))
					throw new InvalidDataException("Repair functionality missing.");
				if (b["radar_source"] != null)
				{
					var radar = node.Value.Nodes.Single(n => n.Key == "radar");
					if (radar.Value.Nodes.Single(n => n.Key == "Filename").Value.Value != (string)b["radar_source"] ||
						Math.Abs((sequences["radar"].GetSprite(0).Offset.X + 11) * (double)b["scale"] - 1) > .001 ||
						Math.Abs((sequences["radar"].GetSprite(0).Offset.Y + 137 - (int)b["offset"][1]) * (double)b["scale"] + 1) > .001 || sequences["radar"].Scale != (float)b["scale"])
						throw new InvalidDataException("Command radar upgrade alignment failed.");
				}
				if (b["smoke_pixels"] != null)
				{
					var overlays = actor.TraitInfos<WithIdleOverlayInfo>().Where(o => o.Sequence == "idle-anim2").ToArray();
					if (overlays.Length != b["smoke_pixels"].Count() || sequences["idle-anim2"].Length != 15 || sequences["idle-anim2"].Scale != 2 || overlays.Any(o => o.Palette != "effect"))
						throw new InvalidDataException("Reactor steam overlay regression.");
					for (var i = 0; i < overlays.Length; i++)
					{
						var p = actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(overlays[i].Offset);
						var target = b["smoke_pixels"][i];
						if (Math.Abs(p.X * 128.0 / 1448 - ((double)target[0] - 128) * (double)b["scale"]) > .1 ||
							Math.Abs((p.Y - p.Z) * 64.0 / 1448 - ((double)target[1] - 192 + (int)b["offset"][1]) * (double)b["scale"]) > .1)
							throw new InvalidDataException("Reactor steam mouth registration failed.");
					}
				}
				if (entry.Name.StartsWith("SCOL", StringComparison.Ordinal))
				{
					var body = actor.TraitInfo<BodyOrientationInfo>();
					var arm = actor.TraitInfo<ArmamentInfo>();
					if (arm.Weapon != "StormColumnZap" || arm.LocalOffset.Length != 5 || actor.TraitInfo<BuildingInfo>().Dimensions != new CVec(1, 1))
						throw new InvalidDataException("Storm Column weapon/1x1 regression.");
					for (var i = 0; i < 5; i++)
					{
						var p = body.LocalToWorld(arm.LocalOffset[i]);
						var target = b["emitter_pixels"][i];
						var x = ((double)target[0] - 128) * (double)b["scale"];
						var y = ((double)target[1] - 192 + (int)b["offset"][1]) * (double)b["scale"];
						if (Math.Abs(p.X * 128.0 / 1448 - x) > .1 || Math.Abs((p.Y - p.Z) * 64.0 / 1448 - y) > .1)
							throw new InvalidDataException("Storm Column emitter registration failed.");
					}
				}
				if (b["support_pixel"] != null)
				{
					var arms = actor.TraitInfos<ArmamentInfo>().ToArray();
					if (actor.TraitInfo<BuildingInfo>().Dimensions != new CVec(1, 1) ||
						arms.Length != (entry.Name.EndsWith(".pdl", StringComparison.Ordinal) ? 2 : 1))
						throw new InvalidDataException("Speaker Tower 1x1/PDL variant regression.");
					foreach (var arm in arms)
					{
						var p = actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(arm.LocalOffset.Single());
						var target = b["support_pixel"];
						if (Math.Abs(p.X * 128.0 / 1448 - ((double)target[0] - (int)b["size"] / 2.0) * (double)b["scale"]) > .1 ||
							Math.Abs((p.Y - p.Z) * 64.0 / 1448 - ((double)target[1] - (int)b["size"] * .75 + (int)b["offset"][1]) * (double)b["scale"]) > .1)
							throw new InvalidDataException("Speaker Tower repair/PDL origin mismatch.");
					}
				}
				if (entry.Name == "chsilo")
				{
					var launch = actor.TraitInfo<NukePowerInfo>();
					var p = actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(launch.SpawnOffset);
					var target = b["launch_pixel"];
					if (Math.Abs(p.X * 128.0 / 1448 - ((double)target[0] - 128) * (double)b["scale"]) > .1 ||
						Math.Abs((p.Y - p.Z) * 64.0 / 1448 - ((double)target[1] - 192 + (int)b["offset"][1]) * (double)b["scale"]) > .1)
						throw new InvalidDataException("Chinese silo launch mouth registration failed.");
				}
				if (entry.Name == "CHSYRD" &&
					(!actor.HasTraitInfo<ProductionInfo>() || !actor.HasTraitInfo<RepairsUnitsInfo>() ||
					actor.TraitInfos<ExitInfo>().Count() != 4 || !actor.TraitInfo<BuildingInfo>().TerrainTypes.Contains("Water") ||
					mod.DefaultRules.Actors["spen"].TraitInfo<RenderSpritesInfo>().Image != "spen.soviet"))
					throw new InvalidDataException("Chinese shipyard water/production/exits or Soviet image regression.");
				if (entry.Name == "RIFT" && (sequences["active-overlay"].Length != 13 || sequences["active-overlay"].Scale != (float)b["scale"]))
					throw new InvalidDataException("Rift pulse mismatch.");
				Console.WriteLine(entry.Name + " -> " + (string)b["filename"] + "; source RGBA, states, extras, UI/construction resolved.");
			}
			Console.WriteLine("New faction buildings checked. Live GPU/repair/docking review remains pending.");
		}
	}
}
