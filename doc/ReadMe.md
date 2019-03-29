# Fishery &middot; ?
 [![GitHub license](https://img.shields.io/badge/license-LGPL-blue.svg)](./LICENSE) [![mono version](https://img.shields.io/badge/mono-v5.18-blue.svg)](https://www.mono-project.com/download/stable/) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/CepheiSigma/Fishery/pulls)

Fishery 是一款插件化的 NAS 内容管理工具。

## Fishery 的特点

* **插件化:** Fishery 本身只有一些基础功能，但是可以通过插件进行不断的扩展，插件与插件本身亦可以互相调用集成，使 Fishery 变得更加的好用。
* **开放API:** Fishery 提倡开发 API，你可以自由选择你喜欢的 App。

## 安装

Fishery 由 C# 编写而成，所以安装之前必须确保安装了以下程序：

* [Windows] .NET framework 4.6
* [macOS/Linux] Mono 5.18

从 [*releases*](https://github.com/CepheiSigma/Fishery/releases) 中下载最新的程序包解压，按照以下命令运行即可。

Windows

```
Fishary.App.Service.exe <数据存放位置> daemon
```

macOS/Linux

```
mono Fishary.App.Service.exe <数据存放目录> daemon
```

## 初始插件包

初始安装的 Fishery 并不包含插件，所以需要安装一些基础插件来进行功能的增强，当然，如果你已经熟练掌握配置的方法，可以自行安装需要的插件，而不安装预置插件包。

### 安装方法

从 [*releases*](https://github.com/CepheiSigma/Fishery/releases) 中下载最新的初始插件包并解压到 Fishery 使用的数据存放目录，重新启动 Fishery 即可。

### 初始插件包中的内容

初始插件包中包含了以下插件：

* 网络工具包
* Fishery 增强工具
* 终端日志

## 插件仓库

所有的插件的二进制包位于 [Fishery.Extension](https://github.com/CepheiSigma/Fishery.Extension) 仓库下，你可以访问 ??[~~ext-fishery.cephei.com.cn~~](http://ext-fishery.cephei.com.cn) (建设中)来查看所有的插件信息。

## 其它

更多关于 Fishery 的信息请查阅 [Wiki](https://github.com/CepheiSigma/Fishery/wiki) 。

## License

Fishery 基于 [LGPL](./LICENSE) 协议开源。

