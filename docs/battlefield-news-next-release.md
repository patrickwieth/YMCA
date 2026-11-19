# Battlefield News — next suitable release

Status: implemented and present on the release line. Retained in the authorized
v0.97.2 preparation alongside gameplay fixes and tournament support. This is not
a standalone announcement release; see `battlefield-news.md` for the implementation.

## Announcement

YMCA World Championships 20–22 November 2026!

Link text: Register on our Discord server!
Discord destination: https://discord.gg/maGHYC3cVk

## Required changes

- Configure a YMCA-owned news feed instead of the default OpenRA game-news endpoint.
- Add clickable HTTPS link support to the news presentation; the current release uses plain labels.
- Ensure long titles/content fit the panel and the Discord link is visibly clickable.
- Verify parsing, cached/offline behavior and link handling before packaging.
- Once the compatible client is released, subsequent news text should be editable in the feed
  without rebuilding the client.

Existing v0.96.21 binaries remain unchanged. Prepare this on the current release line, without
bringing in modular designer experiments. Recheck whether the announcement is still timely if
the next release happens after the championship dates. No new version number is reserved here.
