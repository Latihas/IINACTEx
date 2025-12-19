> 友情裤链: https://raw.githubusercontent.com/Latihas/dalamud-plugins/main/repo.json

![](https://socialify.git.ci/Latihas/IINACTEx/image?description=1&forks=1&issues=1&language=1&name=1&owner=1&pattern=Transparent&pulls=1&stargazers=1&theme=Auto)

## IINACT改版，整合了CN、Triggernometry、PostNamazu

# 重要提醒！！！请一定要先看完介绍再使用，本插件仍然不是很稳定，有炸游戏风险

# 项目介绍

修改IINACT的初衷旨在尽可能满足日常对ACT的基本需求，替代ACT，假装自己是西瓜玩。

本项目仍然处于野蛮开发期，代码管理极其混乱，暗藏神秘bug，仅作开发测试使用。

该插件会在一个类ACT的环境下运行FFXIV_ACT_Plugin与大量修改的Overlay
Plugin以适配现代.NET。与此同时，也添加了大量修改的Triggernometry与Postnamazu。包括类ACT、Triggernometry、Postnamazu在内，这些并非完整的代码移植，并且仍在开发完善中，可能缺少部分原版的函数，开发时请注意。

相关参考文献如下：
> IINACT官方: https://github.com/marzent/IINACT
> 
> IINACT CN: https://github.com/MeowZWR/IINACT
> 
> Machina: https://github.com/MeowZWR/machina
> 
> Edge TTS: https://github.com/AtmoOmen/EdgeTTS
> 
> Triggernometry: https://github.com/MnFeN/Triggernometry
> 
> PostNamazu: https://github.com/Natsukage/PostNamazu

# 悬浮窗

开发者偏好使用Browsingway，但是IINACT原版的URL生成器指向的在线页面可能不是最新版，建议指向本地ACT的目录

比如开发者的ACT装在D盘，那么URL就类似于 file:///D:
/ACT.DieMoe/Plugins/ACT.OverlayPlugin/cactbot/ui/raidboss/raidboss.html?timeline=1&alerts=1

有时候bw会不显示东西（如时间轴），需要手动在bw里面刷新一下。每次插件加载会自动刷新bw的(时间轴,设置)悬浮窗。

IINACTEx也有内置一个原生的伤害统计悬浮窗，输入/iinactoverlay可打开。提供最基础的统计功能:

![](./pic/overlay.jpg)

# TTS/Cactbot

IINACT CN默认使用了EdgeTTS，如果你EdgeTTS工作正常可以跳过这部分。由于开发者比较喜欢用LatihasTTS(纯本地模型推理)所以也做了接口。

cactbot资源可在 https://raw.githubusercontent.com/Latihas/dalamud-plugins/main/cactbot.zip 下载。

仅需将相关文件放入插件安装目录下即可使用，加号代表添加的文件，像这样:

- $(installedPlugins/IINACTEx)
    - Scripts/
    - TriggernometryRepoBackups/
    - +TtsAssets/
        - +pinyin.txt
        - +symbol.txt
        - +vocab.txt
        - +a.ort
        - +v.ort
        - +...
    - Advanced Combat Tracker.dll
    - Triggernometry.dll
    - xxx.dll
    - +cactbot.zip
    - ...

目前支持cactbot.zip与TTS资源放在插件安装目录或者是插件Config目录，优先使用插件安装目录的。cactbot可以选择手动解压，也可以在插件下次加载时自动解压。

# 已知限制

不要在插件加载后立刻卸载插件，否则大概率会线程回收失败，只能重启游戏解决。

Triggernometry有时会因为宝宝椅的鲇鱼精扩展功能炸游戏/显示异常/...。开发者用Penumbra可以恢复部分图形问题。

IActPluginV1仅为不报错存在，不会执行加载等逻辑。

Postnamezu的Preset不可用。

触发器有时候声音比较小，减小游戏音量以调整。

Triggernometry的配置文件与ACT版本完全兼容，可以直接把act的配置文件复制到插件配置目录下。

Triggernometry的触发器不支持导入文件，过大的触发器建议分批导入。

Triggernometry保存配置逻辑与原版一致，即每5分钟或是卸载时，所以游戏崩溃可能会丢失配置。请导入或修改过任何触发器/配置/...后点击`保存
配置`按钮手动保存。

Triggernometry的部分高级内存操作与回调不可用。

Triggernometry的重复触发器导入重命名可能有问题，请尽量不要二次导入相同的触发器。

Triggernometry的部分编辑器还没写。

Triggernometry的绝大部分Form或Control因兼容性被移除，可能误伤配置弹出框，一般报错中可以看出来。

# 常见问题

问题太多了。如果出现bug，试着关开一下插件，说不定就自己会好了。

可以提issue让我写进来。

# 开发相关

本分支基于国服IINACT开发，如需移植到国际服理论上仅需修改IINACT官库与CN库的区别即可(
可能有部分翻译由于开发时未注意多语言需要适配一下)。

同理，卫月路径也需要改一下，在仓库目录中Directory.Build.props。

仓库目录中TrnDevEnv项目为Triggernometry脚本编写环境，喜欢用IDE的小朋友可以用这个开发。

> 以下是IINACT CN原版的介绍

## 笔记

> 国服兼容 [授人以渔](https://www.bilibili.com/opus/1045734514081398787)  
> 自然语音 [EdgeTTS](https://github.com/AtmoOmen/EdgeTTS)  
> ACT解析 [FFXIV_ACT_Plugin_CN](https://github.com/NewMoe-Technology/FFXIV_ACT_Plugin_CN)  
> [opcodes.jsonc](https://github.com/OverlayPlugin/OverlayPlugin/blob/main/OverlayPlugin.Core/resources/opcodes.jsonc)  [FFXIVProcessCn.cs](https://github.com/OverlayPlugin/cactbot/blob/main/plugin/CactbotEventSource/FFXIVProcessCn.cs)  [opcodes.jsonc](https://github.com/moewcorp/FFXIVNetworkOpcodes/tree/master/output)

![icon](https://github.com/marzent/IINACT/blob/main/images/icon.ico?raw=true)

# IINACT

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin to run
the [FFXIV_ACT_Plugin](https://github.com/ravahn/FFXIV_ACT_Plugin) in an [ACT](https://advancedcombattracker.com/)-like
enviroment with a heavily modified port of [Overlay Plugin](https://github.com/OverlayPlugin/OverlayPlugin) for modern
.NET.

The data source here is only based on [Unscrambler](https://github.com/perchbirdd/Unscrambler) and does not require any
extra injection with [Deucalion](https://github.com/ff14wed/deucalion) or network capture with elevated privileges.

This will **not** render overlays by itself, use something
like [Browsingway](https://github.com/Styr1x/Browsingway), [Next UI](https://github.com/kaminaris/Next-UI), [hudkit](https://github.com/valarnin/hudkit) (
Linux only) or [Bunny HUD](https://github.com/marzent/Bunny-HUD) (macOS only) to display Overlays.

## Why

- ACT is too inconvenient IMHO for just wanting to have the game data parsed and served via a WebSocket server
- Drastically more efficent than ACT, in part to .NET 7.0, in part to a more sane log line processing (disk I/O is not
  blocking LogLineEvents and happening on a separate lower priority thread)
- Due to the above and running fully inside the game process CPU usage will be orders of magnitude (not exaggerating
  here) lower when running under Wine compared to network-based capture
- Uses an ultra fast and low latency WebSocket server based
  on [NetCoreServer](https://github.com/chronoxor/NetCoreServer)
- Doesn't use legacy technology that hurts Linux and macOS users
- Follows the Unix philosophy of just doing one thing and doing it well

## Installing

> **Warning**  
> No support will be provided on any Dalamud official support channel. Please use
> the [Issues](https://github.com/marzent/IINACT/issues) page or [Discord](https://discord.gg/pcexJC8YPG) for any
> support
> requests. Do NOT ask for support on the [XIVLauncher & Dalamud Discord](https://discord.gg/holdshift), as support for
> 3rd-party plugins is not provided there.

Install instructions can be found [here](https://www.iinact.com/installation/), but are indentical to any other
3rd-party plugin repository.

## How to build

Just run

```
git clone --recurse-submodules https://github.com/marzent/IINACT.git
cd IINACT
dotnet build
``` 

on a Linux, macOS or Windows machine with the [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).

You will need to be able to reference Dalamud as well, meaning having an install
of [XL](https://github.com/goatcorp/FFXIVQuickLauncher) or [XOM](https://github.com/marzent/XIV-on-Mac) on Windows and
macOS respectively. On Linux `DALAMUD_HOME` needs to be correctly set (for example `$HOME/.xlcore/dalamud/Hooks/dev`).

## FAQ

**Where are my logs?**

- In your Documents folder. For Windows users, `C:\Users\[user]\Documents\IINACT`. For Mac/Linux users, same thing, but
  relative to your wine prefix.

**Are these logs compatible with FFLogs? Can I use the FFLogs Uploader?**

- Yes! 100% compatible.
