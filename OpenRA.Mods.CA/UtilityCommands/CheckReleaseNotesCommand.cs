using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRA.Mods.CA.Widgets.Logic;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckReleaseNotesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-release-notes";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Validate release notes, menu wiring and offline cache without network requests.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			string Read(string path)
			{
				using var reader = new StreamReader(utility.ModData.DefaultFileSystem.Open(path));
				return reader.ReadToEnd();
			}
			var json = Read("ca|release-notes.json");
			var news = ReleaseNotesFeed.Parse(json);
			if (news.Length != 1 || !news[0].Title.StartsWith("YMCA v")) throw new InvalidDataException("Missing bundled release.");
			_ = new WidgetLoader(utility.ModData);
			var menu = Read("ca|chrome/mainmenu.yaml");
			if (menu.Contains("Background@NEWS_BG:") || !menu.Contains("Background@YMCA_NEWS_BG:") ||
				!menu.Contains("Button@YMCA_DISCORD_BUTTON:") || !menu.Contains("Text: You must construct additional News"))
				throw new InvalidDataException("Incorrect menu wiring.");
			foreach (var key in new[] { "draft", "prerelease" })
			{
				var altered = JObject.Parse(json); altered[key] = true; Reject(altered.ToString());
			}
			foreach (var url in new[] { "https://github.com/other/project/releases/tag/v1", "javascript:alert(1)", "https://github.com.evil.test/patrickwieth/YMCA/releases/tag/v1" })
			{
				var altered = JObject.Parse(json); altered["html_url"] = url; Reject(altered.ToString());
			}
			Reject("<html>error</html>"); Reject(new string('x', 262145));
			var edited = JObject.Parse(json);
			edited["body"] = "## Features\n- **Updated notes**";
			if (!ReleaseNotesFeed.Parse(edited.ToString())[0].Content.Contains("- Updated notes"))
				throw new InvalidDataException("Edited release notes not rendered.");
			CheckCache(json, edited.ToString()).GetAwaiter().GetResult();
			Console.WriteLine("Release notes passed: bundled release, stable-only parsing, menu/Discord wiring, edited notes, offline-cache preservation.");
		}

		static void Reject(string json)
		{
			try { ReleaseNotesFeed.Parse(json); }
			catch (Exception ex) when (ex is InvalidDataException || ex is JsonException) { return; }
			throw new InvalidOperationException("Invalid release accepted.");
		}

		static async Task CheckCache(string initial, string edited)
		{
			var dir = Path.Combine(Path.GetTempPath(), "ymca-release-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try
			{
				var cache = Path.Combine(dir, ReleaseNotesFeed.CacheFile);
				foreach (var text in new[] { initial, edited })
				{
					using var client = new HttpClient(new Handler(HttpStatusCode.OK, text));
					await ReleaseNotesFeed.FetchAsync(client, cache);
					if (File.ReadAllText(cache) != text) throw new InvalidOperationException("Notes/cache did not refresh.");
				}
				foreach (var code in new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.NotFound })
				{
					using var client = new HttpClient(new Handler(code, "<html>error</html>"));
					var failed = false;
					try { await ReleaseNotesFeed.FetchAsync(client, cache); }
					catch (Exception ex) when (ex is JsonException || ex is HttpRequestException || ex is InvalidDataException) { failed = true; }
					if (!failed || File.ReadAllText(cache) != edited || Directory.GetFiles(dir).Length != 1)
						throw new InvalidOperationException("Failed fetch damaged cached notes.");
				}
			}
			finally { Directory.Delete(dir, true); }
		}
		sealed class Handler : HttpMessageHandler
		{
			readonly HttpStatusCode status;
			readonly string text;
			public Handler(HttpStatusCode status, string text) { this.status = status; this.text = text; }
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				if (request.Method != HttpMethod.Get || request.RequestUri.AbsoluteUri != ReleaseNotesFeed.Url)
					throw new InvalidOperationException("Only the read-only release endpoint is allowed.");
				return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(text) });
			}
		}
	}
}
