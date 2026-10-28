# YMCA Battlefield News

Status: implemented locally in Game-master; not released or pushed. Existing v0.96.22 clients
still use the engine's OpenRA news feed. Carry this implementation into the next approved release;
do not defer it again merely because it changes the news UI.

## Implementation

- MainMenuLogicCA subclasses the engine menu logic, preserving navigation/startup/direct-connect
  behavior without changing or repinning the engine. The menu intentionally renames the optional
  native NEWS_BG to YMCA_NEWS_BG so the engine news loader does not run alongside the YMCA loader.
- A separate YMCA_NEWS_PANEL renders wrapped titles, author/date, content and optional link buttons.
- The canonical news file is `mods/ca/news.yaml`, both the bundled offline fallback and the future
  remote feed at `https://raw.githubusercontent.com/patrickwieth/YMCA/tournament-bot/mods/ca/news.yaml`.
- The cache is `ymca-news.yaml` in the support directory, separate from OpenRA's `news.yaml`.
- On menu startup, valid cached news overrides the bundled fallback. After the startup/privacy
  prompts, the enabled news feature fetches once per feed/cache combination per game launch.
  News fetching can still be disabled through the existing FetchNews setting. No system-information
  query parameters are sent to the GitHub feed.
- A successful download is parsed before replacing the cache atomically. HTTP/parsing/network errors
  retain the visible cached/bundled news. Corrupt caches fall back to the bundled file. Cache-write
  failure does not prevent displaying a valid download. Empty valid feeds intentionally clear news.
- Newly dated downloaded news opens the dropdown once while an appropriate menu is visible, never
  over a running game. Late callbacks do not update disposed menu instances.
- Only explicit clicks open links, and only absolute HTTPS URLs without embedded credentials are
  accepted. Titles/content/link labels are wrapped to fit the existing 400-pixel dropdown.

The live URL will not have this file until an authorized push includes it on tournament-bot.
Local testing therefore uses the bundled announcement even if the remote request returns 404.
Do not publish a release or touch modular/isometric work merely to enable local testing.

## Editing news after the first compatible release

Edit `mods/ca/news.yaml` on tournament-bot and publish that file with an authorized normal branch
commit/push. No new tag or client build is needed for those clients to fetch the updated text on
next launch. Existing pre-feature clients cannot gain support from a feed edit alone.

```yaml
2026-10-28:
    Title: YMCA World Championships 20–22 November 2026!
    Author: YMCA
    Content: Join us for the YMCA World Championships from 20 to 22 November 2026.
    LinkText: Register on our Discord server!
    LinkUrl: https://discord.gg/maGHYC3cVk
```

Use tabs in the actual MiniYaml file. Date keys must be unique yyyy-MM-dd values; newest entries
are displayed first. Content supports literal `\n` line breaks. LinkText and LinkUrl are optional
but must occur together. Maximum 20 entries / 64 KiB, titles 256 characters, content 4096 characters,
link labels 120 characters. Use a new date for a new announcement. Recheck the championship date's
relevance before releasing after November 2026; update the bundled-announcement regression check
when intentionally replacing that announcement.

## Validation and release integration

`dotnet engine/bin/OpenRA.Utility.dll mods/ca --check-battlefield-news` validates the bundled text,
configuration, all chrome layout parsing/root IDs, link wiring, protocol restrictions, malformed
input, optional links, empty feeds and cache preservation/update using mocked HTTP (no network).

Manual UI check after completely restarting Game-master: open Battlefield News, verify the full
wrapped headline and clickable registration label, and confirm that the browser opens the Discord
invite. Also check FetchNews disabled, offline startup and returning from other menu panels.
A build/headless check is not a substitute for this visual/browser test.

When integrating onto tournament-bot, merge the two new fields into its EXISTING WebServices block;
retain UpdateManifest. Do not introduce a duplicate WebServices block or replace the production
engine pin. Validate against that engine and ensure `ca|chrome/battlefield-news.yaml` and the bundled
news file are packaged before tagging. Do not edit an old release to deliver this feature.
