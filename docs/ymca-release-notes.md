# YMCA release notes in the menu

The menu heading is `You must construct additional News`. Its panel now reads
published stable YMCA release notes from the read-only GitHub endpoint
`https://api.github.com/repos/patrickwieth/YMCA/releases/latest`.
It no longer displays the OpenRA playtest feed or uses `mods/ca/news.yaml` as
its primary content. The earlier Battlefield News documentation describes the
superseded championship-feed implementation.

- Fetch at menu startup and every five minutes while the menu object exists.
  Editing the GitHub release body therefore updates clients without rebuilding.
- Respect FetchNews and the startup privacy prompts. Drafts/prereleases and
  non-YMCA release URLs are rejected. No authentication token is sent.
- Separate `ymca-release-notes.json` support-directory cache; malformed responses,
  network errors and rate limits leave the last valid notes visible.
- Bundled `mods/ca/release-notes.json` is the first-run offline fallback. It is a
  snapshot of published v0.96.22, not invented v0.97.0 notes. Refresh the snapshot
  for future packages once final release notes exist.
- Markdown headings/emphasis are simplified for the native plain-text panel.
  A GitHub button links to the complete release, and a permanent menu button
  links to `https://discord.gg/maGHYC3cVk`.
- `YmcaNewsMainMenuLogic` is the base of `MainMenuLogicCA` in SkirmishSetupLogic.cs,
  so the news loader and singleplayer/multiplayer wizard coexist without the
  duplicate MainMenuLogicCA definitions introduced by the earlier merge.

Validation: isolated build, `--check-release-notes` (including edited notes and
cache preservation using fake HTTP), and a 1000-tick isolated live menu run passed.
The check also loads registered chrome layouts and checks the menu/Discord wiring.
A visual review of the expanded news panel is still separate from those checks.

No GitHub releases, Discord/Overlord announcements, tags, pushes or deployments
are performed by this client code or by the implementation validation. The
previously identified Overlord release-publication blocker remains independent.
