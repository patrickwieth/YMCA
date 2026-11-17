# v0.97.0 preparation

The user authorized merging isometric into master and preparing v0.97.0, and will
write the public release notes. Explicit restriction: do not trigger YMCA Overlord
release announcements.

Included: merged isometric changes, Liberty stream shellmap, hidden My factions,
multiplayer map wizard, YMCA GitHub release notes with permanent Discord link.
The reported Aegis missile turret alignment correction is still not implemented.

Release validation found ignored engine changes missing from clean builds.
`isometric-runtime-completion.patch` now captures the running Game engine's
AutoTile/editor support, sprite bottom anchor, repair docking and selection
follow-ups, after the existing engine patch. The existing patch also needed its
missing System.Linq import. CI and all three packaging jobs prepare both patches.
The production Release build, 311 modular tests, 13 tournament tests, news/cache
checks and Liberty rules/terrain/sprite preflight passed in Game-release.

PUBLICATION BLOCKER: Overlord polls update/stable.yaml and announces automatically.
Both available SSH clients failed to authenticate to the server, so announcement
suppression has NOT been verified. Do not push the v0.97.0 tag or publish a stable
release until the running bot has announcements disabled (or another verified
suppression mechanism is in place). A GitHub draft is safe to edit; publishing it
is not yet safe. Do not bypass match safety guards or force bot/server restarts.

The normal tag pipeline remains builds -> manifest -> server deployment; no tag
or deployment is needed for an editable release draft. Do not infer deployment
success from draft creation or branch pushes.
