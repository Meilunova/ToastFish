<p align="center">
  <img src="Resources/chika128.ico" width="128" height="128" alt="图标"/>
</p>

<div align="center">

# ToastFish

#### 🐟 利用 Windows 通知栏背单词的智能学习软件

#### 📚 支持英语、日语多语言学习，让你在任何环境下都能安全隐蔽地背单词

#### 🎵 全新音频优化 + 智能测试控制，打造完美学习体验

![License MIT](https://img.shields.io/badge/license-MIT-orange)
![GitHub release (latest by date)](https://img.shields.io/badge/release-v3.1-blue)
![GitHub issues](https://img.shields.io/github/issues/Uahh/ToastFish)
[![.NET Build & Test](https://github.com/Uahh/ToastFish/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/Uahh/ToastFish/actions/workflows/dotnet-desktop.yml)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-lightgrey)
![Language](https://img.shields.io/badge/language-C%23-blue)

</div>

## ✨ 最新功能亮点

### 🎯 测试环节智能控制

- **灵活配置**：可单独控制"抽背后测试"和"背诵后测试"
- **一键切换**：右键菜单轻松开启/关闭测试环节
- **个性化学习**：根据个人需求定制学习流程
- **设置持久化**：配置自动保存，重启后依然有效

### 🚀 随机抽背增强

- **定时提醒**：支持设置时间段进行随机单词抽背
- **多词库支持**：英语、日语、五十音全覆盖
- **批量学习**：支持 1-10 个单词的批量抽背模式

## 📖 使用方法

### 🎯 基本流程

1. **选择词库**：右键托盘图标 → 选择英语词汇或日语词汇

![选择词库](https://github.com/Uahh/ToastFish/blob/main/Resources/Gif/选择词库.gif)

2. **设置背诵单词数量**：右键托盘图标 → 参数设置 → 设置单词数量

![设置词数](https://github.com/Uahh/ToastFish/blob/main/Resources/Gif/选择数量.gif)

3. **开始学习**：右键托盘图标 → 开始

![设置词数](https://github.com/Uahh/ToastFish/blob/main/Resources/Gif/开始.gif)

4. **测试环节**：学习完成后可选择进入测试（可在设置中关闭）

![设置词数](https://github.com/Uahh/ToastFish/blob/main/Resources/Gif/测试.gif)

### ⚙️ 高级设置

#### 🎵 音频设置

- **自动播放**：右键托盘图标 → 参数设置 → 勾选"自动播放"
- **英标类型**：可选择美式或英式发音
- **音量优化**：所有发音功能已统一音量，确保清晰可听

#### 🎯 测试控制

- **抽背后测试**：右键托盘图标 → 参数设置 → 勾选/取消"抽背后测试"
- **背诵后测试**：右键托盘图标 → 参数设置 → 勾选/取消"背诵后测试"
- **灵活配置**：可根据个人需求单独开启或关闭任一测试环节

#### 🚀 随机抽背设置

- **启用随机抽背**：右键托盘图标 → 参数设置 → 随机抽背设置
- **时间段设置**：可设置工作时间、学习时间等
- **单词数量**：支持 1-10 个单词的随机抽背
- **勿扰模式**：可设置午休等时间段暂停抽背

### 背诵记录

每一次点击开始都会有记录，文件格式为 xlsx。位于安装目录的 Log 文件夹下。

### 导入单词

可以将背诵记录导入，重新背诵。

![设置词数](https://github.com/Uahh/ToastFish/blob/main/Resources/Gif/导入单词.gif)

### 自定义内容

可以通过自定义 Excel 内容来让 ToastFish 推送所需要的内容。  
自定义 Excel 模板位于安装目录/Resources/自定义模板.xslx

![设置词数](https://github.com/Uahh/ToastFish/blob/main/Resources/Gif/导入自定义单词.gif)

### 操作系统要求

Windows 10 及以上

### Q&A

Q: 每次通知停留时间太短了，如何设置停留时间？  
A: 可以在系统设置 -> 轻松使用 -> 显示 -> 通知显示的时间 里设置停留时间

Q: 使用英语发音功能时会闪退？  
A: 请在系统设置里下载英语语音包，重启软件即可解决。

Q: 有没有 Win7 或是 Mac 版本的开发计划？  
A: 这个真没有，Win7 和 Mac 没有 Windows10 的通知栏。

Q: 没有我想要背的单词怎么办？  
A: 可以使用自定义功能自己构建单词列表，如果单词数量很多，可以联系作者帮忙添加。

Q: 遇到了其他困难或是想给软件提建议？  
A: 可以提 Issue，将问题或建议提供给我。

Q: 软件收费吗？  
A: 软件完全开源且免费。

## 下载与安装

1. 可以去网盘下载，下载双击安装 ToastFishSetup.exe 即可。

```bash
链接：https://pan.baidu.com/s/1VlnJSSbEgcNErV-gy3um6w
提取码：2173
```

2. 也可以去项目 Tag 处下载 Release 版本，解压即可免安装运行。

## 🛠️ 开发环境搭建

### 📋 环境要求

- **操作系统**：Windows 10 及以上
- **开发工具**：Visual Studio 2019/2022 或 Visual Studio Code
- **.NET Framework**：4.7.2 或更高版本
- **Git**：用于克隆代码仓库

### 🚀 快速开始

#### 1. 克隆项目

```bash
git clone https://github.com/Uahh/ToastFish.git
cd ToastFish
```

#### 2. 安装依赖

项目使用 NuGet 包管理器，依赖包会自动还原。主要依赖包括：

- Microsoft.Toolkit.Uwp.Notifications (通知功能)
- Dapper (数据库操作)
- NPOI (Excel 文件处理)
- Newtonsoft.Json (JSON 处理)

#### 3. 编译项目

**方法一：使用 Visual Studio**

1. 打开 `ToastFish.sln` 解决方案文件
2. 选择 `Debug` 或 `Release` 配置
3. 按 `Ctrl+Shift+B` 或点击"生成解决方案"

**方法二：使用命令行**

```bash
# 使用 MSBuild 编译
msbuild ToastFish/ToastFish.csproj /p:Configuration=Release /p:Platform=AnyCPU

# 或使用一键编译脚本
./build.bat
```

#### 4. 运行程序

编译完成后，可执行文件位于：

```
ToastFish/bin/Debug/ToastFish.exe    # Debug版本
ToastFish/bin/Release/ToastFish.exe  # Release版本
```

### 📦 一键编译脚本

为了简化编译过程，我们提供了一键编译脚本 `build.bat`：

```batch
# 运行一键编译脚本
./build.bat
```

该脚本会自动：

- ✅ 检查开发环境（MSBuild、.NET Framework）
- 🧹 清理旧的编译文件
- 📦 还原 NuGet 依赖包
- 🔨 编译 Release 版本
- 🚀 可选择立即运行程序

### 🔧 开发说明

#### 项目结构

```
ToastFish/
├── Model/                  # 业务逻辑层
│   ├── PushControl/       # 通知推送控制
│   ├── SqliteControl/     # 数据库操作
│   ├── RandomSchedule/    # 随机抽背功能
│   └── Mp3/              # 音频处理
├── View/                  # 用户界面
├── Resources/             # 资源文件
└── Test/                 # 测试代码
```

#### 核心技术特性

- **🔔 通知系统**：基于 Windows 10+ 原生通知 API，完美融入系统
- **🎵 音频处理**：统一音量管理 + 智能同步播放
- **💾 数据存储**：SQLite 数据库 + Excel 导入导出
- **⏰ 定时任务**：自定义调度器实现随机抽背
- **🌐 多语言支持**：英语、日语、五十音全覆盖
- **🎯 智能测试**：可配置的测试环节控制

#### 最新技术亮点

- **智能音频同步算法**：根据内容复杂度动态调整播放时机
- **统一音量管理系统**：解决不同功能间音量不一致问题
- **灵活测试环节配置**：支持单独控制各类测试环节
- **跨语言音频优化**：英语、日语发音统一标准

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

### 提交 Issue

- 🐛 Bug 报告：请详细描述问题复现步骤
- 💡 功能建议：请说明功能的使用场景和预期效果
- ❓ 使用问题：请提供系统环境和错误信息

### 提交 Pull Request

1. Fork 本仓库
2. 创建功能分支：`git checkout -b feature/your-feature`
3. 提交更改：`git commit -am 'Add some feature'`
4. 推送分支：`git push origin feature/your-feature`
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 感谢

感谢 @itorr 为本软件提供的支持、建议和测试！

## 📞 联系我们

- 🌐 官方网站：https://lab.magiconch.com/toast-fish/
- 📧 问题反馈：通过 GitHub Issues
- 💬 讨论交流：欢迎在 Issues 中分享使用心得
