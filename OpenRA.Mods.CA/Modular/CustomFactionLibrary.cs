using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.CA.Modular
{
	// Library paths are identities: changing the display name must not create another faction.
	public sealed class CustomFactionLibrary
	{
		public sealed record Entry(string Path, CustomFactionRoster Roster, string Error);
		readonly CustomFactionDesign compiler;
		readonly string directory;

		public CustomFactionLibrary(CustomFactionDesign compiler, string directory)
		{
			this.compiler = compiler;
			this.directory = Path.GetFullPath(directory);
		}

		string CheckedPath(string path)
		{
			var full = Path.GetFullPath(path);
			var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if (!string.Equals(Path.GetDirectoryName(full), directory, comparison) ||
				!Regex.IsMatch(Path.GetFileName(full), @"\Afaction-[a-fA-F0-9]+\.json\z"))
				throw new InvalidDataException("Not a faction library entry.");
			return full;
		}

		public Entry[] Entries()
		{
			if (!Directory.Exists(directory)) return Array.Empty<Entry>();
			var entries = new List<Entry>();
			foreach (var path in Directory.GetFiles(directory, "faction-*.json").OrderBy(p => p, StringComparer.Ordinal))
			{
				CheckedPath(path);
				try { entries.Add(new Entry(path, compiler.DeserializeRoster(File.ReadAllText(path)), null)); }
				catch (Exception e) { entries.Add(new Entry(path, null, e.Message)); }
			}
			return entries.OrderBy(e => e.Roster?.Name ?? Path.GetFileName(e.Path), StringComparer.OrdinalIgnoreCase).ToArray();
		}

		void UniqueName(CustomFactionRoster roster, string except = null)
		{
			compiler.ValidateRoster(roster);
			if (Entries().Any(e => e.Path != except && e.Roster != null &&
				string.Equals(e.Roster.Name.Trim(), roster.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
				throw new InvalidDataException("A faction with this name already exists.");
		}

		public Entry Create(string name, string baseFaction)
		{
			var roster = compiler.NewRoster(baseFaction);
			roster.Name = name.Trim();
			roster.Designs[0].Name = "Custom vehicle";
			UniqueName(roster);
			Directory.CreateDirectory(directory);
			var path = Path.Combine(directory, "faction-" + Guid.NewGuid().ToString("N") + ".json");
			compiler.SaveRoster(path, roster);
			return new Entry(path, roster, null);
		}

		public CustomFactionRoster Load(string path) => compiler.DeserializeRoster(File.ReadAllText(CheckedPath(path)));

		public void Update(string path, CustomFactionRoster roster)
		{
			path = CheckedPath(path);
			if (!File.Exists(path)) throw new FileNotFoundException("This faction no longer exists.", path);
			UniqueName(roster, path);
			compiler.SaveRoster(path, roster);
		}

		// Confirmation is the caller's responsibility. Archive instead of destroying user work.
		// Archived entries and .bak files never appear in Entries(). Frozen maps are untouched.
		public void Delete(string path)
		{
			path = CheckedPath(path);
			var archive = Path.Combine(directory, "Deleted");
			Directory.CreateDirectory(archive);
			File.Move(path, Path.Combine(archive, Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(path)));
		}

		// Only import pre-library installations once; deletion must not resurrect the active snapshot.
		public void ImportLegacyOnce(string profilePath)
		{
			if (Directory.Exists(directory)) return;
			if (File.Exists(profilePath))
			{
				var roster = compiler.DeserializeRoster(File.ReadAllText(profilePath));
				Directory.CreateDirectory(directory);
				compiler.SaveRoster(Path.Combine(directory, "faction-" + Guid.NewGuid().ToString("N") + ".json"), roster);
			}
			else Directory.CreateDirectory(directory);
		}
	}
}
