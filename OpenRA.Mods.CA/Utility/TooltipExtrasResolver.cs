using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Tooltips
{
	public static class TooltipExtrasResolver
	{
		static readonly char[] TooltipNameSeparators = { '/', '\\', '.', ':', '-' };

		public static ActorInfo ResolveActorWithExtras(Ruleset rules, ActorInfo actor, bool requireStandard)
		{
			if (actor == null || rules == null)
				return actor;

			if (HasExtras(actor, requireStandard))
				return actor;

			foreach (var candidate in TooltipActorNameCandidates(actor.Name))
			{
				if (!rules.Actors.TryGetValue(candidate, out var canonicalActor) &&
					!rules.Actors.TryGetValue(candidate.ToLowerInvariant(), out canonicalActor))
					continue;

				if (HasExtras(canonicalActor, requireStandard))
					return canonicalActor;
			}

			return actor;
		}

		static bool HasExtras(ActorInfo actor, bool requireStandard)
		{
			var infos = actor.TraitInfos<TooltipExtrasInfo>();
			if (!requireStandard)
				return infos.Any();

			return infos.Any(info => info.IsStandard);
		}

		static IEnumerable<string> TooltipActorNameCandidates(string actorName)
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var queue = new Queue<string>();

			void Enqueue(string value)
			{
				if (string.IsNullOrWhiteSpace(value))
					return;

				if (seen.Add(value))
					queue.Enqueue(value);
			}

			Enqueue(actorName);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				yield return current;

				Enqueue(current.ToLowerInvariant());
				Enqueue(current.ToUpperInvariant());

				var prefixStripped = StripPromotionPrefix(current);
				if (!string.Equals(prefixStripped, current, StringComparison.Ordinal))
					Enqueue(prefixStripped);

				var segment = StripToLastSegment(current);
				if (!string.Equals(segment, current, StringComparison.Ordinal))
					Enqueue(segment);

				var normalizedSeparators = current.Replace('\\', '/');
				if (!string.Equals(normalizedSeparators, current, StringComparison.Ordinal))
					Enqueue(normalizedSeparators);

				var slashNormalized = normalizedSeparators.Replace("/", ".");
				if (!string.Equals(slashNormalized, normalizedSeparators, StringComparison.Ordinal))
					Enqueue(slashNormalized);

				var slashCompacted = normalizedSeparators.Replace("/", string.Empty);
				if (!string.Equals(slashCompacted, normalizedSeparators, StringComparison.Ordinal))
					Enqueue(slashCompacted);

				var compactUnderscore = current.Replace("_", string.Empty);
				if (!string.Equals(compactUnderscore, current, StringComparison.Ordinal))
					Enqueue(compactUnderscore);

				var underscoresAsDots = current.Replace("_", ".");
				if (!string.Equals(underscoresAsDots, current, StringComparison.Ordinal))
					Enqueue(underscoresAsDots);

				var underscoresAsHyphen = current.Replace("_", "-");
				if (!string.Equals(underscoresAsHyphen, current, StringComparison.Ordinal))
					Enqueue(underscoresAsHyphen);

				var dotsAsUnderscores = current.Replace(".", "_");
				if (!string.Equals(dotsAsUnderscores, current, StringComparison.Ordinal))
					Enqueue(dotsAsUnderscores);

				var compactDots = current.Replace(".", string.Empty);
				if (!string.Equals(compactDots, current, StringComparison.Ordinal))
					Enqueue(compactDots);

				var hyphenCompact = current.Replace("-", string.Empty);
				if (!string.Equals(hyphenCompact, current, StringComparison.Ordinal))
					Enqueue(hyphenCompact);
			}
		}

		static string StripPromotionPrefix(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return value;

			if (!value.StartsWith("promotion", StringComparison.OrdinalIgnoreCase))
				return value;

			var index = "promotion".Length;
			if (index < value.Length && (value[index] == 's' || value[index] == 'S'))
				index++;

			while (index < value.Length && (value[index] == '.' || value[index] == '/' || value[index] == '\\' || value[index] == '-' || value[index] == '_'))
				index++;

			return index >= value.Length ? value : value.Substring(index);
		}

		static string StripToLastSegment(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return value;

			var idx = value.LastIndexOfAny(TooltipNameSeparators);
			if (idx < 0 || idx == value.Length - 1)
				return value;

			return value.Substring(idx + 1);
		}
	}
}
