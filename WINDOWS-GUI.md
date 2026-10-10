# Windows 图形界面

这是基于 real-zony/ZonyLrcToolsX 的 `dev` 分支制作的 Windows 桌面版本。
复用 Common 项目的歌词源、标签读取和专辑封面下载逻辑，使用 Avalonia 提供界面。
上游代码与许可证保留在项目内。

## 使用

解压 `ZonyLrcTools-Windows-GUI-x64.zip`，双击 `ZonyLrcTools.Desktop.exe`。
请保留同目录的 `config.yaml` 和 `BlockWords.json`。
免安装包包含 .NET 运行时，适用于 Windows 10/11 x64。

1. **批量歌词**：浏览并选择音乐文件夹，递归扫描 MP3、FLAC、WAV、M4A、OGG 和 OPUS。
   可以修改表格中的歌名、歌手，点击“开始下载”。歌词保存到歌曲旁，名称与歌曲相同。
2. **单曲歌词**：输入歌名和歌手，点击“匹配歌词”，确认内容后选择目录并保存 `.lrc`。
3. **专辑封面**：选择音乐文件夹，扫描后下载封面，保存到歌曲旁，已有封面自动跳过。
4. **设置**：调整翻译、双语合并、编码、歌词源开关和优先级。改动自动保存。
   代理配置在重启后生效。默认中文，可以选择跟随系统或英文。
5. **运行日志**：查看错误，导出日志。批量任务也支持停止、仅重试失败和导出失败列表。

首次运行默认跳过已有歌词；选择“替换已有文件”后会替换已有歌词。
下载失败不会删除原来的歌词。默认并行数为 2，建议设置为 1～2。
设置保存到 `%LOCALAPPDATA%\ZonyLrcToolsX.Gui`，因此升级和重新解压不会覆盖个人设置。
从安装目录复制的是首次运行的默认配置，日常使用无需手动编辑 YAML。

当前界面覆盖本地音乐、单曲歌词和专辑封面下载；网易云歌单扫码登录、CSV 导入及
加密音乐转换仍属于上游 CLI 功能，未加入此界面。
歌词源可用性和匹配准确度取决于上游接口，单曲模式可以先预览匹配内容。

## 工作台界面

导航按开始、批量歌词、单曲歌词、专辑封面排序，运行日志和设置位于底部。
批量列表在空闲时可用 F2 或双击修改歌名、歌手；下载中可滚动和选择，不能编辑。
选中歌曲只读取本地 `.lrc`，不会联网；宽窗口显示右侧预览，窄窗口显示可用 Esc 关闭的抽屉。
重试仅处理失败项，进度分母为本次重试数量；成功、跳过、失败和未完成分别统计。
单曲预览显示提交查询、实际来源与行数；修改歌名或歌手后需要重新匹配，避免保存旧歌词。
设置中的双语合并在关闭翻译时禁用，但保留此前偏好。
窗口保留系统标题栏，默认 1200×800、最小 900×600，支持浅色/深色和中英切换。

## 界面精修

GUI 1.1.0（2026-10-10）：统一浅深色控件状态、轮廓图标及紧凑设置布局，
改善批量列表和本地歌词预览，保留状态表头的升降序排序。
首页和 Windows 文件版本均使用 GUI 版本；上游下载核心仍为 4.0.3。

侧栏为 180 像素，内容边距为 24 像素。按钮、输入框、表格与设置使用同一套浅深色
主题；导航和辅助操作使用可随状态变色的轮廓图标，键盘焦点保持可见。
数值输入为横向「− 数字 +」，仍支持键盘输入、上下方向键、上下限和任务运行时禁用。

批量页将文件夹路径放在单行，开始/停止位于工具栏右侧；表格使用细水平线和浅色选中背景，
歌手显示为次文本，成功和失败状态分别着色。本地预览为可选择复制、无输入框边框的只读歌词。
内容宽度达到 840 像素时，预览占右侧 260 像素；较窄时为 280 像素抽屉。
抽屉只覆盖列表区域，停止按钮、进度和重试仍可操作；Esc 或关闭图标收起预览。

设置使用紧凑左右行：翻译、合并、歌词源和代理为开关；已有歌词为跳过/替换选择。
来源名称、启用、优先级三列对齐，关闭来源时禁用优先级。网络、外观与语言位于同一滚动区域，
保存结果和恢复默认入口固定在底部。首页的两个真实下载入口会随窗口宽度展开。

图标源文件位于 `src/ZonyLrcTools.Desktop/Assets`。用已安装的 Node.js 和 sharp 可重新导出：

```powershell
node scripts/Export-BrandIcons.cjs <sharp 模块路径>
```

## 构建与验证（开发者）

需要 .NET 10 SDK。运行 `scripts/Publish-WindowsGui.ps1` 生成包含运行时的 x64 便携包。
源码项目：`src/ZonyLrcTools.Desktop/ZonyLrcTools.Desktop.csproj`。

```powershell
dotnet test tests/ZonyLrcTools.Gui.Tests -c Release --filter "Category!=Live"
dotnet test tests/ZonyLrcTools.Gui.Tests -c Release --filter "Category=Live"
```

本地测试覆盖页面渲染/切换、中英动态切换、真实音乐标签扫描、设置持久化、已有歌词保护、
禁用歌词源、编码、取消后的写入保护和封面状态回调；另覆盖重试分母、跳过项排除、
并行数锁定、停止中状态、选中本地预览竞态、下载完成后预览刷新、Esc 抽屉关闭，
以及单曲输入变更、匹配中输入变更和覆盖确认等待期间变更的保存保护。
联网测试通过桌面 ViewModel 命令匹配并保存示例歌曲，需要可访问歌词源。
设置 `ZONY_GUI_TEST_ARTIFACTS` 为输出目录时，本地测试会生成 900×600、1200×800、
1440×900 的中英浅深色页面及含 5000 行的本地预览/匹配结果截图。
这些是 Avalonia Headless 的逻辑像素渲染，不能替代 Windows 原生窗口、任务栏和高 DPI 验收。

复现精修页面及控件状态截图：

```powershell
$env:ZONY_GUI_TEST_ARTIFACTS = "$PWD/artifacts/ui-refinement/renders"
dotnet test tests/ZonyLrcTools.Gui.Tests --filter "FullyQualifiedName!~LiveDownloadTests"
```

除页面矩阵外，`controls-light.png` 和 `controls-dark.png` 展示正常、悬停、按下、禁用及
键盘焦点的实际 Avalonia 模板。数值控件回归测试通过鼠标与键盘操作检查绑定、上下限及禁用。
原生验收时启动新构建程序，分别在默认尺寸和最小尺寸检查导航悬停、开始/停止、数值输入、
表格编辑与选择、预览关闭/复制、设置开关和滚动；切换深色与英文再检查一次。

## GitHub 自动发布

默认主线为 `dev`。推送 `gui-v主版本.次版本.修订版本` 标签后，`Windows GUI Release`
工作流自动测试并发布：

- `ZonyLrcTools-Windows-GUI-x64-Setup-版本.exe`：安装版，包含运行时、开始菜单和可选桌面快捷方式。
- `ZonyLrcTools-Windows-GUI-x64.zip`：解压即可运行的便携版。
- `SHA256SUMS.txt`：两个文件的 SHA256 校验值。

安装版默认安装到当前用户目录，无需管理员权限；升级和卸载保留用户配置。
安装程序目前未进行代码签名。

```powershell
git checkout dev
git pull --ff-only
git tag -a gui-v1.1.0 -m "Windows GUI 1.1.0"
git push origin gui-v1.1.0
```

后续递增版本号，不移动已发布标签。也可在 GitHub 新建并发布 `gui-v` 标签的 Release，
或在 Actions 手动运行工作流并填写已有 GUI 标签。标签提交必须属于 `dev` 主线。
流程使用自动提供的 `GITHUB_TOKEN`，无需配置额外密钥。原有命令行版发布流程保留。

本地制作安装版需要 Inno Setup 6：

```powershell
./scripts/Publish-WindowsGui.ps1 -OutputDirectory ./dist
./scripts/Build-WindowsGuiInstaller.ps1 -Version 1.1.0 -OutputDirectory ./dist
```

## 来源

- 项目：https://github.com/real-zony/ZonyLrcToolsX
- 文档：https://docs.myzony.com
- 官方文档的本地参考副本：`docs/reference/`
- 许可证：`LICENSE`
