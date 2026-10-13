# Cannon portraits and approved caliber consolidation

Only the user's supplied item PNGs are used. Do not create new item artwork or placeholder
pictures. `generate_cannon_art.py` only packs existing pixels (plus quarter-turn copies) into
a runtime texture atlas and emits bindings/chrome regions. It no longer creates review images.
The previous `cannon-art-review.png` is historical and its assignment labels are obsolete.

Nine existing pictures cover **21 chassis families / 22 weapon choices**. They appear in the
grid, held-item preview and weapon picker. Twin cannon packages draw two barrels without
adding components or firing channels. Original PNGs are never overwritten.

## Faction direction for heavy artillery calibers

Prefer **152mm for China and Soviet vehicles**, and **155mm for NATO-oriented vehicles**.
This is a design direction for this caliber family, not a replacement for all tank calibers or
an automatic admission of new interchangeable weapons. **Nona SVK is explicitly a 75mm Short
Cannon**, not heavy 152mm artillery. It currently uses the existing `75mm.png` unchanged until
the user supplies dedicated short artwork. Its legacy `155mm.Nona` rule remains unchanged.
Akatsiya, ISU and Tsar already follow the 152mm direction. Allied artillery and Tank Destroyer
retain 155mm. Future Chinese artillery assignments should follow the 152mm direction where
appropriate; nuclear/incendiary package details still require their own review.
Nod is not automatically classified as NATO; its existing assignments remain pending a separate
faction decision. No damage, firing-rate or range changes follow from these designations.

## Approved consolidation

These are deliberate designer caliber designations, not changes to combat statistics or claims
that internal rule names describe the new caliber. Preserve native rule IDs and saved component
identities, including conditional upgrades, secondary weapons and frozen compiled maps.

| Vehicle | Legacy native rule | New designation / supplied PNG |
| --- | --- | --- |
| Scout Tank | `25mm` | 75mm / `75mm.png` |
| Nona SVK | `155mm.Nona` | 75mm Short Cannon / existing `75mm.png`, dedicated short PNG pending |
| Nod Light Tank, T-34, Devil Tank | `30mm` | 75mm / `75mm.png` |
| Challenger | `90mm` | 88mm / `88mm.png` |
| Tsar Tank | `150mm.Tsar` | 152mm / `152mm short.png`; Tesla channel retained |
| Tank Destroyer | `183mm` | 155mm / `155mm artillery.png` |

**Future class: 30mm Autocannon.** This will be a distinct, rapidly firing weapon class, not a
rename of today's slow-firing legacy `30mm` cannon. Do not admit it, assign firing statistics,
reuse the existing slow-fire package as an autocannon, or fabricate artwork now. Its component,
compatibility, balancing and PNG are future work.

## Retained assignments

| Picture | Existing vehicles |
| --- | --- |
| `105mm.png` | GDI Battle Tank standard; approved designer designation, stock source ID `120mm`. |
| `120mm.png` | GDI's already-approved +20% alternative; Titan (`TitanGun`, approved 120mm designation); Mirage (`120mmHEAT`). |
| `125mm.png` | Heavy Tank and People's Tank, both twin-barrel packages. |
| `130mm.png` | Mammoth (`130mmTD`) and Apocalypse (`130mm`), both twin barrels plus original missiles. |
| `152mm artillery.png` | Akatsiya (`152mm.Akatsiya`) and ISU (`152mm`). |
| `155mm artillery.png` | Nod/Allied Artillery, Howitzer and Specter, plus the reclassified Tank Destroyer. |

The original actor preview and in-game sprites/voxels remain unchanged. Only the inventory
schematic and contextual component labels change. Damage, range, burst, reload, prerequisites,
spatial footprints and existing conditional upgrades remain unchanged.

Shared calculator IDs cannot determine art alone: `medium-cannon` covers GDI, Challenger and
Heavy Tank, while `original-armament` covers many unrelated vehicles. The registry matches
**chassis + selected weapon**. Contextual names do not rename saved IDs or broad Excel data.

## Still open / reserved

Battlemaster, Overlord, Juggernaut, Inferno Cannon and Nuke Cannon have no explicit approved
numeric caliber yet. Their existing schematics remain until a deliberate designation is made.
Mammoth Mk. II uses railguns and missiles, not an ordinary shell cannon. No new fallback images
are generated for these vehicles.

The other 11 supplied PNGs remain in reserve, not selectable new weapons: `115mm`, `140mm`,
`105mm artillery`, `120mm artillery`, `122mm artillery`, `122mm short artillery`,
`152mm short artillery`, `120mm burst`, `125mm burst`, `120mm long` and `130mm recoil`.
The approved Tsar assignment is the specific exception for `152mm short.png`.
New engine, generator and infantry PNGs are outside this cannon pass.

## Repacking and validation

Edit `tools/modular/cannon-art-bindings.json`, then run:

```
python tools/modular/generate_cannon_art.py
```

This updates the C# presentation registry, the marked chrome collection and the runtime
`cannon-icons.png` atlas. The nine source pictures and rotated copies are packed unscaled in a
**1024×2048 power-of-two texture**; this is technical packing, not new item artwork.
Commit sources and generated outputs together; restart OpenRA after updating the atlas.

Native tests cover exact assignments, contextual labels, separation from the future autocannon
and unchanged profiles/rules/weapon options. Python tests compare source pixels, rotations,
chrome regions and reserved variants. The preview utility checks all 22 registry entries against
resolved original actors/armaments, including twin muzzle offsets. Live graphics still require
interactive review.
