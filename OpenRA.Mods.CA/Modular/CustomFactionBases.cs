using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.Modular
{
	public sealed partial class CustomFactionDesign
	{
		static readonly Dictionary<string, (string Group, string Label, string WorldTrait, string ReferenceActor)> Bases =
			new Dictionary<string, (string, string, string, string)>
			{
				{ "eagle", ("gdi", "GDI / Eagle", "FactionCA@11", "mtnk") },
				{ "blackh", ("nod", "Nod / Black Hand", "FactionCA@13", "ltnk") },
				{ "chinatnk", ("china", "China / Tank-General", "FactionCA@22", "chbattle") },
				{ "england", ("allies", "Alliierte / England", "FactionCA@1", "challenger_tank") },
				{ "russia", ("soviet", "Sowjets / Russland", "FactionCA@5", "heavy_tank") },
				{ "traveler", ("scrin", "Scrin / Traveler-59", "FactionCA@18", "seek") }
			};

		public static string[] BaseFactions => Bases.Keys.ToArray();
		public static string BaseLabel(string id) => Bases[id].Label;
		static bool SupportedBase(string id) => id != null && Bases.ContainsKey(id);
		static string ComponentFaction(string id) => SupportedBase(id) ? Bases[id].Group : throw new InvalidDataException("Nicht unterstuetzte Basisfraktion.");

		Dictionary<string, string> DefaultParts(string baseFaction)
		{
			var p = new CustomFactionProfile { BaseFaction = baseFaction };
			SelectPart(p, "chassis", CompatibleOptions(p, "chassis").First());
			Calculate(p);
			return p.Parts;
		}

		// New independent roster: changing nation must never silently convert existing saved designs.
		public CustomFactionRoster NewRoster(string baseFaction, string libraryDirectory = null)
		{
			var r = new CustomFactionRoster
			{
				BaseFaction = baseFaction,
				Name = "Meine " + ComponentFaction(baseFaction).ToUpperInvariant()
			};
			var seed = r.Name;
			for (var suffix = 2; libraryDirectory != null && File.Exists(LibraryFileName(libraryDirectory, r.Name)); suffix++)
				r.Name = seed + " " + suffix;
			r.Designs[0].Parts = DefaultParts(baseFaction);
			r.Designs[0].Name = "Eigener Panzer";
			ValidateRoster(r);
			return r;
		}
	}
}
