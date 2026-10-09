# 歌词下载
歌词下载功能是本工具的核心功能，本节主要介绍了歌词下载子命令 `download` 的常见参数和使用方法。

## 命令结构
执行 `./ZonyLrcTools.Cli download -h` 指令可以得到具体的帮助信息，常规情况来说它会输出以下内容。
```shell
zony@Zonys-MBP TempFiles % ./ZonyLrcTools.Cli download -h
[21:18:12 INF] Starting...
下载歌词文件或专辑图像。

Usage: lyric-tool download [options]

Options:
  -h|--help          欢迎使用 ZonyLrcToolsX Command Line Interface，有任何问题请访问 https://soft.myzony.com 或添加 QQ 群 337656932 寻求帮助。
  -d|--dir           指定需要扫描的目录。
  -l|--lyric         指定程序需要下载歌词文件。
  -a|--album         指定程序需要下载专辑图像。
  -n|--number        指定下载时候的线程数量。(默认值 1)
                     Default value is: 1.
  -sc|--scanner      指定歌词文件扫描器，目前支持本地文件(local)，网易云音乐(netease)，csv 文件(csv)，默认值为 local。
                     Default value is: local.
  -o|--output        指定歌词文件的输出路径。
                     Default value is: DownloadedLrc.
  -p|--pattern       指定歌词文件的输出文件名模式。
                     Default value is: {Artist} - {Name}.lrc.
  -f|--file          指定 CSV 文件的路径。
  -s|--song-list-id  指定网易云音乐歌单的 ID，如果有多个歌单，请使用 ';' 分割 ID。
```
其中几个参数是必填的，从 [快速开始](quickstart.md) 说明我们可以看到，下载歌词时必须传递 `-d` 和 `-l`/`-a` 参数。其中 `-l` 说明我们要下载歌词，而 `-a` 则表明我们需要下载专辑图像。两个参数二选一提供即可，如果同时提供则不会有任何效果。

## 参数详解
### 线程数量
`-n` 选项用于指定下载歌词时的线程数量，理论上来说线程数量越多，同时下载的歌词文件就越多，整体的下载速度越快。
但是由于歌词源服务器的检测和限制，如果同一个 IP 地址同时在短时间内发起大量下载请求，会导致服务器对这个 IP 进行临时封禁，程序就会提示大量的下载错误信息。
因此，这个线程数量一般建议设置为 `1` 或者 `2`，这样可以保证程序在下载歌词的时候不会被服务器进行限制。

### 文件扫描器
`-sc` 选项用于指定歌曲信息的来源，默认情况下会使用 `local` 扫描器以歌曲的元信息(标签)，或者是文件名来作为歌词搜索的依据。对应的歌词文件也会在歌曲所在目录生成，不需要用户执行多余的操作。
**支持的扫描器**

| 参数值 | 说明 |
| --- | --- |
| local | 从指定目录的歌曲文件中获取歌曲信息。 |
| netease | 从网易云歌单获取歌曲信息。 |
| csv | 从 CSV 文件获取歌曲信息。 |

#### 网易云歌曲扫描器
使用网易云歌曲扫描器(`netease`)的完整命令如下:
```shell
.\ZonyLrcTools.Cli.exe download -sc netease -o "D:\TempFiles" -s "7224428149" -l
```
其中 `-sc` 参数用于指定歌词信息的来源是网易云歌单; `-o` 参数指定的是歌词文件的输出目录，请尽量使用绝对路径; `-s` 参数指定的的是歌单的 ID，该 ID 可以从网页版的网易云音乐获得。
例如获取地址 [https://music.163.com/#/playlist?id=158010361](https://music.163.com/#/playlist?id=158010361) 的歌单信息，那么歌单 ID 就应该传递 158010361。
如果你需要获取多个歌单的歌曲数据，请在传递歌单 ID 时使用分号 `;` 指定多个 ID，例如下面的命令就是下载两个歌单的歌曲数据。
```shell
.\ZonyLrcTools.Cli.exe download -sc netease -o "D:\TempFiles" -s "7224428149;158010361" -l
```
由于网易云音乐的限制，要想获取完整的歌单信息，必须扫码登录程序，还是以最上面的为例，我需要下载歌单内的歌词数据，就必须扫码之后程序才会执行。
![QR_CODE](./Images/image-20230328223155280.png)

### CSV 歌曲扫描器
应 [Issue 126](https://github.com/real-zony/ZonyLrcToolsX/issues/126) 的请求，增加了从 CSV 获取歌曲信息的方式。这样可以在没有原始歌曲的情况下载歌词数据。
示例命令:
```shell
.\ZonyLrcTools.Cli.exe download -sc csv -o "D:\TempFiles" -f "D:\TempFiles\demo.csv" -l
```
其中 `-f` 参数用于指定 csv 文件的路径，csv 文件的格式应该如下所示，保证第一行是列信息，一共要包含两列。
```csv
Song,Artist
刀马旦,李玟
发如雪,周杰伦
说书人,寅子
爱的供养,张国荣
七里香,周杰伦
```

### 歌词输出路径
`-o` 选项可以指定歌词文件最终输出的路径，如果你的扫描器指定的是`local` 以外的扫描器的话，这个参数是必填项。
这是因为默认的 `local` 扫描器会直接将歌词文件输出到歌曲所在目录，而其他的扫描器，例如网易云歌单扫描器，因为没有原始歌曲文件的路径，所以只能由用户自己指定一个目录用于存储歌词文件。
