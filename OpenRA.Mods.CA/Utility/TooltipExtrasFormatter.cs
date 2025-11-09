using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenRA;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.CA.Tooltips
{
    public static class TooltipExtrasFormatter
    {
        static readonly string NewLine = Environment.NewLine;
        static readonly string DoubleNewLine = NewLine + NewLine;
        static readonly string LineFeedString = ((char)10).ToString();

        const char BulletChar = '\u2022';
        const char LegacyBulletChar = '\u0007';
        const char LineFeed = (char)10;
        const string LegacyNewLineLiteral = "\n";
        static readonly string BulletString = BulletChar.ToString();
        static readonly string LegacyBulletString = LegacyBulletChar.ToString();
        static readonly string BulletPrefix = $"  \u2022 ";
        static readonly Regex MidSentenceBullet = new Regex(@"(?<!^)(?<!\n)[^\S\n\r]*\u0007", RegexOptions.Compiled);

        static readonly (string Key, string Fallback, Func<TooltipExtrasInfo, string> Selector)[] BulletSections =
        {
            ("tooltipextras.strengths", "Strengths", info => info.Strengths),
            ("tooltipextras.weaknesses", "Weaknesses", info => info.Weaknesses),
            ("tooltipextras.attributes", "Attributes", info => info.Attributes)
        };

        public static string Format(IEnumerable<TooltipExtrasInfo> extras)
        {
            if (extras == null)
                return string.Empty;

            var sections = new List<string>();

            foreach (var info in extras.Where(i => i != null))
            {
                var parts = new List<string>();

                var description = NormalizeDescription(info.Description);
                AddIfNotEmpty(parts, description);

                foreach (var section in BulletSections)
                {
                    var text = FormatBulletSection(section.Key, section.Fallback, section.Selector(info));
                    AddIfNotEmpty(parts, text);
                }

                if (parts.Count > 0)
                    sections.Add(string.Join(DoubleNewLine, parts));
            }

            return string.Join(DoubleNewLine, sections);
        }

        static void AddIfNotEmpty(List<string> parts, string value)
        {
            if (!string.IsNullOrEmpty(value))
                parts.Add(value);
        }

        static string NormalizeDescription(string value)
        {
            var localized = LocalizeText(value);
            if (string.IsNullOrWhiteSpace(localized))
                return string.Empty;

            var normalized = NormalizeNewLines(localized);
            return string.Join(NewLine, normalized.Split(LineFeed)).Trim();
        }

        static string FormatBulletSection(string translationKey, string fallbackTitle, string value)
        {
            var list = FormatBulletList(value);
            if (list.Length == 0)
                return string.Empty;

            var title = FluentProvider.TryGetMessage(translationKey, out var localizedTitle)
                ? localizedTitle
                : fallbackTitle;

            if (list.StartsWith(title + ":", StringComparison.OrdinalIgnoreCase))
                return list;

            return $"{title}:{NewLine}{list}";
        }

        static string FormatBulletList(string value)
        {
            var localized = LocalizeText(value);
            return NormalizeBulletString(localized);
        }

        public static string NormalizeBulletString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var localized = LocalizeText(value);
            var normalized = NormalizeNewLines(localized).Trim();
            if (normalized.Length == 0)
                return string.Empty;

            // Handle legacy \u0007 control character
            normalized = normalized.Replace('\u0007', '\u2022');

            var parts = normalized.Split('\u2022');
            if (parts.Length == 1)
                return normalized;

            var lines = new List<string>();
            var prefix = parts[0].TrimEnd();
            if (prefix.Length > 0)
                lines.Add(prefix);

            for (var i = 1; i < parts.Length; i++)
            {
                var content = parts[i].Trim();
                if (content.Length == 0)
                    continue;

                lines.Add($"  \u2022 {content}");
            }

            return string.Join(NewLine, lines);
        }

        static string NormalizeNewLines(string value)
        {
            return value.Replace(LegacyNewLineLiteral, LineFeedString)
                .Replace("\r\n", LineFeedString)
                .Replace("\r", LineFeedString);
        }

        static string LocalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (FluentProvider.TryGetMessage(value, out var localized))
                return localized;

            return value;
        }
    }
}
