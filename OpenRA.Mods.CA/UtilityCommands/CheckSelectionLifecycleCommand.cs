using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRA.Mods.CA.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSelectionLifecycleCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-selection-lifecycle";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

		[Desc("Regression: pending render updates must not query removed/disposed actor bounds.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			foreach (var disposed in new[] { false, true })
			{
				// Deliberately no World, traits, renderer or providers: any bounds query fails.
				var actor = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
				typeof(Actor).GetProperty(nameof(Actor.Disposed)).SetValue(actor, disposed);
				var map = (ScreenMap)RuntimeHelpers.GetUninitializedObject(typeof(ScreenMap));
				Set(map, "addOrUpdateActors", new HashSet<Actor>());
				Set(map, "removeActors", new HashSet<Actor>());
				var mouse = new SpatiallyPartitioned<Actor>(1000, 1000, 250);
				var render = new SpatiallyPartitioned<Actor>(1000, 1000, 250);
				mouse.Add(actor, new Rectangle(10, 10, 20, 20));
				render.Add(actor, new Rectangle(10, 10, 20, 20));
				Set(map, "partitionedMouseActors", mouse);
				Set(map, "partitionedRenderableActors", render);
				Set(map, "partitionedMouseActorBounds", new Dictionary<Actor, ActorBoundsPair>());
				Set(map, "addOrUpdateFrozenActors", new Cache<Player, HashSet<FrozenActor>>(_ => new HashSet<FrozenActor>()));
				Set(map, "removeFrozenActors", new Cache<Player, HashSet<FrozenActor>>(_ => new HashSet<FrozenActor>()));
				map.AddOrUpdate(actor);
				if (!disposed) map.Remove(actor);
				map.TickRender();
				map.TickRender();
				if (mouse.Contains(actor) || render.Contains(actor)) throw new InvalidOperationException("Stale actor retained in screen partitions.");
				if (disposed)
				{
					var decoration = (IRender)RuntimeHelpers.GetUninitializedObject(typeof(IsometricSelectionDecorationsForUnits));
					if (decoration.ScreenBounds(actor, null).Any()) throw new InvalidOperationException("Disposed actor still has selection bounds.");
				}
			}
			foreach (var width in new[] { 16, 45, 90, 128 })
				foreach (var height in new[] { 24, 48, 96, 160 })
					foreach (var offset in new[] { -20, 0, 20 })
					{
						var body = new Rectangle(-width / 2 + offset, 12 - height, width, height);
						var prism = IsometricSelectionDecorationsForUnits.ProjectBounds(new Rectangle(-45, -22, 90, 44), body, 24);
						foreach (var corner in new[] { body.TopLeft, body.TopRight + new int2(-1, 0), body.BottomLeft + new int2(0, -1), body.BottomRight - new int2(1, 1) })
							if (!prism.Contains(corner)) throw new InvalidOperationException("Body corner outside fitted selection prism.");
					}
			Console.WriteLine("Deferred removal/disposal: no trait/provider access; both screen partitions cleaned; repeated render tick safe; disposed unit decoration empty. Offset/tall/wide body corners fit the selection prism.");
		}
	}
}
