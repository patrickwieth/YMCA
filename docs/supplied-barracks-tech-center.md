# Supplied Soviet Barracks and Allied Tech Center

Local Game changes only. No release, push or deployment; supplied PNG bytes preserved.

## Soviet Barracks

`bits/soviet/buildings/barracks.png` is directly referenced by both resolved body
states. The supplied 256x256 source is unscaled. Its additional transparent bottom
margin is compensated by the final `26,12` sequence offset (formerly `26,-31`).
Original and supplied images are archived under `art-source/soviet-barracks/`.

At the user's explicit request, the resolved site is now **2x2**, offset `0,0`,
center `0,0,0`, with footprint `xx ==`: the lower-left/front row, cells `(0,1)` and
`(1,1)`, is passable. Both existing infantry exits already use those cells and are
retained. The inherited foundation is `sovietbib2x2`; the later override that had
removed the Soviet bib is removed. The rectangular hit shape covers the occupied
row only; selection bounds match the 2x2 site. The existing dedicated shadow is
retained on BARR, not moved to any neighboring rule.

Price, HP, power, build prerequisites and production remain unchanged. The change
to occupied cells/collision is intentional; unrelated actor rules are unchanged.

The deferred portrait is now implemented as `bits/faction-icons/barr-soviet.png`,
directly derived from the supplied image, with crop `(29,70,229,220)` and OsShp
Barracks caption. It is no longer allowed as a legacy icon in the Soviet gallery.

## Allied Tech Center

`bits/allies/buildings/tech center.png` is directly referenced by all six body
states. Source dimensions remain 256x256. The new bottom margin is 12px smaller;
all six sequence offsets change from `0,32` to `0,20`, preserving the prior visible
ground line without changing scale, bib, footprint or gameplay.

The cameo uses crop `(16,35,248,209)`, showing the supplied blue dome and facade.
Only the Allied Tech Center icon changes. Earlier approved icon hashes remain
frozen, with this authorized replacement recorded separately in
`art-source/allied-tech-center/icon-replacement.json`.

## Checks

Both buildings and their menu portraits were inspected in native runtime at 1.25
UI scale. Evidence is in the two art-source directories, including screenshots,
resolved rule/sequence snapshots and menu probe results. Tests cover original
bytes, unrelated icons, all body-state paths/offsets, Barracks footprint/bib/exit
cells, preserved economics and unrelated actor rules. Passability checks use the
resolved footprint, not a claim of an additional live unit-traversal test.

The gallery preparer also keeps new actor aliases inside the Rules block when a
source map places Actors afterwards, and creates World/Player overrides when
absent. Its source still needs to be a controlled menu fixture: the all-faction
building-display map exposes unrelated legacy upgrade icons and is not suitable
for strict family-wide menu acceptance. Final menu checks use the established
Soviet and Allied menu fixtures. Original map packages are untouched.
