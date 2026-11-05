using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class TemporaryGeneratedMapTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--temporary-generated-map-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Test session map ownership, edited/copy preservation, missing files and locked-file cleanup.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var root = Path.Combine(Path.GetTempPath(), "ymca-temporary-map-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
			try
			{
				string Create(string name = null)
				{
					var path = Path.Combine(root, name ?? TemporaryGeneratedMapFiles.NewFilename()); File.WriteAllText(path, "original"); return path;
				}
				void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); Console.WriteLine("TEMPORARY MAP PASS " + text); }
				var files = new TemporaryGeneratedMapFiles(); var reports = new List<string>();
				var first = Create(); files.Register(first); var copy = Path.Combine(root, "saved-copy.oramap"); File.Copy(first, copy);
				var edited = Create(); files.Register(edited); File.WriteAllText(edited, "editor save"); files.Register(edited);
				var foreign = Create(); var otherSession = new TemporaryGeneratedMapFiles(); otherSession.Register(foreign);
				var ordinary = Create("hill-fortress-players-4-seed-42.oramap"); var rejected = false;
				try { files.Register(ordinary); } catch (ArgumentException) { rejected = true; }
				Check(rejected, "ordinary existing map cannot be registered");
				var missing = Create(); files.Register(missing); File.Delete(missing);
				files.Cleanup(reports.Add);
				Check(!File.Exists(first), "owned unchanged export removed");
				Check(File.ReadAllText(edited) == "editor save", "edited original preserved even after repeat registration");
				Check(File.Exists(copy) && File.Exists(ordinary), "saved copy and existing map preserved");
				Check(File.Exists(foreign), "other session retained");
				Check(!File.Exists(missing), "already missing map tolerated");
				otherSession.Cleanup(reports.Add); Check(!File.Exists(foreign), "other session cleans only its own map");
				files.Cleanup(reports.Add); Check(File.Exists(edited), "cleanup is idempotent");
				if (OperatingSystem.IsWindows())
				{
					var locked = Create(); files.Register(locked);
					using (var stream = File.Open(locked, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						files.Cleanup(reports.Add); Check(File.Exists(locked), "locked file cannot break shutdown");
					}
					files.Cleanup(reports.Add); Check(!File.Exists(locked), "cleanup can retry after lock release");
				}
				Check(reports.Count > 0, "preservation/failure diagnostics reported");
			}
			finally { Directory.Delete(root, true); }
		}
	}
}
