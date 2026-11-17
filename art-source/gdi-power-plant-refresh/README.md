# GDI power plants — direct canonical bindings installed

Both supplied 256x256 originals are now bound directly, with no color-copy override:

| Actor | Image | Authoritative filename | Bottom offset |
| --- | --- | --- | --- |
| NUKE | nuke.gdi.png | bits/gdi/buildings/power plant.png | -2,5 |
| NUK2 | nuk2.gdi.png | bits/gdi/buildings/advanced power plant.png | 12,-3 |

Source SHA256, unchanged:
- Power Plant: `bddc2cd6ec016eb637fe31a4509f27d13d726f444e58c8994654754fed89b2d8`
- Advanced Power Plant: `ddd8eb4eea527e16d0e059852eddfbe3946601c3466bc20e910e4ea0ed25717d`

[Native placement preview](native-preview.png), constructed from engine-resolved
registration, not a live screenshot. Three body states bind the supplied original
at Scale1. Existing icons/construction effects remain inherited.

The earlier color override layer shadowed the replacement power-plant source. It is
now removed from the manifest for ALL supplied GDI buildings. Color derivatives
are previews only. The generator cannot recreate active Filename redirects. The
approved Ion Defense pixels were explicitly promoted unchanged to the same canonical
root folder. No supplied PNG has been overwritten.

New visual calibration is recorded in `calibration.json` and
`tools/gdi-building-alignment.json`. Both use centered visual ground envelopes and
separate native 3x3 decorative bibs. Original 2x3 building dimensions, occupied
cells, hit shapes, power100/200, costs300/500, health, prerequisites, production and
other gameplay traits remain unchanged. Selection changes are decoration-only.
The Nod variants retain their own images/bibs and original selection geometry;
NUK2 inheritance keys were named without changing order/values to permit overlays.
Its redundant Nod legacy-bib removal was dropped because the parent now removes it.

`before/` contains resolved rules captured before this refresh. Tests compare gameplay
against those snapshots, verify both original hashes and actual resolved filenames,
and ensure no GDI bib/selection leaks into Nod. Source/color/alignment/Guard tests pass.
Fresh engine utility checks pass for 14 GDI buildings, Guard Tower and Ion Defense;
actual PNG loaders read each new plant as one native 256x256 frame.

The solution compiled but output copying was blocked by the running game's CA DLL.
The new utility assembly was tested using workspace `.gdi-authoritative-utility/bin`;
the game was not stopped/restarted. No claim of live appearance verification. The
running game may retain cached old art until the user next starts it normally.
No new commit, push, release, deployment or original test-map modification.
