# Duplicate ISelectableInfo crash in the isometric building test

Scope: Game / modular only, not production master/release.

Mouseover called ActorInfo.SelectionPriority, which uses TraitInfoOrDefault<ISelectableInfo>.
Several concrete buildings inherited IsometricSelectable while retaining legacy Selectable
customizations. Both implement ISelectableInfo, so the unique TypeDictionary lookup threw.

The resolved default-rule audit found 11 affected actors:
- hosp, miss, oilb
- nkorea_bunker
- chbunker, chbunker.advinf, chbunker.ai
- chbunker2, chbunker2.advinf
- chpropagandatower, chpropagandatower.pdl

Of these, nkorea_bunker is placed on the source Building Grid Test map. The test map itself has
no Selectable/IsometricSelectable overrides. This was not a missing Prism Tower PNG.

## Correction

Change the six legacy Selectable customizations in civilian.yaml and china/structures.yaml to
IsometricSelectable. This merges with the inherited isometric trait instead of adding a second
selection provider. Preserve explicit priorities, selection classes and the speaker tower's
numeric custom Bounds values. Visual tuning of those custom isometric bounds is separate.

Update oilb.indestructible's removal to IsometricSelectable and also remove its dependent
IsometricSelectionDecorations, retaining its explicit Interactable/unselectable intent.
No engine lookup is weakened to silently choose one of multiple traits.

## Regression checks

`--check-selection-traits` checks every resolved concrete actor for at most one ISelectableInfo,
and invokes the actual SelectionPriority lookup for each valid actor. It also checks neutral
building priorities, bunker selection classes and the unselectable oil derrick's trait
construction dependencies. It permits zero selection traits on intentionally unselectable actors.

Build and checks pass after the rule changes; --check-allied-building-art still passes.
The test-game.cmd launcher now runs the selection check before preparing/launching the map,
so future default-rule duplicates fail before mouseover crashes. Arbitrary third-party map
rule overrides are not covered by this default-rule check. Live hover testing remains necessary.

Run `test-gebaeude.cmd --check-only` to exercise the real Windows launcher, build, rule check
and temporary-map preparation without opening a game. The launcher resolves dotnet.exe explicitly:
the project-root dotnet.cmd points to a possibly missing .dotnet/dotnet.exe and otherwise shadows
the installed runtime when CMD is started from Game. A missing local SDK must not break preflight.
