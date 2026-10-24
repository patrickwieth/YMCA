using System;
using System.IO;
using OpenRA.Network;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class SkirmishDebugPreferenceTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--skirmish-debug-preference-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Check local debug preference defaults, restart, map changes, locked maps and legacy migration.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var directory = Path.Combine(Path.GetTempPath(), "ymca-debug-preference-" + Guid.NewGuid());
			Directory.CreateDirectory(directory);
			try
			{
				var path = Path.Combine(directory, "preference.txt");
				Session.LobbyOptionState Option(bool value = false, bool locked = false) => new() { Value = value.ToString(), PreferredValue = value.ToString(), IsLocked = locked };
				void Check(bool condition, string message) { if (!condition) throw new InvalidDataException(message); Console.WriteLine("DEBUG PREFERENCE PASS " + message); }
				var preference = new SkirmishDebugPreference(path, null); var option = Option();
				preference.Restore("first", option);
				Check(!option.IsEnabled && !File.Exists(path), "fresh install stays off");
				option.Value = "True"; preference.Observe("first", option);
				preference = new SkirmishDebugPreference(path, null); option = Option(); preference.Restore("generated-second", option);
				Check(option.IsEnabled, "enabled choice survives restart and generated map replacement");
				option = Option(false, true); preference.Observe("locked", option);
				preference.Observe("locked", option);
				Check(!option.IsEnabled && File.ReadAllText(path) == "True", "locked-off map neither enables cheats nor overwrites preference");
				option = Option(); preference.Observe("unlocked", option);
				Check(option.IsEnabled, "returning to unlocked map restores choice");
				option.Value = "False"; preference.Observe("unlocked", option);
				preference = new SkirmishDebugPreference(path, "True"); option = Option(true); preference.Restore("third", option);
				Check(!option.IsEnabled, "disabled choice survives restart and overrides stale legacy state");
				option = Option(true, true); preference.Observe("locked-on", option); preference.Observe("locked-on", option);
				Check(option.IsEnabled && File.ReadAllText(path) == "False", "locked-on map does not overwrite disabled preference");
				preference.Observe("missing-option", null); option = Option(true); preference.Observe("restored-option", option);
				Check(!option.IsEnabled, "maps without developer option do not erase preference");
				File.Delete(path); preference = new SkirmishDebugPreference(path, "True"); option = Option(); preference.Restore("legacy", option);
				Check(option.IsEnabled && File.ReadAllText(path) == "True", "old skirmish choice migrates automatically");
				File.WriteAllText(path, "invalid"); preference = new SkirmishDebugPreference(path, null); option = Option(); preference.Restore("invalid", option);
				Check(!option.IsEnabled, "malformed preference does not enable cheats");
			}
			finally { Directory.Delete(directory, true); }
		}
	}
}
