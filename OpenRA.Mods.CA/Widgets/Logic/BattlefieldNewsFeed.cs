using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenRA.Support;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public sealed class BattlefieldNewsItem
	{
		public string Title;
		public string Author;
		public DateTime Date;
		public string Content;
		public string LinkText;
		public string LinkUrl;
	}

	public static class BattlefieldNewsFeed
	{
		public static BattlefieldNewsItem[] Parse(string text)
		{
			if (text.Length > 65536)
				throw new InvalidDataException("News feed is too large.");
			var nodes = MiniYaml.FromString(text, "YMCA Battlefield News");
			if (nodes.Count > 20)
				throw new InvalidDataException("Too many news entries.");
			var result = nodes.Select(node =>
			{
				if (!DateTime.TryParseExact(node.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture,
					DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
					throw new InvalidDataException("News entry dates must use yyyy-MM-dd.");
				var fields = node.Value.ToDictionary();
				string Field(string key, int max, bool required = true)
				{
					var value = fields.TryGetValue(key, out var field) ? field.Value ?? "" : "";
					if ((required && string.IsNullOrWhiteSpace(value)) || value.Length > max)
						throw new InvalidDataException("Invalid news field: " + key);
					return value;
				}

				var item = new BattlefieldNewsItem
				{
					Date = date,
					Title = Field("Title", 256),
					Author = Field("Author", 100),
					Content = Field("Content", 4096, false),
					LinkText = Field("LinkText", 120, false),
					LinkUrl = Field("LinkUrl", 2048, false)
				};
				if ((item.LinkText.Length == 0) != (item.LinkUrl.Length == 0) ||
					(item.LinkUrl.Length != 0 && !IsSafeLink(item.LinkUrl)))
					throw new InvalidDataException("News links require both a label and an absolute HTTPS URL.");
				return item;
			}).OrderByDescending(n => n.Date).ToArray();
			if (result.Select(n => n.Date).Distinct().Count() != result.Length)
				throw new InvalidDataException("Duplicate news dates.");
			return result;
		}

		public static bool IsSafeLink(string url)
		{
			return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
				uri.Host.Length != 0 && uri.UserInfo.Length == 0 && uri.IsWellFormedOriginalString();
		}

		public static async Task<BattlefieldNewsItem[]> FetchAsync(string url, string cachePath)
		{
			if (!IsSafeLink(url))
				throw new InvalidDataException("News feed must use HTTPS.");
			using var client = HttpClientFactory.Create();
			client.Timeout = TimeSpan.FromSeconds(15);
			client.MaxResponseContentBufferSize = 65536;
			return await FetchAsync(client, url, cachePath);
		}

		internal static async Task<BattlefieldNewsItem[]> FetchAsync(HttpClient client, string url, string cachePath)
		{
			if (!IsSafeLink(url))
				throw new InvalidDataException("News feed must use HTTPS.");
			var text = await client.GetStringAsync(url);
			var news = Parse(text); // Never overwrite the last valid cache with an error page or malformed feed.
			var temporary = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				await File.WriteAllTextAsync(temporary, text);
				File.Move(temporary, cachePath, true);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				Log.Write("debug", "Could not cache YMCA news: " + ex.Message);
			}
			finally
			{
				try { if (File.Exists(temporary)) File.Delete(temporary); }
				catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
				{ Log.Write("debug", "Could not remove temporary YMCA news cache: " + ex.Message); }
			}

			return news;
		}
	}
}
