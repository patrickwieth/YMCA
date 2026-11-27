# Urban Warfare — map concept and asset plan

Planning only. The user requested a dedicated map combining a city, adjacent
villages and looser settlements at the outer edges. The artwork collection is now
supplied in `mods/ca/bits/misc/buildings/`; initial visual categorization is recorded
in `urban-art-inventory.md`. Cutouts/registration are pending; no urban map or new
garrison gameplay is installed. Do not retrofit the accepted existing map presets.

## Winter setting and supplied menu preview

User requests a snow-map treatment: ordinary open ground should primarily be snow,
not a requirement to cover every surface in snow/ice. Roads, courtyards and bridges
can remain exposed; retain source-art snow patches. Do not freeze the river into
an unintended walkable crossing.

The inspected installed Rubberduck art and external
`E:/SharedShitzens/src/openRA/tilesets/rubberduck/grassland` contain sand/grass/dirt
but no dedicated snow-named ground set was found. The external ruin folder includes
frozen ruin-floor sheets, not established as suitable natural snow ground. A
**source-preserving color-shift derivative of sand** is explicitly suggested by the
user and has now been tested separately for terrain only in
`art-source/urban-warfare/road-core/snow-from-sand.png`. Texture detail and alpha are
preserved. It is not installed in a runtime tileset; the building/vehicle
cut-and-paste-only restriction still applies.

Supplied preview: `E:/SharedShitzens/src/openRA/retrodiffusion/Map Preview/`
`game_map_like_treasu...-690451201-0.png` (256x256, “The City Search”). Byte-identical
copy: `art-source/urban-warfare/sources/map-preview.png`, SHA256
`ed8e29d4e22588d6f918623fc388078ae6b465ea3ba072e39be950d68c522c8f`.
Reserved as menu-preview source, not yet registered in the chooser. The illustrated
river/roads are not authoritative map geometry: the agreed urban river crossings,
outer starts and absence of edge crossings take precedence.

## Spatial concept

Use connected districts, not an arbitrary scatter of civilian buildings:

- **City:** dense blocks, multi-storey buildings, shops/workshops, courtyards, a few
  squares and parking areas. Several broad through-streets plus secondary routes.
- **Adjacent villages:** recognizable small centers with lower buildings, a local
  crossroads or square, houses, barns/yards and gardens. Separate clusters linked
  to the city, rather than a continuous wall of buildings.
- **Outer settlements:** detached houses, small businesses, lots, sheds, gardens
  and ample open space near the player deployment areas.
- **Transitions:** fringe workshops, vacant lots, vegetation and connecting roads;
  gradual changes in building size, spacing and street layout.

Population density is initially a **layout/art parameter**, not a simulated civilian
population or an economy mechanic. Each district has a coherent density profile
controlling building spacing, building types, street pattern, courtyard/open-space
share and parked-vehicle density. Density must not override navigability.

Recommendation, not a settled generator specification: establish one deliberate
reference layout and test its urban mechanics first. A procedural Urban Warfare
preset can reuse the same asset catalog/district rules later if desired. Player
count, final dimensions and handcrafted-versus-generated delivery are not yet fixed.

## River and team separation — confirmed user requirements

- Players start in the outer area, with the two teams on opposite sides of a river.
- The river runs through the city and can be crossed at several locations there.
- **No crossing at the map edges between the teams.** No peripheral bridge, ford,
  walkable river end or accidental land connection may bypass the urban crossings.
- The river must maintain the land-route separation all the way to the playable
  boundaries. Side routes along each team's own bank are allowed; they must not
  become an outer cross-river shortcut.
- Validate land connectivity on the actual exported map, including shores and
  bridge approaches. Multiple urban crossings should offer distinct attack routes,
  rather than converge immediately into a single unavoidable choke point.
- Crossing count, exact bridge locations/widths and destructibility are still design
  choices. Aircraft/amphibious/naval access is not restricted by this land-layout
  requirement; any special unit restrictions would need a separate decision.

## Starting resources — confirmed user requirement

- Only a small amount of Tiberium at the outer edges near **each individual player
  start**, not one shared starting field per team.
- Keep those initial deposits comparable in accessible amount and harvester travel
  distance, with clear harvesting routes and enough space for the starting base.
- Exact quantities, regrowth/exhaustion rules and any additional resources in the
  city or elsewhere are not decided. Do not infer rich central deposits or a fully
  resource-free city from the limited starting-resource requirement.

## Gameplay layout requirements

- Spacious, reasonably comparable start areas at the outer edges; no unavoidable
  enemy garrison firing directly into starting construction space.
- A connected main road network with several approaches to the urban river
  crossings and lateral routes on each bank, but no cross-team edge bypass.
  Do not make one intersection or one occupied house decide the entire match.
- Side streets/courtyards offer infantry routes; main corridors must handle tanks,
  harvesters and grouped units without decoration-induced jams.
- Comparable access to early resources and neutral occupation opportunities.
  Building density is not a substitute for resource/access balance.
- Deliberately reserve entrances, unloading cells and supply routes before placing
  buildings, fences, props or cars. Revalidate after decoration is applied.
- Civilian footprints provide movement obstacles. Actual projectile obstruction,
  firing lines and visibility/occlusion need explicit policy and native checks;
  opaque building pixels alone do not establish working combat cover.

## Required visual kit

### Buildings

- City: apartment blocks, corner buildings, shops, offices, workshops/warehouses.
- Villages: low houses, a few local landmarks, farm/outbuilding variants.
- Settlements: detached houses, small garages/sheds and light commercial buildings.
- Enough silhouette/roof/material variety to avoid obvious repetition. Do not
  synthesize missing facades by arbitrary mirrored lighting or rescale supplied
  buildings silently to manufacture variants.

### Roads and ground

- Main streets and narrower residential/village roads.
- Straight runs in both isometric axes, corners, T-junctions and crossroads.
- Road-width transitions, dead ends and driveways.
- Urban river banks/quays, bridge spans and approaches matching the street kit;
  multiple crossings inside the city, none between teams at the outer edges.
- Sidewalks/curbs, parking bays/lots, courtyard/plaza surfaces and transitions to
  dirt/grass. Optional markings/crosswalks only if they match the supplied style.
- Road art and logical terrain are separate: no automatic movement-speed bonus
  or buildability change merely because asphalt was drawn.

### Vehicles and props

- Parked civilian cars, vans, delivery trucks, optionally buses, plus restrained
  wreck variants if artwork is supplied.
- Static vehicles first; no civilian traffic/pathfinding simulation is implied.
- Place mainly in parking bays, roadside pockets, yards and squares, not on mandatory
  vehicle routes. Decide decorative/destructible/crushable behavior explicitly;
  do not turn every parked car into a blocker or explosive trap by default.
- Optional fences, walls/gates, lamps, dumpsters, benches and vegetation. These
  complement the road/building kit; they do not excuse missing core connections.

## Required modular assembly and art method

User requirement: the kit must support checkerboard/grid street networks with
independently placed buildings inside the resulting blocks. Deliver reusable,
compatible street segments and clean building cutouts, not fixed scene patches.
Validate multiple adjacent blocks and repeated road joins before treating the kit
as usable. Grid refers to the isometric game axes, not screen-horizontal roads.

**Cut-and-paste from supplied sources only; no newly generated artwork.** Do not
invent missing facades, procedurally synthesize textures or inpaint absent details.
Use suitable existing source pieces or report the precise asset gap: the user can
generate more artwork. See `urban-art-inventory.md` for the extraction constraints.

## Source handling / cutout workflow

When the collection is provided, inventory the actual images first. Identify all
usable standalone buildings, road surfaces/segments, vehicles and props, and list
missing categories instead of inventing extraction success for absent assets.
Preserve originals and source provenance. Produce separate transparent cutouts,
retain intentional shading and material detail, and flag baked-in terrain/shadows
that cannot be cleanly separated. Match native pixel density and perspective.

Catalog each asset with source file/crop, alpha bounds, ground anchor, render
placement, footprint, collision/decorative role and district suitability. For
occupiable buildings also record entrances, exits, window/shot ports, capacity and
flag anchor. Vehicle directions must be coherent with their source lighting. Road
pieces need seamless native-grid joins; a cropped road texture is not automatically
a finished tiling road set. Review in the actual game, not just on a contact sheet.

## Existing pieces found during inventory

- `mods/ca/rules/civilian.yaml` contains classic civilian buildings, including V01–V37
  and regional houses/huts. They are behavior/layout references, **not** a verified
  art match for the new isometric collection.
- `mods/ca/rules/vehicles.yaml` includes TRUK (Supply Truck). Military bus/scrap-car
  assets also exist, but are not a ready neutral parked-car catalog.
- Classic tilesets define Road terrain. The inspected Rubberduck tileset/art paths
  did not reveal a complete ready-to-use urban road kit. Treat that as required
  artwork/integration work, not as already available streets.
- Garrison/ownership/firing/flag building blocks exist; see `urban-garrisons.md`.

## Garrison integration

Ordinary civilian buildings begin neutral. First occupation temporarily assigns
ownership and a player-color flag; infantry fires through configured ports. Last
exit restores Neutral and removes the flag. An optional small bonus is still a
proposal and must remain weaker than the dedicated bunker treatment. Neutral tech
buildings with permanent capture/reward behavior must remain a separate category.
Test allied/enemy access, occupant loss, ejection/destruction, blocked exits,
ownership restoration and all selected infantry weapon types.

## Phasing

1. Source collection received and visually categorized (`urban-art-inventory.md`);
   refine individual-object inventory and kit gaps during extraction.
2. Source-only extraction is underway for houses, a coherent city-building group,
   outbuildings, wells, a handcart and parked vehicles. Cuts and bounded same-roof
   repair reviewed against their sources; see `urban-art-inventory.md`.
3. A native-density **image assembly** now checks independent placement and road-core
   connectivity across several blocks. This is not an engine render. Continue with
   source-cut curbs/sidewalks, markings, transitions and more city buildings, then
   validate actual engine scale, footprints, entrances, flags and firing positions.
4. Compose the complete city/village/settlement layout.
5. Native movement/combat/garrison checks and visual review across all districts.

Separate unfinished task: existing stationary bunker fire-port alignment has been
started but is not yet implemented; see `bunker-fire-ports.md`.
