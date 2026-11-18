using System;
using System.IO;
using OpenRA.Mods.CA.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSkirmishWidgetArgumentsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-skirmish-widget-arguments";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		[Desc("Verify required wizard constructor arguments through ObjectCreator for local and dedicated callers.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var creator = utility.ModData.ObjectCreator;
			var constructor = creator.GetCtor(typeof(SkirmishSetupLogic));
			Action<string> select = _ => { };
			Action<string> generate = _ => { };
			Action existing = () => { };
			foreach (var remote in new[] { false, true })
			{
				var supplied = new WidgetArgs { { "onSelect", select } };
				if (remote)
				{
					supplied.Add("onGenerateRecipe", generate);
					supplied.Add("onChooseExisting", existing);
				}

				var prepared = SkirmishSetupLogic.PrepareArguments(supplied);
				if (!remote && supplied.ContainsKey("onGenerateRecipe"))
					throw new InvalidDataException("Preparing a window mutated the caller's arguments.");
				// WidgetLoader/PostInit supply these before invoking the real constructor.
				prepared.Add("widget", null);
				prepared.Add("modData", utility.ModData);
				foreach (var parameter in constructor.GetParameters())
					if (!prepared.ContainsKey(parameter.Name))
						throw new InvalidDataException("Missing wizard constructor argument: " + parameter.Name);

				var captured = (Callbacks)creator.CreateUsingArgs(typeof(Callbacks).GetConstructors()[0], prepared);
				if (captured.Select != select || captured.Generate != (remote ? generate : null) ||
					captured.Existing != (remote ? existing : null))
					throw new InvalidDataException("Wizard callbacks were lost or replaced.");
			}

			Console.WriteLine("Wizard arguments passed: local null callbacks and dedicated callbacks supplied explicitly through ObjectCreator.");
		}

		sealed class Callbacks
		{
			public readonly Action<string> Select;
			public readonly Action<string> Generate;
			public readonly Action Existing;
			public Callbacks(Action<string> onSelect, Action<string> onGenerateRecipe, Action onChooseExisting)
			{
				Select = onSelect;
				Generate = onGenerateRecipe;
				Existing = onChooseExisting;
			}
		}
	}
}
