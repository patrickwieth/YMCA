# Urban Warfare — supplied source inventory

Source folder: `mods/ca/bits/misc/buildings/`. Initial visual inventory completed;
original files unchanged. These are source scenes, not already integrated standalone
actors or terrain tiles. Most images are 256x256 and contain several objects, so
individual objects have substantially fewer pixels than a standalone 256x256 asset.
Do not silently enlarge them to pretend otherwise.

## Initial classification by visible contents

| Sources (PNG filename stems) | Classification / extraction candidates |
| --- | --- |
| city_buildings-1965092624-0, city_buildings-224831740-0, city_buildings-37591774-0, city_buildings-671001408-0, city_buildings-914674145-0 | Dense city: multi-storey apartments, shops/corner buildings, roof tanks, fire escapes. Cars/vans and street furniture where unobscured. Rear buildings are often occluded; cropped facade fragments are not complete standalone buildings. |
| village_buildings-1193763147-0, village_buildings-214923985-0 | Village/loose settlement: timber houses, barns/outbuildings, wells, fences, vegetation and small yard props. Best initial candidates for relatively isolated buildings. |
| village_buildings-18637419-0, village_buildings-2100749753-0 | Rural clusters: houses, barns, carts, fencing and yard objects; windmill in 18637419. Snow patches and baked terrain need separate review before combining with other districts. |
| city_roads-1825452772-0, town_roads-879967796-0 | Urban paved intersections, sidewalks, crossings/markings, cars, vans/trucks, lamps/poles, benches and bins. Despite its name, town_roads depicts a fairly dense urban scene. |
| paved_roads-991488041-0 | Industrial/peripheral paved-road scene, military-looking trucks, fencing, barrels/crates, poles and ground patches. Do not classify armed/military vehicles as ordinary civilian cars. |
| village_roads-184354158-0 | Rural dirt-track intersection and low-density roadside buildings, wells/fences/gardens. Potential outer-settlement ground/detail references; not a finished seamless tile sheet. |
| barracks-1065902500-0, barracks-1587415456-0 | Military/industrial compound scenes: brick halls, watchtowers, fences, supplies and military vehicles. Separate category, not automatic civilian garrison or civilian traffic content. |
| infantry_wooden_barr...-1174669056-0, infantry_wooden_barr...-1653080959-0 | Wooden barracks/outbuilding and compound. First image has a relatively isolated long hall; second includes yard/vehicles and snow. Peripheral utility-building candidates, subject to scale/style review. |
| bridge_repair_hut-1686237632-0, br_ckenh_uschen-1349403680-0 | Existing bridge-related references, including a stone bridge scene. Preserve earlier supplied assets and existing runtime bindings; no automatic replacement by this inventory. |
| research core, statue of liberty | Existing special-purpose/landmark assets. Preserve and exclude from automatic generic urban cutout processing. |

## Binding art-method constraint (user clarification)

Use **cut-and-paste from the supplied artwork only**. Do not generate new artwork,
paint invented missing facades, procedurally synthesize textures or use generative
inpainting to complete objects. Source-derived cutouts/masks and rearranged source
pieces are allowed; preserve originals and record provenance. An incomplete object
is not permission to invent its hidden parts: use a complete source alternative,
retain a coherent group, or request additional source art from the user.

The required result is a reusable kit that can build **checkerboard/grid street
layouts with buildings placed independently inside the blocks**, not a set of
fixed neighborhood screenshots. Roads and buildings must be separable. Need clean
straight segments in both isometric axes, matching crossroads, T-junctions/corners,
end/edge treatments and compatible sidewalks. Check repeat seams, road widths,
sidewalk joins and native pixel density in a multi-block assembly. Buildings need
clean silhouettes, usable ground anchors and footprints that fit the block lots.
No cars, poles or building fragments accidentally baked into every repeating tile.

The user can generate additional sources. Maintain a concrete missing-piece list
with desired object, view, dimensions/pixel density and connections. Ask for missing
source pieces rather than silently manufacturing substitutes. Native-grid placement
must fit the artwork; do not distort source pieces to force incompatible joins.

## Extraction rules / next work

- Categorize each extracted object separately, even if one scene supplies buildings,
  vehicles, roads and props. Record source crop/mask, district use, ground anchor
  and completeness. Building density is assigned by placement, not just filename.
- Keep only genuinely separable silhouettes as direct cutouts. Flag overlapping,
  clipped or hidden faces as source gaps; request additional artwork when existing
  source cuts cannot supply them, rather than inventing or exporting broken faces.
- Classify parked vehicles by actual appearance and available facing. Poles, wires,
  pedestrians and adjoining shadows must not remain accidentally attached.
- Road surfaces require native-grid joins, consistent widths and clean junctions;
  scene cutouts alone do not provide all missing road pieces.
- Snow/season, pixel density and perspective must be reconciled coherently before
  mixing sources. Do not overwrite source pixels while experimenting.
- Source-only cutouts now include timber/log houses, a porch cottage, barn, wooden
  utility hall, a coherent three-building city group, wells, a handcart, passenger
  cars, a minibus and delivery truck. See `art-source/urban-warfare/cutouts/`.
- `tools/prepare_urban_source_cuts.py` consumes `source-cuts*.json`, checks pinned
  `source-hashes.json`, and records masks, crops, initial ground references and donor
  patches in `cutout-catalog.json`. A replaced/new source requires re-inventory;
  old cut coordinates must not silently be applied to another image.
- Internal light/dark reviews under `reviews/current/` drove corrections to roof
  silhouettes, foreign building fragments, fence/asphalt remnants and vehicle
  extents. The porch cottage's baked chimney smoke is removed using a bounded patch
  of shingles from the **same roof**, not painted or generated replacement pixels.
  The city group's annexes stay attached because they obscure the main building's
  lower walls; they are not falsely catalogued as three complete independent houses.
- Every visible RGBA pixel must match the source or an explicitly recorded 1:1 donor
  patch. No blending, resampling, mirroring or texture synthesis. Ground references
  are provisional art anchors, not approved engine actor centers/footprints.
- Initial-cut evidence is retained under `reviews/initial/`; the reviewed next batch
  and its assembly are archived under `reviews/batch02/`.
- A separate **road-core** test now composes straight/corner/T/cross connections
  on the 128x64 native grid by pasting an actual asphalt source crop and applying
  binary geometric masks. `tools/prepare_urban_road_core.py` also tests the allowed
  sand-to-snow color shift and arranges cutouts independently inside several blocks.
  Tile-edge snow overdraw was found in review and removed by drawing all asphalt
  overlays after the ground layer. Large buildings are given larger lots, and test
  vehicles are placed off the main streets.
- Source-cut lane dashes now cover both street axes; copied zebra-stripe pieces
  form a crossing in one orientation. A bounded exact-palette mask removes asphalt
  and pole remnants without recoloring the retained paint. `marking_stamps` records
  every placement; markings are clipped to existing road coverage, not used to
  expand roads. Reverse/other crossing orientations remain to be sourced.
- `source-cuts-batch04.json` also isolates a genuine rounded east-facing pavement
  corner, a plain paving strip, and a curb strip beyond the original drain.
  `tools/prepare_urban_sidewalk_sources.py` checks repeated straight runs in both
  native axes. Keeping the original drain-containing strip separate avoids repeated
  drains along an entire block. These source runs are not yet four-sided loops.
- This is an internal Pillow assembly, **not an engine render or finished street
  kit**. Complete curb/sidewalk joins, the remaining marking variants, material transitions, more building variety,
  collision footprints, garrison ports and native runtime registration remain
  pending. The uniform asphalt core is not presented as final urban street art.
- `tools/test_urban_source_cuts.py` checks donor provenance, source replacement
  rejection, reproducible source pixels, snow alpha preservation, connected road
  masks and source-only marking composition without changing road alpha coverage. These checks do not establish artistic acceptance, gameplay or pathfinding.
- After development checkpoint `87dd31d0` (pushed to `isometric`), extraction
  continued with two native-density sidewalk/curb source strips in the two street
  axes (`source-cuts-batch03.json`). These are isolated source fragments, not yet
  seamless edge modules. Repeated drains/details, end cuts and corner joins still
  require review before they are added to the road assembly.
- Winter setting and supplied menu-preview source are recorded in
  `urban-warfare-map.md`. The suggested sand-to-snow color-shift exception applies
  only to terrain, not to generating/repainting buildings or vehicles.
