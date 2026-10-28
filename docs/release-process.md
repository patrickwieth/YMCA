# Release publication and server deployment

## Public release notes

English, concise, player-facing changes only. Use Features / Balance / Bug Fixes and omit empty
categories. Do not include test counts, implementation details or an account of the development
work. Put engineering validation in development documentation instead.

## Automatic pipeline

Release tags must be created on the current release line (`tournament-bot`), not on the older
master gameplay baseline. The tag-push workflow executes:

1. Build and upload Windows, Linux and macOS packages.
2. After ALL three jobs succeed, download/check assets, calculate checksums, and publish
   `update/stable.yaml` on `tournament-bot`.
3. Only after successful manifest publication, verify that the remote stable manifest still names
   this tag and that its GitHub release is neither a draft nor a prerelease.
4. Invoke the existing `./deploy-ymca-server.sh` via the configured SSH action.

Failed or skipped prerequisites prevent automatic deployment. Draft/prerelease releases are
rejected by the stable publication step. A release pipeline concurrency group prevents overlapping
build/publication/deployment runs. Automatic and manual deployments also share a deployment lock.
This is an ordering change, not a rewrite of the remote deployment script or its match/restart
behavior; no FORCE_DEPLOY override is added.

The separate Deploy workflow is manual-only (`workflow_dispatch`) and requires explicit confirmation.
Editing descriptions of future releases using these workflow files no longer triggers deployment.
Old tags retain their historical workflow definitions, so check their workflow version before
editing old releases. The existing v0.96.21 tag/packages are not rewritten by this change.

Do not trigger a new release/deployment merely to test this wiring. Validate the YAML with actionlint
and check the needs chain. Confirm the actual deployment job result after the next authorized tag.
After the GitHub workflow completes, distinguish workflow success from live game-version/health
verification if those have not been checked separately.
