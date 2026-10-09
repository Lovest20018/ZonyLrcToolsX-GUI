# 快速开始
## 打开终端
### Windows 用户
> **⚠️注意**
> Windows 用户请注意斜杠的区别，Windows 的路径分隔符是 `/` 而类 UNIX 系统则是 `\`，如果你指定了错误的符号，那么可能会出现找不到路径的问题导致程序执行失败。

Windows 用户在下载完成软件的压缩包之后，打开解压后的文件夹，按住 Shift + 右键呼出菜单，然后选择 PowerShell/命令提示符/Windows 终端，根据下述说明执行命令即可。

### macOS 用户
> **⚠️注意**
> macOS 和 Linux 系统在文件解压完成之后，还需要确保其是否具备可执行权限。
> 请使用 `sudo chmod +x ./ZonyLrcTools.Cli` 命令授予工具可执行权限。

 按下 `Command+空格` 唤醒搜索框，在里面输入 `终端` 之后按回车会运行终端程序，它看起来像是这样。
![image-20230813204445279](./Images/image-20230813204445279.png)

![image-20230813204520840](./Images/image-20230813204520840.png)

### Linux 用户
> **⚠️注意**
> macOS 和 Linux 系统在文件解压完成之后，还需要确保其是否具备可执行权限。
> 请使用 `sudo chmod +x ./ZonyLrcTools.Cli` 命令授予工具可执行权限。

既然你已经使用 Linux 系统了，那么你肯定知道如何打开 Terminal，这里不再详细赘述如何打开终端，仅提醒使用者注意程序是否拥有可执行权限。

## 下载歌词
这里假设你需要下载歌词的歌曲文件夹，其路径是 `C:\歌曲目录` 或  `/media/music`，直接执行以下指令即可进行下载操作。

**Windows**
```powershell
.\ZonyLrcTools.Cli.exe download -d "C:\歌曲目录" -l -n 2
```

**macOS/Linux**
```shell
./ZonyLrcTools.Cli download -d "/media/music" -l -n 2
```

## 下载专辑封面
这里假设你需要下载歌词的歌曲文件夹，其路径是 `C:\歌曲目录` 或  `/media/music`，直接执行以下指令即可进行下载操作。

**Windows**
```powershell
.\ZonyLrcTools.Cli.exe download -d "C:\歌曲目录" -a -n 2
```

**macOS/Linux**
```shell
./ZonyLrcTools.Cli download -d "/media/music" -a -n 2
```

## 格式转换
如果你拥有某些特定格式的歌曲，例如 `*.ncm` 需要转换为通用格式 `*.flac` / `*.mp3` ，可以直接使用以下命令进行转换。

这里以加密歌曲路径 `C:\加密歌曲` 或 `/media/ncm-music` 为例。
**Windows**
```powershell
.\ZonyLrcTools.Cli.exe util -s C:\加密歌曲
```

**macOS/Linux**
```shell
.\ZonyLrcTools.Cli.exe util -s /media/ncm-music
```
