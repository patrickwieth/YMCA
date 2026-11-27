# Shared propaganda cameo overlay

`iconoverlays/speaker` now references the supplied
`bits/misc/icons/propagandaicon.png` directly, replacing `speakericonoverlay.shp`.
The 20×21 RGBA PNG is unmodified; the existing offset `1,1`, manager and `Speaker`
overlay type are unchanged. Existing and future `WithProductionIconOverlay` traits
using `Types: Speaker` automatically use this image, including building, vehicle
and commander-tree consumers. Full-size independent upgrade icons (such as the
Helix `speakericon` sequence) are not overwritten by a small overlay.

Engine-resolved binding, source hash, native China Building/Defense menu captures
and regression evidence: `art-source/propaganda-overlay/`.
Local only; no release, push, deployment or interruption of a user's game.
