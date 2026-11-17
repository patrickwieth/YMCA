# Isometric merge and Mono CI compatibility

Requested integration: origin/isometric at473576b39e6eb3dc81d9f2123b45bf2807f45991
into modular (previous HEAD4e091337). Four incoming commits: CI preparation/build
failure propagation, hidden Play menu, Tiberium carpets/trees and hand-edited motifs.
There were no incoming paths overlapping the existing local modifications. Merge
was conflict-free; local artwork/placement changes were not staged into this merge.
Protected Nod conyard PNG was neither edited nor staged. No push/release/deployment.
SSH fetch lacked a usable key; fetched the same repository over HTTPS without
changing the configured remote or credentials.

## Failure

GitHub Actions43683331311 failed only Linux (mono); Linux/Windows .NET6 passed.
Initial errors: absent System.Text.Json and IsExternalInit in netstandard2.1.
After resolving those, a clean API-target compilation exposed additional .NET6-only
calls: SHA256.HashData, Convert.ToHexString, OperatingSystem.IsWindows,
Enum.GetValues<T>, File.Move(overwrite), PriorityQueue and newer FirstOrDefault overloads.
Fixing only the initial errors would leave the next Mono build failing.

## Fix

- Conditional IsExternalInit metadata marker for netstandard2.1 only.
- Geometry export uses existing Newtonsoft.Json; parsed JSON compared equal to the
  previous exporter against the same live rules.
- RuntimeCompat uses portable hashing, uppercase hex, platform/enum APIs and atomic
  File.Replace/File.Move, preserving old files if replacement fails.
- Portable quaternary priority queue preserves .NET6 equal-priority ordering and is
  used on both runtimes; mixed-runtime path decisions must not diverge. Randomized
  enqueue/dequeue regression compares every element and priority to the .NET queue.
- Compatible LINQ equivalents preserve explicit -1 defaults.
- Modular regression tests now also run in both .NET CI jobs. Mono job retained.

## Validation

- Clean merge-index CA sources plus the compatibility fix compiled successfully
  against netstandard2.1 references with the engine's Mono-conditioned dependencies.
  This was an isolated source copy: temporary project files selected Mono property
  branches while dotnet SDK provided the compiler. No installed Mono/WSL runtime
  was available; this is NOT a native Linux/Mono job execution.
- Repeated netstandard2.1 compilation including all pending local CA source changes:
  successful as well.
- Full working .NET6 build successful in isolated output (running game untouched).
-311 Modular.Tests passed, including hash/hex, heap tie ordering, file replacement,
  enum/platform and existing faction/designer tests.
- Incoming test-ci-build-guards.ts passed: both Makefile loops propagate failures.
- Existing rotor/smudge, Allied flak, Sentry, GDI AA, Ion Defense and Nod Laser engine
  checks passed after merging; existing parsed selection JSON is unchanged.
- Existing NU1901 NuGet.CommandLine and unrelated compiler warnings remain.

The historical GitHub run remains failed until a future authorized push and new run;
no remote success is claimed. Uncommitted building-art work remains separate.
