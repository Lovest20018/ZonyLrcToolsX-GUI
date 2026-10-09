# Windows 图形界面

这是基于 real-zony/ZonyLrcToolsX 的 `dev` 分支制作的 Windows 桌面版本。
复用 Common 项目的歌词源、标签读取和专辑封面下载逻辑，使用 Avalonia 提供界面。
上游代码与许可证保留在项目内。

## 使用

解压 `ZonyLrcTools-Windows-GUI-x64.zip`，双击 `ZonyLrcTools.Desktop.exe`。
请保留同目录的 `config.yaml` 和 `BlockWords.json`。
免安装包包含 .NET 运行时，适用于 Windows 10/11 x64。

1. **歌词下载**：浏览并选择音乐文件夹，递归扫描 MP3、FLAC、WAV、M4A、OGG 和 OPUS。
   可以修改表格中的歌名、歌手，点击“开始下载”。歌词保存到歌曲旁，名称与歌曲相同。
2. **单曲歌词**：输入歌名和歌手，点击“匹配并预览”，确认内容后选择目录并保存 `.lrc`。
3. **专辑封面**：选择音乐文件夹，扫描后下载封面，保存到歌曲旁，已有封面自动跳过。
4. **设置**：调整翻译、双语合并、编码、歌词源开关和优先级。改动自动保存。
   代理配置在重启后生效。默认中文，可以选择跟随系统或英文。
5. **运行日志**：查看错误，导出日志。批量任务也支持停止、仅重试失败和导出失败列表。

首次运行默认跳过已有歌词；关闭“跳过已有 .lrc 文件”后会替换已有歌词。
下载失败不会删除原来的歌词。默认并行数为 2，建议设置为 1～2。
设置保存到 `%LOCALAPPDATA%\ZonyLrcToolsX.Gui`，因此升级和重新解压不会覆盖个人设置。
从安装目录复制的是首次运行的默认配置，日常使用无需手动编辑 YAML。

当前界面覆盖本地音乐、单曲歌词和专辑封面下载；网易云歌单扫码登录、CSV 导入及
加密音乐转换仍属于上游 CLI 功能，未加入此界面。
歌词源可用性和匹配准确度取决于上游接口，单曲模式可以先预览匹配内容。

## 构建与验证（开发者）

需要 .NET 10 SDK。运行 `scripts/Publish-WindowsGui.ps1` 生成包含运行时的 x64 便携包。
源码项目：`src/ZonyLrcTools.Desktop/ZonyLrcTools.Desktop.csproj`。

```powershell
dotnet test tests/ZonyLrcTools.Gui.Tests -c Release --filter "Category!=Live"
dotnet test tests/ZonyLrcTools.Gui.Tests -c Release --filter "Category=Live"
```

本地测试覆盖页面渲染/切换、真实音乐标签扫描、设置持久化、已有歌词保护、
禁用歌词源、编码、取消后的写入保护和封面状态回调。
联网测试通过桌面 ViewModel 命令匹配并保存示例歌曲，需要可访问歌词源。

## 来源

- 项目：https://github.com/real-zony/ZonyLrcToolsX
- 文档：https://docs.myzony.com
- 官方文档的本地参考副本：`docs/reference/`
- 许可证：`LICENSE`
