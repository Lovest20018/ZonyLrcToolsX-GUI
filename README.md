# ZonyLrcToolsX Windows GUI

基于 [real-zony/ZonyLrcToolsX](https://github.com/real-zony/ZonyLrcToolsX) 的 Windows 图形界面版本。
日常操作通过界面完成，无需命令行；x64 免安装包包含 .NET 运行时。

- 批量扫描音乐文件夹，下载同名 `.lrc` 歌词。
- 单曲匹配与预览，确认后保存歌词。
- 专辑封面下载、停止任务、失败重试及失败列表导出。
- 图形化设置歌词源、优先级、翻译、编码、已有歌词跳过和 HTTP 代理。
- 运行日志查看及导出。

详细操作、功能范围及构建方法见 [Windows GUI 使用说明](./WINDOWS-GUI.md)。
源码位于 `src/ZonyLrcTools.Desktop`，发布脚本为 `scripts/Publish-WindowsGui.ps1`。
使用 .NET 10 SDK 构建发布包后，双击 `ZonyLrcTools.Desktop.exe` 运行。

本地生成的 EXE、ZIP 和 SDK 缓存不纳入 Git。源码附带 12 项本地自动化测试及 1 项真实联网下载测试。

## 上游说明

English | [简体中文](./docs/zh_CN.md)

# Disclaimer

- This tool is for personal learning and research purposes only. The executable binary files are for demonstration purposes only and the source code and its products must not be used for commercial purposes. Otherwise, I [here](https://github.com/real-zony) will not be responsible for any related legal issues.
- Any unit or individual that downloads and uses the software resulting in any accidents, negligence, contract breaches, defamation, copyright or intellectual property infringement, and their resulting losses (including but not limited to direct, indirect, incidental or derivative losses), I [here](https://github.com/real-zony) will not bear any legal responsibility.
- Users clearly agree to all the terms listed in this statement and fully assume the risks and consequences of using this tool. I [here](https://github.com/real-zony) will not bear any legal responsibility.

# Introduction

ZonyLrcToolX 4 is a cross-platform lyrics download tool based on CEF. **QQ Group: 337656932**. Detailed video tutorials are available in the group files.

🚧 The current version is under development.
🚧 If you want to see working code, please switch to the dev branch.

# Download

To get the latest version, please visit the **[Release](https://github.com/real-zony/ZonyLrcToolsX/releases)** page for download.

## Arch Linux User Repository

This software has been packaged into [AUR](https://aur.archlinux.org/packages/zonylrctoolsx-bin). Arch Linux and its derivative distribution users can install it with the following command:

```bash
# yay or other AUR Helpers
yay -S zonylrctoolsx-bin

ZonyLrcTools.Cli --help
```

# Usage
Detailed usage instructions have been moved to a new documentation site. Please visit [https://docs.myzony.com](https://docs.myzony.com) for complete documentation.

# Donation
[爱发电](https://afdian.net/a/zony-lrc-tools)

# Star History

[![Star History Chart](https://star-history.dera.page/svg?repos=real-zony/ZonyLrcToolsX&type=Timeline)](https://star-history.dera.page/#real-zony/ZonyLrcToolsX&Timeline)
