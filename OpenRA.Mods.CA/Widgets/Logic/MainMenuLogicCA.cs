using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Keep the engine's menu/navigation behavior. The CA layout deliberately uses
	// YMCA_NEWS_BG instead of its optional NEWS_BG, so only this news loader runs.
	public class YmcaNewsMainMenuLogic : MainMenuLogic
	{
		static readonly HashSet<string> AnnouncedDownloads = new();
		readonly ModData modData;
		readonly ScrollPanelWidget panel;
		readonly Widget template;
		readonly LabelWidget status;
		readonly DropDownButtonWidget button;
		readonly int maxHeight;
		bool disposed;
		bool panelOpen;

		[ObjectCreator.UseCtor]
		public YmcaNewsMainMenuLogic(Widget widget, World world, ModData modData)
			: base(widget, world, modData)
		{
			this.modData = modData;
			var background = widget.Get("YMCA_NEWS_BG");
			background.IsVisible = () => Game.Settings.Game.FetchNews && menuType != MenuType.None && menuType != MenuType.StartupPrompts;
			button = background.Get<DropDownButtonWidget>("NEWS_BUTTON");
			background.Get<ButtonWidget>("YMCA_DISCORD_BUTTON").OnClick = () => OpenLink(ReleaseNotesFeed.DiscordUrl);
			panel = Ui.LoadWidget<ScrollPanelWidget>("YMCA_NEWS_PANEL", null, new WidgetArgs());
			template = panel.Get("NEWS_ITEM_TEMPLATE");
			status = panel.Get<LabelWidget>("NEWS_STATUS");
			panel.RemoveChild(template);
			maxHeight = panel.Bounds.Height;
			button.OnClick = () =>
			{
				panelOpen = true;
				button.AttachPanel(panel, () => panelOpen = false);
			};

			var updateNotice = widget.GetOrNull("UPDATE_NOTICE");
			if (updateNotice != null)
			{
				var originalVisibility = updateNotice.IsVisible;
				updateNotice.IsVisible = () => !panelOpen && originalVisibility();
			}

			// Respect the initial privacy/settings prompts before making any request.
			Game.RunAfterTick(LoadWhenReady);
		}

		void LoadWhenReady()
		{
			if (disposed)
				return;
			if (menuType == MenuType.StartupPrompts)
			{
				Game.RunAfterDelay(100, LoadWhenReady);
				return;
			}
			if (!Game.Settings.Game.FetchNews)
				return;

			BattlefieldNewsItem[] initial = null;
			try
			{
				using var reader = new StreamReader(modData.DefaultFileSystem.Open("ca|release-notes.json"));
				initial = ReleaseNotesFeed.Parse(reader.ReadToEnd());
			}
			catch (Exception ex) { Log.Write("debug", "Bundled YMCA news could not be loaded: " + ex.Message); }

			var cache = Path.Combine(Platform.SupportDir, ReleaseNotesFeed.CacheFile);
			var seenDates = new HashSet<DateTime>();
			try
			{
				if (File.Exists(cache))
				{
					initial = ReleaseNotesFeed.Parse(File.ReadAllText(cache));
					seenDates.UnionWith(initial.Select(n => n.Date));
				}
			}
			catch (Exception ex) { Log.Write("debug", "Cached YMCA news could not be loaded: " + ex.Message); }

			Display(initial ?? Array.Empty<BattlefieldNewsItem>());
			LoadRemote(ReleaseNotesFeed.Url, cache, seenDates);
		}

		async void LoadRemote(string url, string cache, HashSet<DateTime> seenDates)
		{
			try
			{
				var key = url + "|" + cache;
				var news = await ReleaseNotesFeed.FetchAsync(cache);
				Game.RunAfterTick(() =>
				{
					if (disposed)
						return;
					Display(news);
					if (Game.Settings.Game.FetchNews && menuType != MenuType.None && menuType != MenuType.StartupPrompts &&
						news.Any(n => !seenDates.Contains(n.Date)) && AnnouncedDownloads.Add(key) && !panelOpen)
						button.OnClick();
				});
			}
			catch (Exception ex)
			{
				// Leave cached/bundled news visible on HTTP, network or parsing failures.
				Log.Write("debug", "YMCA news download failed; keeping offline news: " + ex.Message);
			}
			finally
			{
				// Pick up edits to the published release notes without rebuilding or restarting.
				Game.RunAfterTick(() => Game.RunAfterDelay(300000, () =>
				{
					if (!disposed && Game.Settings.Game.FetchNews)
						LoadRemote(url, cache, seenDates);
				}));
			}
		}

		void Display(BattlefieldNewsItem[] news)
		{
			panel.RemoveChildren();
			if (news.Length == 0)
			{
				status.GetText = () => "YMCA release notes are currently unavailable.";
				panel.AddChild(status);
				panel.Bounds.Height = maxHeight;
				return;
			}

			foreach (var item in news)
			{
				var row = template.Clone();
				var y = 0;
				void Label(string id, string text)
				{
					var label = row.Get<LabelWidget>(id);
					var font = Game.Renderer.Fonts[label.Font];
					var wrapped = WidgetUtils.WrapText(text.Replace("\\n", "\n"), label.Bounds.Width, font);
					label.GetText = () => wrapped;
					label.Bounds.Y = y;
					label.Bounds.Height = font.Measure(wrapped).Y;
					y += label.Bounds.Height + 8;
				}

				Label("TITLE", item.Title);
				Label("AUTHOR_DATETIME", item.Author + " — " + item.Date.ToString("d", CultureInfo.CurrentCulture));
				Label("CONTENT", item.Content);
				var link = row.Get<ButtonWidget>("NEWS_LINK");
				link.IsVisible = () => item.LinkUrl.Length != 0;
				if (item.LinkUrl.Length != 0)
				{
					var font = Game.Renderer.Fonts[link.Font];
					var text = WidgetUtils.WrapText(item.LinkText, link.Bounds.Width - 16, font);
					link.GetText = () => text;
					link.Bounds.Y = y;
					link.Bounds.Height = Math.Max(28, font.Measure(text).Y + 10);
					link.OnClick = () => OpenLink(item.LinkUrl);
					y += link.Bounds.Height + 8;
				}

				row.Bounds.Height = y;
				panel.AddChild(row);
			}

			panel.Layout.AdjustChildren();
			panel.Bounds.Height = Math.Min(panel.ContentHeight, maxHeight);
		}

		static void OpenLink(string url)
		{
			if (!BattlefieldNewsFeed.IsSafeLink(url))
				return;
			try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
			catch (Exception ex) { Log.Write("debug", "Could not open Battlefield News link: " + ex.Message); }
		}

		protected override void Dispose(bool disposing)
		{
			disposed = true;
			base.Dispose(disposing);
		}
	}
}
