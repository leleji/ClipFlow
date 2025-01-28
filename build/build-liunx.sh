#!/bin/bash

# 清理
rm -rf ./out/
rm -rf ./staging_folder/

mkdir ./out
mkdir ./out/linux-x64


# .NET 发布
# 推荐使用 self-contained 发布，这样用户不需要安装 .NET运行时
dotnet publish "./str/ClipFlow/ClipFlow.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./out/linux-x64"

# 暂存目录
mkdir staging_folder

# Debian control文件
mkdir ./staging_folder/DEBIAN
cp ./build/control ./staging_folder/DEBIAN

# 启动脚本
mkdir ./staging_folder/usr
# mkdir ./staging_folder/usr/bin
# cp ./src/MyProgram.Desktop.Debian/myprogram.sh ./staging_folder/usr/bin/myprogram
# chmod +x ./staging_folder/usr/bin/myprogram # 设置启动脚本的执行权限

# 其他文件
mkdir ./staging_folder/usr/lib
mkdir ./staging_folder/usr/lib/clipflow
cp -f -a ./out/linux-x64/. ./staging_folder/usr/lib/clipflow/ # 从publish目录复制所有文件
chmod -R a+rX ./staging_folder/usr/lib/clipflow/ # 设置所有文件的读权限
chmod +x ./staging_folder/usr/lib/clipflow/ClipFlow # 设置主可执行文件的执行权限

# 桌面快捷方式
mkdir ./staging_folder/usr/share
mkdir ./staging_folder/usr/share/applications
cp ./build/clipflow.desktop ./staging_folder/usr/share/applications/clipflow.desktop

# 桌面图标
# 一个 1024px x 1024px 的 PNG 文件，类似于 VS Code 使用的图标
mkdir ./staging_folder/usr/share/pixmaps
cp ./build/logo.png ./staging_folder/usr/share/pixmaps/clipflow.png
chmod 644 staging_folder/usr/share/pixmaps/clipflow.png

# Hicolor 图标
mkdir ./staging_folder/usr/share/icons
mkdir ./staging_folder/usr/share/icons/hicolor
mkdir ./staging_folder/usr/share/icons/hicolor/scalable
mkdir ./staging_folder/usr/share/icons/hicolor/scalable/apps
cp ./build/logo.svg ./staging_folder/usr/share/icons/hicolor/scalable/apps/build.svg


# 制作 .deb 文件
dpkg-deb --root-owner-group --build ./staging_folder/ ./ClipFlow_1.0.0_amd64.deb