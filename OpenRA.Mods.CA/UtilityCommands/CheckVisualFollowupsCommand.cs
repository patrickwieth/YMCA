using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits.Render;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckVisualFollowupsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-visual-followups";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		static string Canonical(MiniYamlNode n) => n.Key + ":" + n.Value.Value + "[" + string.Join("", n.Value.Nodes.Select(Canonical).OrderBy(x => x, StringComparer.Ordinal)) + "]";

		[Desc("Check fixed foundation hover, production-leader mode gates and actual C130 voxel/crate bindings.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actors = mod.DefaultRules.Actors;
			foreach (var name in new[] { "army.assignment.red", "army.assignment.blue", "army.assignment.black", "promotion.army_leader_blue", "promotion.army_leader_black" })
			{
				var b = actors[name].TraitInfo<BuildableInfo>();
				foreach (var faction in new[] { "scrin", "reaper", "soviet", "gdi", "nod" })
					foreach (var mode in new[] { "Tactical", "Strategic", "Operational", "" })
						if (b.IsAvailableInContext(new[] { mode }, faction) != (mode == "Operational"))
							throw new InvalidDataException(name + ": production-leader icon leaked outside Operational.");
				if (!b.Prerequisites.Contains("~mode.operational")) throw new InvalidDataException(name + ": missing hidden mode prerequisite.");
			}
			var hoverInfo = actors["grav"].TraitInfo<HoversAboveFoundationInfo>();
			var hover = new HoversAboveFoundation(hoverInfo);
			if (hoverInfo.InitialHeight.Length != 136 || hoverInfo.BobDistance.Length != -68 || hoverInfo.Ticks != 24)
				throw new InvalidDataException("Unexpected Gravity Stabilizer hover envelope.");
			var sprite = (Sprite)RuntimeHelpers.GetUninitializedObject(typeof(Sprite));
			var body = new SpriteRenderable(sprite, WPos.Zero, WVec.Zero, 0, null, 1f, 1f, float3.Ones, TintModifiers.None, false);
			var bib = new SpriteRenderable(sprite, WPos.Zero, WVec.Zero, 0, null, 1f, 1f, float3.Ones, TintModifiers.None, true);
			foreach (var z in new[] { 0, 68, 136, 204 })
			{
				typeof(Hovers).GetProperty(nameof(Hovers.WorldVisualOffset)).SetValue(hover, new WVec(0, 0, z));
				var rendered = ((IRenderModifier)hover).ModifyRender(null, null, new IRenderable[] { body, bib }).ToArray();
				if (rendered[0].Pos != WPos.Zero + new WVec(0, 0, z) || !ReferenceEquals(rendered[1], bib))
					throw new InvalidDataException("Hover must move body only, never the decorative foundation.");
			}

			var rules = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Rules, null).ToDictionary(n => n.Key.ToLowerInvariant());
			bool Cosmetic(MiniYamlNode n) => new[] { "RenderSprites", "RenderVoxels", "WithFacingSpriteBody", "WithVoxelBody", "WithShadow", "BodyOrientation", "GrantCondition@Model", "Contrail@", "SpawnActorOnDeath" }.Any(p => n.Key.StartsWith(p, StringComparison.Ordinal));
			foreach (var (source, replacement) in new[] { ("c17", "c17.crate"), ("c17.no_color", "c17.crate.no_color"), ("c17.husk", "c17.crate.husk"), ("c17.husk.emp", "c17.crate.husk.emp") })
			{
				_ = actors[replacement].TraitsInConstructOrder().ToArray();
				if (!rules[source].Value.Nodes.Where(n => !Cosmetic(n)).Select(Canonical).OrderBy(x => x).SequenceEqual(rules[replacement].Value.Nodes.Where(n => !Cosmetic(n)).Select(Canonical).OrderBy(x => x)))
					throw new InvalidDataException(replacement + ": nonvisual delivery/flight behavior changed.");
				var voxel = actors[replacement].TraitInfo<RenderVoxelsInfo>();
				if (voxel.Image != "c130" || voxel.Scale != 1.3f || actors[replacement].HasTraitInfo<WithFacingSpriteBodyInfo>())
					throw new InvalidDataException(replacement + ": voxel-only binding missing.");
			}
			if (actors["world"].TraitInfo<CrateSpawnerInfo>().DeliveryAircraft != "c17.crate") throw new InvalidDataException("Random crates still use the old sprite plane.");
			using var vs = mod.DefaultFileSystem.Open("c130.vxl");
			using var hs = mod.DefaultFileSystem.Open("c130.hva");
			var vxl = new VxlReader(vs);
			var hva = new HvaReader(hs, "c130.hva");
			if (vxl.LimbCount == 0 || vxl.Limbs.Sum(l => (long)l.VoxelCount) == 0 || vxl.LimbCount != hva.LimbCount || hva.FrameCount == 0)
				throw new InvalidDataException("C130 voxel/HVA pair is empty or mismatched.");
			Console.WriteLine("Production leader entries restricted to Operational for all factions; body-only hover preserves bib; crate aircraft/husks use engine-loaded C130 VXL/HVA with unchanged flight/drop behavior.");
		}
	}
}
