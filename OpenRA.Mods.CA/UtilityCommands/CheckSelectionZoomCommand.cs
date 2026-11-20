using System;
using System.IO;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSelectionZoomCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-selection-zoom";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check selection-frame zoom boundaries and isolated settings persistence.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var s = new GameSettings();
			void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
			Check(!s.ShowSelectionBoxes(.25f) && !s.ShowSelectionBoxes(.5f) && !s.ShowSelectionBoxes(.99f) && s.ShowSelectionBoxes(1), "Default/boundary");
			s.SelectionBoxMinimumZoom = .75f;
			Check(!s.ShowSelectionBoxes(.7f) && s.ShowSelectionBoxes(.75f), "Custom threshold");
			s.SelectionBoxMinimumZoom = 0;
			Check(s.ShowSelectionBoxes(.01f), "Always show");
			s.SelectionBoxMinimumZoom = float.NaN;
			Check(s.EffectiveSelectionBoxMinimumZoom == 1f, "Invalid threshold fallback");
			s.SelectionBoxMinimumZoom = 3;
			Check(s.EffectiveSelectionBoxMinimumZoom == 2, "Upper bound");
			var file = Path.Combine(Path.GetTempPath(), "ymca-selection-zoom-" + Guid.NewGuid().ToString("N") + ".yaml");
			try
			{
				var settings = new Settings(file, new Arguments(Array.Empty<string>()));
				settings.Game.SelectionBoxMinimumZoom = .75f;
				settings.Save();
				Check(new Settings(file, new Arguments(Array.Empty<string>())).Game.SelectionBoxMinimumZoom == .75f, "Persistence");
				settings.Game.SelectionBoxMinimumZoom = new GameSettings().SelectionBoxMinimumZoom;
				settings.Save();
				Check(new Settings(file, new Arguments(Array.Empty<string>())).Game.SelectionBoxMinimumZoom == 1f, "Default reset persistence");
			}
			finally { if (File.Exists(file)) File.Delete(file); }
			Console.WriteLine("PASS: selection zoom boundaries, always-show, validation, save/load and reset.");
		}
	}
}
