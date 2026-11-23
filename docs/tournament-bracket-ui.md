# Bracket UI redesign — deployed 2026-11-23

User priorities: (1) standard fixed double-elimination routing/cross-feeds that delay rematches, especially in the lower bracket; (2) a readable interactive bracket, inspired by the BieberLAN tournament tree; (3) existing Discord replay links must remain usable.

The supplied reference URL returned a Cloudflare challenge to the available HTTP client. No claim is made to have inspected its rendered UI. Implementation follows the user's described interaction goals, not copied third-party assets.

## Implemented

- Separate sections and navigation for upper bracket, lower bracket, final and third place (where recorded).
- Compact series summaries with players, scores and status. Native expandable details show maps, individual games, deadlines, walkovers and unchanged replay/spectator links. Details and links work without JavaScript.
- Later cards align with feeder cards. Orthogonal SVG edges connect player rows only within adjacent columns of the same bracket.
- Cross-bracket and skipped-round transitions are explicit source links, labeled Winner/Loser when a completed result is known. Clicking navigates to and focuses the source card. No sprawling lines across unrelated sections/cards.
- Hovering a player highlights their recorded path throughout the bracket. Keyboard focus highlights the selected series' participants. Mobile uses horizontally scrollable bracket regions and native tap-to-expand summaries.
- Ten-second refresh preserves expanded cards, horizontal scroll positions and focused summary/link/scroll-region. Resize and detail expansion redraw edges.
- Escaping, private-data restrictions and result-confirmation semantics retained.

## History / replay guarantees

Rendering is read-only. No tournament state migration, re-seeding, new match IDs, result edits, replay relocation, deletion or route changes. Regression test serializes the full fixture before/after rendering and checks every original `/replay/M...` href. Existing deployed replay pages/downloads remain untouched.

**Versioned fixed routing is also deployed**; see `tournament-fixed-routing.md`. Historical pairings remain unchanged. Source links reflect each player's last recorded earlier-round series; new tournaments additionally persist fixed slot IDs. Completed tournaments are never recalculated.

## Validation

- Pre-deployment .NET suite: 133 passed, one optional remote smoke skipped.
- Browser: isolated headless Edge, desktop 1440x1000 and mobile 390x844. Verified separate lanes; same-lane adjacent edges only; hover highlights; source navigation; details; unchanged replay hrefs; refresh retention of expanded state/focus/scroll; no page-wide mobile overflow; no JavaScript errors; replay access through native details with JavaScript disabled.
- Inspected actual desktop/mobile screenshots, generated from explicitly labeled synthetic fixtures (not real T013 results).

Reproduce from workspace root:

```sh
YMCA_WEB_PREVIEW='E:/src/YMCA/.tmp/bracket-preview' dotnet test .tmp/tournament-remote-host/TournamentBot.Tests/TournamentBot.Tests.csproj --no-restore
YMCA_PLAYWRIGHT='E:/src/YMCA/.tmp/bracket-browser/node_modules/playwright' YMCA_BROWSER_EXECUTABLE='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' node .tmp/tournament-remote-host/tools/test_tournament_bracket_browser.cjs
```

Browser test requires Playwright; paths are environment-configurable. Local Node 18 required Playwright 1.48.2; it uses the installed Edge executable, not a browser downloaded by the package. Prefer current Node/Playwright for CI. Neither is a production dependency.

Published with the authorized bot-only rollout **45249852873**, source `61f9a49aeeb58ab04b90aa5998f981260440f5da`, along with fixed routing, exact-name resolution and timestamp safety. Live T013 desktop/mobile checks passed; all 62 public replays and 62 maps remain byte-identical. Game binaries/assets were not changed.
