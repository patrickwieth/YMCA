using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace OpenRA.Mods.CA.Modular
{
	// Presentation only: retain the native faction ID for flags, prerequisites and gameplay.
	public sealed record CustomFactionLobbyIdentity(string Name, string BaseFaction)
	{
		public string Side => "Custom";
		public bool AppliesTo(string faction) => string.Equals(BaseFaction, faction, StringComparison.OrdinalIgnoreCase);
	}

	public sealed partial class CustomFactionDesign
	{
		public static CustomFactionLobbyIdentity ReadLobbyIdentity(string frozenJson)
		{
			var root = JObject.Parse(frozenJson);
			var schema = root.Value<int?>("Schema");
			if ((schema != 1 && schema != 2) || root["Name"]?.Type != JTokenType.String ||
				root["BaseFaction"]?.Type != JTokenType.String ||
				(schema == 1 ? root["Parts"] is not JObject : root["Designs"] is not JArray))
				throw new InvalidDataException("Invalid frozen faction metadata.");
			var name = root.Value<string>("Name");
			var faction = root.Value<string>("BaseFaction");
			if (!ValidName(name) || string.IsNullOrWhiteSpace(name) || !SupportedBase(faction))
				throw new InvalidDataException("Invalid frozen faction identity.");
			return new CustomFactionLobbyIdentity(name, faction);
		}
	}
}
