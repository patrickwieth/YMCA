namespace OpenRA.Mods.CA.Modular
{
	// A test session must neither restore nor overwrite the ordinary skirmish setup.
	// Latch this for the server lifetime, even if the user later changes maps in its lobby.
	public sealed class CustomFactionLobbySession
	{
		bool? useSavedSettings;

		public bool UseSavedSettings(bool isSkirmish, bool hasFrozenDesign)
		{
			useSavedSettings ??= !(isSkirmish && hasFrozenDesign);
			return useSavedSettings.Value;
		}
	}
}
