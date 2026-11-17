using System;
using System.IO;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Bounded, versioned generator inputs only: never accept uploaded files, paths or map rules.
	static class GeneratedMapRecipe
	{
		public const string Capability = "ymca-map-generator";
		public const string Selection = "ymca-generated-map";
		public const string Version = "1";
		public const string Command = "generate_map ";

		public static void Validate(MapGenerationOptions o)
		{
			if (o == null || o.Width > 256 || o.Height > 256 || o.Teams != 2 ||
				!Enum.IsDefined(typeof(GeneratedMapMode), o.Mode) || !Enum.IsDefined(typeof(MapGenerationPreset), o.Preset) ||
				!Enum.IsDefined(typeof(ResourceFieldLayout), o.Resources) || !Enum.IsDefined(typeof(TechBuildingDensity), o.TechBuildings))
				throw new ArgumentException("Unsupported generator settings (maximum size 256x256, two teams).");
			var c = o.Custom;
			if (c != null && (!Enum.IsDefined(typeof(CustomTopology), c.Topology) || !Enum.IsDefined(typeof(StrongholdSubtype), c.Strongholds) ||
				!Enum.IsDefined(typeof(PlainsSubtype), c.Plains) || !Enum.IsDefined(typeof(IslandSubtype), c.Islands)))
				throw new ArgumentException("Unsupported custom topology.");
			o.Validate();
		}

		public static string Encode(MapGenerationOptions o)
		{
			Validate(o);
			using var stream = new MemoryStream();
			using var w = new BinaryWriter(stream);
			w.Write((byte)1); w.Write((byte)o.Mode); w.Write((byte)o.Preset);
			w.Write((ushort)o.Width); w.Write((ushort)o.Height); w.Write((byte)o.Players); w.Write((byte)o.Teams);
			w.Write(o.Seed); w.Write((byte)o.ExpansionsPerPlayer); w.Write((byte)o.Resources); w.Write(o.FiniteResources);
			w.Write((byte)o.TechBuildings); w.Write(o.MountainLines); w.Write(o.WaterPatches); w.Write(o.Custom != null);
			if (o.Custom != null)
			{
				var c = o.Custom;
				w.Write((byte)c.Topology); w.Write((byte)c.Strongholds); w.Write((byte)c.Plains); w.Write((byte)c.Islands);
				w.Write(c.ConnectedStartAreas); w.Write(c.BattleInTheCenter); w.Write(c.CenterBuildable); w.Write(c.Connections);
			}

			return Convert.ToBase64String(stream.ToArray());
		}

		public static MapGenerationOptions Decode(string text)
		{
			if (string.IsNullOrEmpty(text) || text.Length > 64)
				throw new ArgumentException("Invalid generator recipe length.");
			using var stream = new MemoryStream(Convert.FromBase64String(text));
			using var r = new BinaryReader(stream);
			if (r.ReadByte() != 1) throw new ArgumentException("Unsupported generator protocol.");
			bool Boolean()
			{
				var b = r.ReadByte();
				if (b > 1) throw new ArgumentException("Invalid generator boolean.");
				return b == 1;
			}

			var o = new MapGenerationOptions
			{
				Mode = (GeneratedMapMode)r.ReadByte(), Preset = (MapGenerationPreset)r.ReadByte(),
				Width = r.ReadUInt16(), Height = r.ReadUInt16(), Players = r.ReadByte(), Teams = r.ReadByte(),
				Seed = r.ReadInt32(), ExpansionsPerPlayer = r.ReadByte(), Resources = (ResourceFieldLayout)r.ReadByte(),
				FiniteResources = Boolean(), TechBuildings = (TechBuildingDensity)r.ReadByte(),
				MountainLines = Boolean(), WaterPatches = Boolean(),
			};
			if (Boolean())
				o.Custom = new CustomMapSettings
				{
					Topology = (CustomTopology)r.ReadByte(), Strongholds = (StrongholdSubtype)r.ReadByte(),
					Plains = (PlainsSubtype)r.ReadByte(), Islands = (IslandSubtype)r.ReadByte(),
					ConnectedStartAreas = Boolean(), BattleInTheCenter = Boolean(), CenterBuildable = Boolean(), Connections = Boolean(),
				};
			if (stream.Position != stream.Length) throw new ArgumentException("Unexpected generator recipe data.");
			Validate(o);
			return o;
		}
	}
}
