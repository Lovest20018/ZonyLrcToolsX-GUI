# Windows GUI 精修 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan. Steps use checkbox syntax for tracking.

**Goal:** 将已确认的批量页和设置页视觉落实到 Windows 应用，并统一全应用控件细节。

**Architecture:** 保留 Avalonia MVVM 和现有下载服务。集中管理主题、状态样式及轮廓图标，按已确认样稿重排视图，必要的视图辅助逻辑只处理显示与布局。

**Tech Stack:** Avalonia 11.2.3、CommunityToolkit.Mvvm、.NET 10。

**Spec:** docs/superpowers/specs/2026-10-10-ui-refinement.md

## Global Constraints

- 按确认样稿精修现有 Avalonia Windows 应用。批量页、设置页是验收重点，其余页面沿用同一套控件与间距。
- 保留下载、取消、扫描、重试、失败导出、编辑元数据、覆盖保护、本地歌词预览、设置保存、语言切换、日志及专辑封面功能。
- 保留原生窗口标题栏、现有 Logo、默认 1200×800、最小 900×600、浅色和深色模式以及中英文。
- 不增加播放器、账号、网络请求、歌词源或假数据；样稿的示例歌曲不能进入正式应用。
- Common 下载服务、配置格式、安装位置及 GitHub Release 自动打包流程保持兼容。

## Task 1: 实施整套精修样式与视图

**Files:**
- Modify: src/ZonyLrcTools.Desktop/App.axaml：主题令牌、控件状态、细线图标。
- Create if needed: src/ZonyLrcTools.Desktop/Styles/RefinedControls.axaml：独立管理复杂 ControlTheme/模板。
- Modify: src/ZonyLrcTools.Desktop/Views/MainWindow.axaml：外壳、导航状态。
- Modify: src/ZonyLrcTools.Desktop/Views/Pages/LyricsDownloadPage.axaml and .axaml.cs：路径栏、工具行、表格、预览、底部；AlbumDownloadPage 复用。
- Modify: src/ZonyLrcTools.Desktop/Views/Pages/SettingsPage.axaml：紧凑设置行及来源表。
- Modify: src/ZonyLrcTools.Desktop/Views/Pages/HomePage.axaml、ManualDownloadPage.axaml、LogsPage.axaml、Views/Dialogs/*.axaml：同套控件与间距。
- Modify as needed: ViewModels、Infrastructure/Localization/*.resx：仅显示状态/布局和新增中英文文案。
- Verify: tests/ZonyLrcTools.Gui.Tests/WorkbenchRenderTests.cs 现有真实窗口渲染工具。
- Update: WINDOWS-GUI.md：本轮变化与复现步骤。

**Interfaces:** 保留所有当前 ViewModel 命令和绑定，数值控件继续绑定 Value、Minimum、Maximum、IsEnabled；仅允许增加显示用属性或布局辅助。Common 服务接口不变。

- [x] Step 1：先完整读取设计规范和 HTML 样稿，核对现有控件模板及绑定；保留控制器已运行的测试基线。
- [x] Step 2：实现主题令牌与细线图标；覆盖 Button、ToggleButton、NumericUpDown、DataGrid 行/单元格/表头和 ComboBox 的内部状态。必要时从本地 NuGet 中的 Fluent 模板建立小而完整的 ControlTheme，不能只改最外层属性。

令牌示例：
```xml
<SolidColorBrush x:Key="ControlBorderBrush">#D6DCE6</SolidColorBrush>
<SolidColorBrush x:Key="NavActiveBackgroundBrush">#EDF3FF</SolidColorBrush>
```

- [x] Step 3：按规范重排主外壳、批量页和设置页；统一其他页面。保留行为绑定，避免镜像业务逻辑。歌词正文可采用无边框只读 TextBox：
```xml
<TextBox Text="{Binding PreviewText}" IsReadOnly="True"
         AcceptsReturn="True" TextWrapping="Wrap"
         BorderThickness="0" Background="Transparent"
         ScrollViewer.VerticalScrollBarVisibility="Auto"/>
```
实际模板还需覆核 focus/pointerover 无输入框边框。
- [x] Step 4：若新增状态/布局行为，先添加能揭示实际回归的失败测试再实现；纯视觉修改不编写匹配 XAML 字符串的测试。运行一次完整非 Live GUI 测试，并使用已有渲染工具产生中英文、双主题、三尺寸六页和有数据状态。

运行：
```powershell
$env:ZONY_GUI_TEST_ARTIFACTS='D:/workspace/ZonyLrcToolsX/artifacts/ui-refinement/renders'
& 'D:/workspace/ZonyLrcToolsX/.tools/dotnet/dotnet.exe' test tests/ZonyLrcTools.Gui.Tests/ZonyLrcTools.Gui.Tests.csproj --filter 'FullyQualifiedName!~LiveDownloadTests' --verbosity minimal
```
必要时网络/本机测试权限按工具审批机制获取。不要停在沙箱失败。检查截图并修正溢出、不可读状态及内部模板默认样式。
- [x] Step 5：更新文档，检查 git diff，无关文件不纳入；提交实现。完整报告记录构建/测试命令和结果、渲染目录、源文件变更及仍需控制器检查的实机事项。不要 push、merge、发布，也不要派遣子代理。

实施工作区：D:/workspace/ZonyLrcToolsX/ui-redesign，分支 feature/ui-refinement，基线 41e3bed0d8662685a1d33414ffed8c609c586ffd。

控制器在实现后进行任务审查、实际 Windows 验收和最终整分支审查，再按用户已授权的主线合并/推送要求完成交付；新版本 Release 标签不在本轮实施范围。
