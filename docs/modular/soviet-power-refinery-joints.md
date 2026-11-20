# Soviet Tesla Reactor and refinery joint colors

Local only, not committed/pushed/released.

TPWR inherits APWR and Soviet_Refinery inherits PROC. Both resolved to the Allied
WithBibRemap fields (`ImagePrefix: alliedbib`, `Palette: alliedbibplayer`) despite
rendering Soviet foundations. An empty WithBibRemap inherited from the Soviet bib
template does not remove those explicit parent fields. Thus no Soviet mask matched.

Both actors now explicitly select `sovietbib` / `bibplayer`. No PNG, concrete fill,
footprint, docking, placement or gameplay property was changed. Ordinary Soviet
power and nuclear power already used the correct defaults and remain unchanged.

`--check-bib-connections` now explicitly covers TPWR, Soviet refinery and advanced
power, and rejects any concrete actor with Soviet bib overlays but a foreign remap.
The expanded check passes across the loaded faction definitions. Native screenshots
of both affected actors visibly show blue joints with neutral concrete, consistent
with neighboring Soviet buildings. Evidence: `art-source/soviet-power-refinery-joints/`.

The normal build compiled the updated diagnostic but failed to copy its DLL because
the user's running OpenRA held it open. Validation used an isolated copy of engine
binaries plus the newly compiled CA DLL; neither the user's game nor servers were
stopped. The actual fix is YAML-only and loads on the next game/test restart.
Protected Nod conyard SHA and git whitespace checks pass.
