using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.Mods.CA.Widgets.Logic;
using OpenRA.Mods.Common;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-battlefield-news", "Validate YMCA news, HTTPS links, layout wiring and cache failure handling without network requests.")]
	public sealed class CheckBattlefieldNewsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-battlefield-news";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			string Read(string path)
			{
				using var reader = new StreamReader(utility.ModData.DefaultFileSystem.Open(path));
				return reader.ReadToEnd();
			}

			var text = Read("ca|news.yaml").Replace("\r\n", "\n");
			var news = BattlefieldNewsFeed.Parse(text);
			var championship = news.Single(n => n.Title == "YMCA World Championships 20–22 November 2026!");
			if (championship.LinkText != "Register on our Discord server!" || championship.LinkUrl != "https://discord.gg/maGHYC3cVk")
				throw new InvalidDataException("Championship registration link is incorrect.");
			var services = utility.ModData.Manifest.Get<WebServices>();
			if (services.GameNews != "https://raw.githubusercontent.com/patrickwieth/YMCA/tournament-bot/mods/ca/news.yaml" ||
				services.GameNewsFileName != "ymca-news.yaml")
				throw new InvalidDataException("YMCA must use its own feed and cache.");
			_ = new WidgetLoader(utility.ModData); // Parse all registered layouts and reject duplicate root widget IDs.
			var menu = Read("ca|chrome/mainmenu.yaml");
			if (!menu.Contains("Logic: MainMenuLogicCA") || !menu.Contains("Background@YMCA_NEWS_BG:") || menu.Contains("Background@NEWS_BG:"))
				throw new InvalidDataException("Menu still uses the engine news loader.");
			if (!utility.ModData.Manifest.ChromeLayout.Contains("ca|chrome/battlefield-news.yaml") ||
				!Read("ca|chrome/battlefield-news.yaml").Contains("Button@NEWS_LINK:"))
				throw new InvalidDataException("Clickable news layout is missing.");

			foreach (var unsafeUrl in new[] { "javascript:alert(1)", "file:///C:/test.exe", "http://discord.gg/test", "https://user:pass@example.com/", "//example.com/" })
			{
				if (BattlefieldNewsFeed.IsSafeLink(unsafeUrl))
					throw new InvalidDataException("Unsafe news URL accepted.");
				Reject(text.Replace(championship.LinkUrl, unsafeUrl));
			}

			Reject(text.Replace("2026-10-28:", "not-a-date:"));
			Reject(text + "\n" + text);
			Reject(text.Replace("\tLinkText: Register on our Discord server!\n", ""));
			Reject("<html>upstream error</html>");
			Reject(new string('x', 65537));
			var withoutLink = text.Replace("\tLinkText: Register on our Discord server!\n", "")
				.Replace("\tLinkUrl: https://discord.gg/maGHYC3cVk\n", "");
			if (BattlefieldNewsFeed.Parse(withoutLink).Length != 1 || BattlefieldNewsFeed.Parse("").Length != 0)
				throw new InvalidDataException("Optional links/empty feeds are not supported.");

			CheckCache(text).GetAwaiter().GetResult();
			Console.WriteLine("Battlefield News checks passed: bundled announcement, own feed/cache, clickable layout, safe URLs, malformed-feed rejection, atomic cache update and offline-cache preservation.");
		}

		static void Reject(string text)
		{
			try { BattlefieldNewsFeed.Parse(text); }
			catch (Exception ex) when (ex is InvalidDataException || ex is YamlException || ex is ArgumentException) { return; }
			throw new InvalidOperationException("Malformed news feed was accepted.");
		}

		static async Task CheckCache(string text)
		{
			var directory = Path.Combine(Path.GetTempPath(), "ymca-news-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			try
			{
				var cache = Path.Combine(directory, "news.yaml");
				using (var client = new HttpClient(new FeedHandler(HttpStatusCode.OK, text)))
					await BattlefieldNewsFeed.FetchAsync(client, "https://example.com/news.yaml", cache);
				if (File.ReadAllText(cache) != text)
					throw new InvalidOperationException("Valid feed was not cached.");

				foreach (var status in new[] { HttpStatusCode.OK, HttpStatusCode.NotFound })
				{
					using var client = new HttpClient(new FeedHandler(status, "<html>error</html>"));
					var failed = false;
					try { await BattlefieldNewsFeed.FetchAsync(client, "https://example.com/news.yaml", cache); }
					catch (Exception ex) when (ex is InvalidDataException || ex is HttpRequestException || ex is YamlException) { failed = true; }
					if (!failed || File.ReadAllText(cache) != text || Directory.GetFiles(directory).Length != 1)
						throw new InvalidOperationException("Failed fetch overwrote the valid cache or left temporary files.");
				}
			}
			finally { Directory.Delete(directory, true); }
		}

		sealed class FeedHandler : HttpMessageHandler
		{
			readonly HttpStatusCode status;
			readonly string text;
			public FeedHandler(HttpStatusCode status, string text) { this.status = status; this.text = text; }
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
				=> Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(text) });
		}
	}
}
