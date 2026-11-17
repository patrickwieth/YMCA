using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class PlateauActorEdit
	{
		public static Action<bool> Replace(EditorActorLayer layer, IReadOnlyCollection<MiniYamlNode> before, IReadOnlyCollection<MiniYamlNode> after)
		{
			var remove = Patch(layer, before, true);
			var add = Patch(layer, after, false);
			return forward =>
			{
				if (forward)
				{
					remove(true);
					try { add(true); }
					catch { remove(false); throw; }
				}
				else
				{
					add(false);
					try { remove(false); }
					catch { add(true); throw; }
				}
			};
		}

		public static Action<bool> Patch(EditorActorLayer layer, IReadOnlyCollection<MiniYamlNode> definitions, bool erase)
		{
			return forward =>
			{
				var added = new List<EditorActorPreview>();
				var removed = new List<EditorActorPreview>();
				try
				{
					foreach (var node in definitions)
						if (forward != erase)
						{
							if (layer[node.Key] != null) throw new InvalidOperationException("Plateau actor ID already exists.");
							added.Add(layer.Add(node.Key, new ActorReference(node.Value.Value, node.Value.ToDictionary())));
						}
						else
						{
							var preview = layer[node.Key] ?? throw new InvalidOperationException("Plateau actor is missing.");
							removed.Add(preview);
							layer.Remove(preview);
						}
				}
				catch
				{
					foreach (var preview in added) layer.Remove(preview);
					foreach (var preview in removed) layer.Add(preview);
					throw;
				}
			};
		}
	}
}
