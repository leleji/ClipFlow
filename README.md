# ClipFlow
剪贴板多端同步(文字,文件,文件夹)，支持windows,macos,liunx

## 功能列表

- ✅ 剪贴板同步
- ✅ 多设备支持
- ❌ 端到端加密（开发中）
- ❌ 快捷键（开发中）
- ❌ 历史记录（计划中）


## 服务端安装
#### Docker

```
docker run -d \
  --name=clipflow-server \
  -p 8080:8080 \
  --restart unless-stopped \
  leleji/clipflow-server:latest
```
#### Docker Compose


```
version: '3'
services:
  syncclipboard-server:
    image: leleji/clipflow-server:latest
    container_name: clipflow-server
    restart: unless-stopped
    ports:
      - "8080:8080" # Update this if you have changed the port in appsettings.json
    environment:
```

## 客户端

### 依赖： 
名称带`self-contained`名称版本安装包，已经包含运行环境无需安装环境。
#### windows  
[x64](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-8.0.12-windows-x64-installer)， [x86](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-8.0.12-windows-x86-installer)，[ Arm64 ](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-8.0.12-windows-arm64-installer)
下载对应架构运行环境，找到对应未安装会弹窗提醒并跳转到微软官方下载页面  

#### liunx
使用 Microsoft 提供的 APT 仓库
```
sudo apt update
sudo apt install -y dotnet-runtime-8.0
```
官方安装说明：[在 Linux 上安装 .NET](https://learn.microsoft.com/zh-cn/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website)

#### macos

 使用 Homebrew
```
brew install dotnet-sdk
```
[x64](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-8.0.12-macos-x64-installer)，[ Arm64 ](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-8.0.12-macos-arm64-installer) 手动下载对应安装包，m芯片arm64,英特尔x64

#### IOS
暂无，

#### Android
暂无，

### 注意：
- 删除软件时，配置文件目录不会被删除，配置文件储存在`~/.config/ClipFlow/`(Linux)，`~/Library/Application Support/ClipFlow/`(macOS)，需要彻底删除软件时请手动删除整个目录
- 使用`deb`、`rpm`安装包时，每次更新版本需要先删除旧版，再安装新版，不支持直接更新
- macOS: `“ClipFlow”已损坏，无法打开`，在终端中执行`sudo xattr -d com.apple.quarantine /Applications/ClipFlow.app`

### 客户端配置说明

全平台依赖三条必要配置（配置的拼写可能会有所不同，含义相同）。
- token  `用于验证权限`
- userKey  `用来区分用户`
- url， `格式为http(s)://ip(或者域名):port。`

## API

```
curl -X POST 'https://localhost:7145/api/Clipboard/filelist?filename=files_202230211214.zip' -H 'Host: localhost:7145' -H 'Content-Type: application/zip' -H 'X-Auth-Token: token1' -H 'X-User-Key: 1333' -H 'X-Client-Id: 17ad2d30-ca24-4dcf-a28e-6ea8905edffc' --data-binary @"D:\swftware\ClipFlow.zip"

```


