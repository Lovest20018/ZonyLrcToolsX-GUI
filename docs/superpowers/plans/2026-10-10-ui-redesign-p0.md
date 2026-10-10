# Windows GUI P0 implementation

Goal: implement the user-approved lightweight music workbench and SVG logo.
Architecture: preserve Avalonia MVVM and existing Common download services; update desktop views, state contracts, resources, and targeted tests.
Stack: Avalonia 11.2.3, CommunityToolkit.Mvvm, .NET 10.
Spec: D:/workspace/ZonyLrcToolsX/design/2026-10-10-ui-logo/UI-优化方案.md

## Global Constraints

- P0 only. Preserve source ordering, encoding, translation, overwrite protection, safe writes and cancellation behavior.
- All new text has Chinese and English resources. Keep native window chrome.
- Minimum 900×600, default 1200×800; light/dark themes; responsive toolbar and previews.
- No player, account, new provider, P1 filtering, persisted last page, or cover preview.
- Worktree: D:/workspace/ZonyLrcToolsX/ui-redesign.
- SDK: D:/workspace/ZonyLrcToolsX/.tools/dotnet/dotnet.exe.

## Task 1: Implement the approved P0 workbench

Read the spec above fully. It contains exact tokens, dimensions, interaction contracts, and acceptance criteria. Its previous-turn restriction on implementation is superseded by user approval.

1. Baseline: run existing non-Live tests and preserve baseline rendering before edits.
2. Brand/theme/navigation: App.axaml, MainWindow view/model, ThemeService as needed, Desktop.csproj. Integrate supplied logo SVG/PNG from the spec directory. Export all specified PNG/ICO sizes with reproducible script. Sidebar order and bottom navigation, distinct logs icon, native chrome.
3. Batch: LyricsDownloadPage/view model. Compact toolbar, editable-before-scan concurrency, central editable-idle/selectable-running grid, selected local preview with race protection, wide side panel and narrow Esc-close drawer, counters and retry-run denominator, stopping/empty/unrecognized states, path actions and failure export menu. Preserve safe download service.
4. Single: ManualDownloadPage/view model. Permanent labels, query snapshot/provider/line count, timestamp/text and raw LRC, save filename/location, invalidate on changed input, matching-source/stop/no-result states.
5. Album: reuse batch layout and correct all cover-specific captions/export filenames; no lyric preview or P1 cover preview.
6. Settings/log/home: approved grouping, explicit source enabled/priority labels, existing-file choices, translation-dependent merge disabled without preference loss, adjacent proxy restart notice, persistent error and unobtrusive success, logs empty state, compact home with two truthful navigation actions.
7. Localize everything new in both resx. Add focused behavior tests for changed concurrency, selection race, manual invalidation, retry/cancel states, and safety contracts. Use failing tests before state behavior implementation. Cosmetic-only edits need no mirror tests.
8. Render screenshots for all pages and representative populated states at minimum/default/large sizes in both themes and English. Inspect renders and fix layout defects.
9. Run complete non-Live suite once at end; report commands and results. Live checks are separate. Update WINDOWS-GUI.md as appropriate and commit implementation on feature/ui-redesign-p0.

Avoid unrelated restructuring. Do not spawn subagents, push, or merge. Report omitted/unverifiable native DPI checks honestly. Generated artifacts go to D:/workspace/ZonyLrcToolsX/artifacts/ui-redesign and final package will be made by controller.
