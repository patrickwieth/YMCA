namespace OpenRA.Mods.CA.MapGeneration
{
	enum CustomTopology { Strongholds, OpenPlains, Islands }
	enum StrongholdSubtype { River, Hills }
	enum PlainsSubtype { Flat, Rocky, Ponds }
	enum IslandSubtype { SmallIslands, BigIslands, MixedSizes }

	// Independent UI choices; preset identity remains stable for existing maps/CLI.
	sealed class CustomMapSettings
	{
		public CustomTopology Topology = CustomTopology.Strongholds;
		public StrongholdSubtype Strongholds = StrongholdSubtype.Hills;
		public PlainsSubtype Plains = PlainsSubtype.Rocky;
		public IslandSubtype Islands = IslandSubtype.SmallIslands;
		public bool ConnectedStartAreas = true;
		public bool BattleInTheCenter = true;
		bool? centerBuildable;
		public bool CenterBuildable { get => centerBuildable ?? Topology != CustomTopology.Strongholds; set => centerBuildable = value; }
		public void ResetCenterDefault() => centerBuildable = null;
		public bool Connections;
		public bool HasCenter => Topology == CustomTopology.Strongholds && BattleInTheCenter ||
			Topology == CustomTopology.Islands && Islands == IslandSubtype.MixedSizes;
		public string Description => $"Custom {Topology}/{(Topology == CustomTopology.Strongholds ? Strongholds.ToString() : Topology == CustomTopology.OpenPlains ? Plains.ToString() : Islands.ToString())}" +
			(Topology == CustomTopology.Strongholds ? $" connected={ConnectedStartAreas} center={BattleInTheCenter}" :
			Topology == CustomTopology.Islands ? $" connections={Connections}" : "") + (HasCenter ? $" buildable={CenterBuildable}" : "");
		public bool River => Topology == CustomTopology.Strongholds && Strongholds == StrongholdSubtype.River;
		public CustomMapSettings Copy() => (CustomMapSettings)MemberwiseClone();
		public MapGenerationPreset BasePreset => Topology switch
		{
			CustomTopology.OpenPlains => Plains == PlainsSubtype.Ponds ? MapGenerationPreset.WaterBasins : MapGenerationPreset.Arabia,
			CustomTopology.Islands => Islands switch
			{
				IslandSubtype.SmallIslands => MapGenerationPreset.Archipelago,
				IslandSubtype.BigIslands => MapGenerationPreset.Continents,
				_ => MapGenerationPreset.Migration,
			},
			_ => ConnectedStartAreas ? (River ? MapGenerationPreset.TwoSides : MapGenerationPreset.HillFortress) :
				BattleInTheCenter ? MapGenerationPreset.BattleNexus : MapGenerationPreset.MountainValleys,
		};
		public void ApplyAutomaticSize(MapGenerationOptions options)
		{
			if (!River || BattleInTheCenter || ConnectedStartAreas) return;
			var columns = (int)System.Math.Ceiling(System.Math.Sqrt(options.Players + 2));
			var rows = (int)System.Math.Ceiling((options.Players + 2.0) / columns);
			options.Width = System.Math.Max(options.Width, columns * 36);
			options.Height = System.Math.Max(options.Height, rows * 36);
		}

		public bool Compatible(GeneratedMapMode mode) => mode != GeneratedMapMode.Strategic ||
			Topology != CustomTopology.Islands && BasePreset != MapGenerationPreset.BattleNexus;
	}
}
