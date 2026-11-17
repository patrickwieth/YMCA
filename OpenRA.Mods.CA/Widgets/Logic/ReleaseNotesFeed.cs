using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Read-only GitHub client. Never publishes releases or sends Discord messages.
	public static class ReleaseNotesFeed
	{
		public const string Url = "https://api.github.com/repos/patrickwieth/YMCA/releases/latest";
		public const string CacheFile = "ymca-release-notes.json";
		public const string DiscordUrl = "https://discord.gg/maGHYC3cVk";
		public const string ReleasesUrl = "https://github.com/patrickwieth/YMCA/releases";

		public static BattlefieldNewsItem[] Parse(string json)
		{
			if (json.Length > 262144) throw new InvalidDataException("Release response is too large.");
			var release = JObject.Parse(json);
			var tag = (string)release["tag_name"];
			var url = (string)release["html_url"];
			if ((bool?)release["draft"] != false || (bool?)release["prerelease"] != false ||
				string.IsNullOrWhiteSpace(tag) || tag.Length > 100 ||
				!BattlefieldNewsFeed.IsSafeLink(url) || !url.StartsWith(ReleasesUrl + "/tag/", StringComparison.Ordinal))
				throw new InvalidDataException("Expected a published stable YMCA release.");
			if (!DateTime.TryParse((string)release["published_at"], CultureInfo.InvariantCulture,
				DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
				throw new InvalidDataException("Invalid release date.");
			var body = (string)release["body"] ?? "";
			if (body.Length > 32768) throw new InvalidDataException("Release notes are too long.");
			// The native news panel renders plain text, not HTML or Markdown.
			body = Regex.Replace(body, @"(?m)^#{1,6}\s+", "");
			body = Regex.Replace(body, @"\[([^\]]+)\]\(([^)]+)\)", "$1 ($2)");
			body = body.Replace("**", "").Replace("`", "").Trim();
			return new[] { new BattlefieldNewsItem
			{
				Title = "YMCA " + tag, Author = "YMCA", Date = date,
				Content = body.Length == 0 ? "Release notes have not been added yet. Open the release page for updates." : body,
				LinkText = "Read release notes on GitHub", LinkUrl = url
			} };
		}

		public static async Task<BattlefieldNewsItem[]> FetchAsync(string cachePath)
		{
			using var client = HttpClientFactory.Create();
			client.Timeout = TimeSpan.FromSeconds(15);
			client.MaxResponseContentBufferSize = 262144;
			client.DefaultRequestHeaders.UserAgent.ParseAdd("YMCA-ReleaseNotes/1.0");
			client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
			return await FetchAsync(client, cachePath);
		}

		internal static async Task<BattlefieldNewsItem[]> FetchAsync(HttpClient client, string cachePath)
		{
			var json = await client.GetStringAsync(Url);
			var news = Parse(json);
			var temporary = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				await File.WriteAllTextAsync(temporary, json);
				if (File.Exists(cachePath))
					File.Replace(temporary, cachePath, null);
				else
					File.Move(temporary, cachePath);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{ Log.Write("debug", "Could not cache YMCA release notes: " + ex.Message); }
			finally
			{
				try { if (File.Exists(temporary)) File.Delete(temporary); }
				catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
				{ Log.Write("debug", "Could not remove release notes temporary file: " + ex.Message); }
			}
			return news;
		}
	}
}
