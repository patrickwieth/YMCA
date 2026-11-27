# Nod foundations missing from the building test map

Root cause: changing the locked Multi0 player from Black Hand to England exposed
missing actor-level Faction initializers in the original Nod display block.
`GrantConditionOnFaction@NODBIB` uses FactionInit, falling back to the owner faction.
The condition `nod-custom-bib` was consequently absent, hiding the conditional
Nod foundations even though all their rules and PNGs still existed. The earlier
claim that existing bib rules proved their visibility was incorrect.

`prepare-test-game-map.ps1` now validates the named Nod display slots and supplies
`Faction: blackh` where no explicit faction exists. Ownership, locations and the
England player remain unchanged. The original `testmap1.oramap` is immutable.
Existing explicit actor factions are preserved. No global Nod bib conditions,
artwork, gameplay or protected construction-yard PNG were edited.

The test-map faction regression verifies the surviving Nod slots, ownership and
source-map hash. Native before/after runs reproduced missing foundations without
FactionInit and restored them with the correction. Cropped evidence is stored at
`art-source/nod-bib-testmap/`. Some foundations were unconditional and had never
vanished; the fix restores the conditionally hidden ones.

Start the building test map again through its launcher to regenerate the disposable
package. Existing running games are not stopped or modified. No release/deployment.
