# GUI release automation implementation plan

**Goal:** merge the approved GUI into dev, push it, and publish tested Windows installers automatically.
**Architecture:** keep the existing CLI workflow; a dedicated Windows workflow tests the GUI, calls the existing portable publisher, compiles Inno Setup, calculates hashes, and attaches assets with the built-in GitHub token.
**Stack:** .NET 10, PowerShell, Inno Setup 6, GitHub Actions/CLI.
**Spec:** user instruction on 2026-10-10: merge mainline, push GitHub, configure automatic Release installers.

## Constraints

- dev is the remote default/mainline branch. Fast-forward only; no force pushes.
- Releases use gui-vMAJOR.MINOR.PATCH tags; manual published GUI releases also build assets.
- Test Category!=Live before publishing. No external secret required.
- Installer is per-user, x64, includes runtime and shortcuts; portable ZIP remains available.
- Preserve user config under LocalAppData on upgrade/uninstall.

## Task 1: Merge and release

- [x] Fetch origin and fast-forward dev to the reviewed GUI branch.
- [x] Add scripts/windows-gui.iss and Build-WindowsGuiInstaller.ps1 for setup EXE plus SHA256SUMS.
- [x] Add .github/workflows/windows-gui-release.yml with GUI tag, published-release and manual triggers.
- [x] Update WINDOWS-GUI.md with exact tag commands and assets.
- [x] Run Release GUI tests (30/30); compile installer; verify package contents and installer metadata. Independent release review: no blockers.
- [x] Commit/push dev, create gui-v1.0.0, monitor Actions until release assets are available.

Verified: Actions run 38009549632 succeeded for commit 6d55668. Published gui-v1.0.0
contains setup EXE (34,201,806 bytes), portable ZIP (45,834,926 bytes), and SHA256SUMS.txt.
The GUI Release workflow is active; no custom GitHub secrets were added.
