# Automatic tournament map download

## Contract and user flow

The bot writes a bounded, versioned download receipt in each generated match's
support folder before starting its dedicated server. The server checks the
receipt against its actual assigned package (UID, size, SHA-256) and announces
it as a locked lobby option. The fixed tournament map pool remains enforced;
this does not grant players map-generation or map-change permissions.

Updated players and spectators download the exact archive automatically when
joining, including later joins. They do not copy files or restart the game.
Downloads use HTTPS, check size and SHA-256, validate archive paths/expansion
limits and the engine's map UID, then install a uniquely owned session package
into an actually loaded User map directory and refresh the live map cache.
Ready becomes available only after installation; the client then reports NotReady.
Existing maps with the same UID are reused without replacing user files.

If automatic downloads are disabled in settings, the lobby instead offers an
explicit Download map action. Failures leave the map unavailable and offer Retry;
leaving/changing the lobby cancels outstanding work. Late completions cannot
install into another lobby. The old repository search is suppressed while a
mod-managed download or recipe is active.

The join page retains a manual download link as a fallback for old clients. The
new behavior requires updated clients AND the updated tournament host/bot. It is
not silently enabled on a running v0.97.3 tournament.

## Safety

The shared `TournamentMapDownloadInfo` contract currently accepts only the
project-owned `https://tournament.crowdcontrol.network/map/M.../download` endpoint:
no HTTP, alternate ports, user info, query strings, fragments, arbitrary hosts or
local-network URLs. Redirects are not followed. There are no cookies or credentials
on the map request. Self-hosted services on other origins require an explicit
client trust-policy change, not an untrusted server-provided exception.

Compressed bodies are bounded to 16 MiB and two minutes; actual lengths are
checked even for chunked responses. Archive validation shares the portable replay
validator (64 MiB expanded, 256 entries, duplicate/unsafe path rejection, exact
UID). No archive entries are extracted to server-supplied filesystem paths.

A native multi-client test exposed an existing engine race: a delayed resource
repository response could overwrite a freshly installed local map and disconnect
a player at StartGame. `map-cache-local-priority.patch` keeps available/downloading
local maps authoritative and atomically discards stale remote metadata. The
replay-map utility also checks this invariant.

## Tests

- `Modular.Tests/TournamentMapDownloadTests.cs`: protocol validation, origin
  restrictions, size limits, HTTP failures/redirects, checksums, chunked/truncated
  bodies, cancellation and successful transfer.
- `TournamentBot.Tests/TournamentMapDownloadTests.cs`: immutable-package receipt
  binding and stale-receipt removal when disabled.
- `tools/test-tournament-map-download.ps1`: isolated dedicated host and fresh
  client profiles, real HTTPS download of a public immutable tournament map,
  exact local SHA/UID, late spectator, disabled-download preference and explicit
  permission, followed by a real two-player match start. It uses Launch.Connect
  rather than Launch.URI so the test cannot forward a join to a user's running game.

No live tournament state is changed by these tests. Evidence and final results
are recorded in the release validation document.
