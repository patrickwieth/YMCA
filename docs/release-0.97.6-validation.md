# v0.97.6 release validation

Authorized by the user on 20 November 2026: release all discussed follow-up fixes.

## Scope

- Client selection-frame zoom slider, default/reset 100%, health bars unchanged.
- Default-off synchronized lobby option for green/blue Tiberium infantry damage,
  retaining damage values and existing immunities when enabled.
- Elite Cyborg (Range) cameo palette correction.
- Tesla Reactor body and selection registration for the existing 3x3 foundation;
  separate reactor added to the disposable building-test display.
- Soviet player-colored foundation joints restored for TPWR and Soviet refinery.
- Tournament Docker and Compose version pins advanced together to v0.97.6.

Only approved changes were staged. Protected Nod conyard and unrelated art studies,
Blender files, caches, experimental maps and personal replay remain untouched.
Existing Battlefield News and UpdateManifest remain in one WebServices block.

## Validation before tag publication

- Full Windows engine/mod Release build in Game-release passed, zero errors.
- Modular tests: 365 passed; tournament tests: 96 passed, including host version pin.
- Shared engine-patch preparation and public deployment-guard regression tests pass.
- Native release-worktree selection test: retained eight selected actors; hidden,
  always-show and close-up frame pixels 0/1950/2148; unchanged 468 health pixels.
  Real UI slider, persistence and Reset to100% pass.
- Native release-worktree Tiberium simulations pass for absent/default, on and off.
  Both resource types, clear ground, hazmat, cyborg, Scrin and vehicle cases checked.
- Resolved production/promotion Elite Cyborg icons all use rmbocicnh.shp/chrometd.
- Expanded bib utility verifies Soviet/China masks and other faction mappings.
- Tesla placement and restored joint colors reviewed in actual native screenshots;
  source PNG checksum and focused calibration regression pass.
- Protected Nod conyard SHA256 check passes; no unrelated assets included.
- Historical broad Soviet calibration/occupancy failures remain unrelated and are
  not claimed fixed (see individual feature documents).

## Safety preflight

Read-only workflow 44695235304 succeeded. Tournament host currently v0.97.5 has
M0032 and M0033 WaitingForPlayers. Do not interrupt or discard these matches.
Tournament deployment is deferred while matches remain active/retained. No timers,
resolutions, replacement tournaments or force overrides are requested or activated.
Public server deployment uses the committed fail-closed established-connection
check on1234/1235; Cameo is outside the restart scope.

## Publication status

Prepared and validated; tag/package workflow and live verification still pending.
Feature documents saying local-only describe their pre-release implementation stage;
this release record tracks subsequent publication and deployment results.
