# Master integration into modular

Merged published origin/master a5559b160b7bb2064933dc9108c5e2c5e73a3b0f into modular
following the isometric/Mono compatibility merge314f95bd. This brings the Gene
Splicer ordering/ownership fixes, invalid mind-control target protection, corrected
starting infantry and the post-build stable-manifest/deployment workflow.

Conflict resolution:
- CI: preserve modular's engine-patch preparation, Modular.Tests and tournament test
  builds/error propagation; do not restore the older --no-build invocation.
- Packaging: retain master's draft/prerelease safeguards and deployment dependency
  on successful stable-manifest publication. No workflow was triggered.
- Soviet starts: use fixed conscript entries but retain modular's mcv.soviet.
  GDI starting-unit correction merged automatically.

Preservation:
- Only overlapping local file was defaults.yaml's one construction-sequence line.
  Temporarily stashed that path alone and restored the line as an unstaged change.
- All other pre-existing modified regular files checked byte-identical against a
  pre-merge SHA256 manifest, including protected Nod conyard artwork.
- All10423 pre-existing untracked paths remain present. No local building-art work
  was added to the merge commit. No running game or Blender was stopped.
- Unpublished Battlefield News on the separate local Game-master branch remains
  separate; it is not part of the published origin/master being merged here.

Validation:
- Full .NET6 working-copy build passed in isolated output.
- netstandard2.1/Mono API-target build passed in the isolated compatibility fixture
  described in isometric-merge-mono-ci.md (not native Linux/Mono execution).
-311 Modular.Tests passed.
- Engine checks passed: Gene Splicer, mind-control targets, all faction starting-unit
  classes, rotor/smudge registration and four-barrel Allied flak.
- Git staged whitespace/conflict check passed.

Local merge only. No push, tag, release, deployment or remote CI run.
