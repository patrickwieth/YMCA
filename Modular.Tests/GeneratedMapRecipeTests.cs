using NUnit.Framework;
using OpenRA.Mods.CA.MapGeneration;

namespace Modular.Tests
{
	[TestFixture]
	public class GeneratedMapRecipeTests
	{
		[Test]
		public void RoundTripsEveryCompatiblePresetAndMode()
		{
			foreach (var mode in Enum.GetValues<GeneratedMapMode>())
				foreach (var preset in Enum.GetValues<MapGenerationPreset>())
				{
					var options = new MapGenerationOptions { Mode = mode, Preset = preset, Players = 16, Width = 224, Height = 224,
						Seed = int.MinValue, Resources = ResourceFieldLayout.Patches, FiniteResources = true, ExpansionsPerPlayer = 2,
						TechBuildings = TechBuildingDensity.Dense, MountainLines = true, WaterPatches = true };
					try { options.Validate(); } catch (ArgumentException) { continue; }
					var encoded = GeneratedMapRecipe.Encode(options);
					var decoded = GeneratedMapRecipe.Decode(encoded);
					Assert.That(GeneratedMapRecipe.Encode(decoded), Is.EqualTo(encoded));
					Assert.That(decoded.Mode, Is.EqualTo(mode));
					Assert.That(decoded.Preset, Is.EqualTo(preset));
					Assert.That(decoded.Seed, Is.EqualTo(int.MinValue));
					Assert.That(decoded.Width, Is.EqualTo(224));
					Assert.That(decoded.FiniteResources && decoded.MountainLines && decoded.WaterPatches, Is.True);
				}
		}

		[Test]
		public void RoundTripsAllCustomSettings()
		{
			foreach (var topology in Enum.GetValues<CustomTopology>())
				foreach (var subtype in Enum.GetValues<IslandSubtype>())
					foreach (var flags in Enumerable.Range(0, 16))
					{
						var custom = new CustomMapSettings { Topology = topology, Islands = subtype, Strongholds = StrongholdSubtype.River,
							Plains = PlainsSubtype.Ponds, ConnectedStartAreas = (flags & 1) != 0, BattleInTheCenter = (flags & 2) != 0,
							CenterBuildable = (flags & 4) != 0, Connections = (flags & 8) != 0 };
						var o = new MapGenerationOptions { Custom = custom, Preset = custom.BasePreset };
						var encoded = GeneratedMapRecipe.Encode(o);
						var decoded = GeneratedMapRecipe.Decode(encoded);
						Assert.That(GeneratedMapRecipe.Encode(decoded), Is.EqualTo(encoded));
						Assert.That(decoded.Custom.Description, Is.EqualTo(custom.Description));
					}
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("not a recipe")]
		[TestCase("AA==")]
		[TestCase("AQ==")]
		public void RejectsInvalidRecipes(string recipe) => Assert.Catch(() => GeneratedMapRecipe.Decode(recipe));

		[Test]
		public void RejectsOversizeInvalidEnumsAndExtraBytes()
		{
			Assert.Catch(() => GeneratedMapRecipe.Encode(new MapGenerationOptions { Width = 257 }));
			Assert.Catch(() => GeneratedMapRecipe.Encode(new MapGenerationOptions { Height = 63 }));
			Assert.Catch(() => GeneratedMapRecipe.Encode(new MapGenerationOptions { Teams = 3 }));
			Assert.Catch(() => GeneratedMapRecipe.Encode(new MapGenerationOptions { Mode = (GeneratedMapMode)255 }));
			Assert.Catch(() => GeneratedMapRecipe.Encode(new MapGenerationOptions { Resources = (ResourceFieldLayout)255 }));
			Assert.Catch(() => GeneratedMapRecipe.Encode(new MapGenerationOptions { Custom = new CustomMapSettings { Topology = (CustomTopology)255 } }));
			var bytes = Convert.FromBase64String(GeneratedMapRecipe.Encode(new MapGenerationOptions()));
			Assert.Catch(() => GeneratedMapRecipe.Decode(Convert.ToBase64String(bytes.Concat(new byte[] { 0 }).ToArray())));
			bytes[15] = 2;
			Assert.Catch(() => GeneratedMapRecipe.Decode(Convert.ToBase64String(bytes)));
		}
	}
}
