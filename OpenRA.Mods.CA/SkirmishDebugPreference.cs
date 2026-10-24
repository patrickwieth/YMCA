using System;
using System.IO;
using OpenRA.Network;

namespace OpenRA.Mods.CA
{
	// Per-installation preference, separate from map-specific slots/options.
	// Only the local Skirmish server creates this; network lobbies never use it.
	sealed class SkirmishDebugPreference
	{
		readonly string path;
		bool? enabled;
		string map;

		public SkirmishDebugPreference(string path, string legacyValue)
		{
			this.path = path;
			try
			{
				if (File.Exists(path))
				{
					if (bool.TryParse(File.ReadAllText(path).Trim(), out var saved)) enabled = saved;
				}
				else if (bool.TryParse(legacyValue, out var legacy))
				{
					enabled = legacy;
					Save();
				}
			}
			catch (IOException e) { Log.Write("debug", "Cannot read skirmish debug preference: " + e.Message); }
			catch (UnauthorizedAccessException e) { Log.Write("debug", "Cannot read skirmish debug preference: " + e.Message); }
		}

		public bool Restore(string mapUid, Session.LobbyOptionState option)
		{
			map = mapUid;
			if (!enabled.HasValue || option == null) return false;
			// Locked maps keep their effective value, but do not erase the user's
			// choice when the lobby later switches back to an unlocked map.
			option.PreferredValue = enabled.Value.ToString();
			if (option.IsLocked || option.Value == option.PreferredValue) return false;
			option.Value = option.PreferredValue;
			return true;
		}

		public bool Observe(string mapUid, Session.LobbyOptionState option)
		{
			if (map != mapUid) return Restore(mapUid, option);
			if (option == null || option.IsLocked || !bool.TryParse(option.Value, out var value) || enabled == value) return false;
			enabled = value;
			Save();
			return false;
		}

		void Save()
		{
			try
			{
				File.WriteAllText(path + ".tmp", enabled.Value.ToString());
				File.Move(path + ".tmp", path, true);
			}
			catch (IOException e) { Log.Write("debug", "Cannot save skirmish debug preference: " + e.Message); }
			catch (UnauthorizedAccessException e) { Log.Write("debug", "Cannot save skirmish debug preference: " + e.Message); }
		}
	}
}
