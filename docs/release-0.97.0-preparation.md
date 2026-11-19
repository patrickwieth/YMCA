# v0.97.0 preparation

The user authorized merging isometric into master and preparing v0.97.0, and will
write the public release notes. Explicit restriction: do not trigger YMCA Overlord
release announcements.

Included: merged isometric changes, Liberty stream shellmap, hidden My factions,
multiplayer map wizard, YMCA GitHub release notes with permanent Discord link.
The reported Aegis missile turret alignment correction is still not implemented.

Correction after CI failures: the initial local release-engine validation used
an older engine checkout. A clean download of the pinned engine commit
2dfb43c864804ede5efca9e31f7add52f7233aa3 already contains AutoTile, sprite anchors,
repair docking and the LINQ import. The redundant completion patch and added
import hunk were removed. Packaging now applies only the original idempotent
selection/building presentation patch. The clean pinned-engine .NET build and
both offline/cache checks pass. News cache replacement now uses File.Replace
(or two-argument File.Move for first creation), compatible with Mono.
Earlier modular and tournament tests passed; the new CI run must confirm Mono.

PUBLICATION BLOCKER: Overlord polls update/stable.yaml and announces automatically.
Both available SSH clients failed to authenticate to the server, so announcement
suppression has NOT been verified. Do not push the v0.97.0 tag or publish a stable
release until the running bot has announcements disabled (or another verified
suppression mechanism is in place). A GitHub draft is safe to edit; publishing it
is not yet safe. Do not bypass match safety guards or force bot/server restarts.

The user subsequently published the draft, creating tag v0.97.0 at 724b0033.
Packaging run 44115441076 was cancelled before uploads/manifest/deployment to
preserve the no-announcement restriction and avoid known build failures.
The release has no binaries. Fix commits do not automatically change that tag;
do not silently rewrite the published tag. User-written release notes must be
preserved. The normal tag pipeline remains builds -> manifest -> deployment.
