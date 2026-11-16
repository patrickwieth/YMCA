# Building feedback follow-up — Game/isometric, local only

## Approved Soviet nuclear-plant balance

APWR.Soviet now costs1500 (previously500) and produces800 power at full health
(previously200). ScalePowerWithHealth remains. Destruction triggers exactly ONE
`CHNukeCannon.Impact` at CenterPosition, with the same damage/effects as the Chinese
Nuke Cannon impact. Both loaded/empty death weapon references use that weapon.
The inherited Footprint explosion mode was explicitly replaced so a4x4 plant does
not generate16 nuclear impacts. Normal selling still follows the sell/dispose path.
Allied APWR, GDI reactors and Russian TPWR/Tesla Reactor are unchanged.

BLIMP and promotion.Airship both require the HIDDEN prerequisite `~mode.operational`,
so the aircraft and commander-tree entry no longer remain visible-but-disabled in
Tactical mode. Existing faction, technology and promotion requirements remain.

## Static building presentation, not frozen gameplay

Basic buildings opt into RenderSprites.SuppressBuildingAnimations and
WithSpriteBody.UseIdleFrameForRendering. Animation.RenderSequenceOverride displays
frame zero of the idle/damaged idle art while the original current sequence, tick,
completion callbacks and make/sell/deploy conditions continue normally. Charge and
resource-state bodies use the static idle art when available; their controllers
still tick. Facing functions are retained, including embedded turret bodies.

Old production/door/delivery and animated cosmetic overlays are not rendered; static
parts, bibs and the supplied radar remain. Old building death sprite animations are
suppressed. Combat explosions (including the newly requested reactor explosion),
weapon muzzles, turret/barrel motion and status feedback remain. Preview rendering
and bounds use the same presentation path. Defaults are unchanged for other mods
and for units that do not opt into the building policy.

The checker compares actual forward/reverse construction and production completion
with/without static presentation: callbacks occur on the same ticks. It also checks
that the Obelisk remains visible using its idle art while its charge animation
controller is active, and that Gatling muzzle flashes are not suppressed.

## Placement markers and vehicle selection

The60x30 valid/invalid placement sprites now use Scale2 (120x60, inset slightly from
128x64 tiles), with ZOffset4096. FootprintPlaceBuildingPreview previously ignored
sequence Scale and ZOffset; it now honors both, retaining its old-511 bias when no
override is specified. Positive sequence depth moves those footprint categories
above BOTH actor-preview and sequence-preview sprites. Bibs therefore cannot hide
them. Unrelated support-power targeting markers keep their original scale.

The shared ground-vehicle selection prism was only10px high. It is now24px high,
including the Scrin Harvester. The2:1 ground projection was not changed. Click bounds,
collision, movement and vehicle art/scale remain unchanged.

## Grid/position and supplied platform

- All current decorative bib surface centers now use native grid anchors. Body,
  attachments and selection move together; see art-source/bib-grid-alignment/README.md.
- China bunkers' inherited0,-724 center offset was wrong for their2x2 footprint.
  Their origin is now0,0,0: body/selection/garrison32px down, mine cells unchanged.
- Chinese power plants moved4px higher relative to their stationary3x3 bibs:
  native offsets0,16 and0,28. Their2x2 occupied cells remain unchanged.
- Nod conyard moved another4px higher relative to its bib: -22,-14; decoration follows.
  The protected original PNG is untouched. Global grid translation is additional
  and shared with its bib, so it does not undo this relative adjustment.
- Supplied Gatling mount platform is cut/pasted to128px, background/ground removed,
  installed at native Scale1. Gun atlas, rotation/mount/muzzles and eight weapon
  profiles unchanged. See art-source/chinese gatling/platform-cut/README.md.

## Validation and limits

`tools/test-game.cmd --check-only` passes the actual build/selection/test-map path;
the game is not started and the authoritative map hash is unchanged. Dedicated
engine checks cover building presentation, reactor/Airship rules, marker dimensions
and ordering, actual Gatling sprite loads/projections and Scrin bib bindings.
Grid tests compare resolved non-anchor fields and cell registrations, including the
unchanged bunker mine locations. Native composed previews are not live screenshots.
Live visuals, firing, docking and production still need review after restart.

No user process was stopped, no original supplied art overwritten, no commit/push,
release or deployment. Some historical art-only baselines predate these explicitly
requested anchor/balance changes and must not be presented as an unchanged-gameplay
result for the whole batch.
