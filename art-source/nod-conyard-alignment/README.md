# Nod Conyard — alignment only

User requested aligning the supplied Nod conyard with its bib. The authoritative
`mods/ca/bits/nod/buildings/conyard.png` is **not edited or regenerated**.
`alignment.json` records its current SHA256; the regression test enforces identity.

Native image400×256, bbox45,50–399,255; existing Nod3×3 bib384×199. The original
Bottom anchor/offset0,0 left the supplied worksite/ground diamond displaced toward
the right and front. Both `nodfact.custom` idle/damaged-idle now use Bottom anchor
and offset−22,−10. This brings the visible ground corners back inside the bib with
balanced front/side margins. Scale,3×3 footprint, center, selection and production
remain unchanged; actor placement preview uses the same aligned sprite.

`before-after.png` is a composed native-pixel layout check, visually reviewed, not
a live screenshot. Original artwork/earlier assets are preserved. Tests verify
actual resolved filename/anchor/offset and protected-source SHA256.

The same follow-up requested disabling Nod Gun Turret construction: it already
had `~disabled` in its resolved prerequisites. This remains enforced and tested;
`Actor15: gun.nod` stays on the building test map. LTUR remains buildable.

No running game stopped. Restart the building test to load changed rules.
