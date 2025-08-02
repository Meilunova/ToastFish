@echo off
chcp 65001 >nul
echo ========================================
echo        ToastFish 一键编译脚本
echo ========================================
echo.

echo [1/5] 检查环境...
where msbuild >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误：未找到 MSBuild
    echo.
    echo 请安装以下任一工具：
    echo - Visual Studio 2019/2022
    echo - Visual Studio Build Tools
    echo - .NET Framework Developer Pack 4.7.2+
    echo.
    pause
    exit /b 1
)
echo ✅ MSBuild 环境检查通过

echo.
echo [2/5] 检查 .NET Framework...
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误：未找到 .NET Framework 4.7.2 或更高版本
    echo.
    echo 请从以下地址下载并安装：
    echo https://dotnet.microsoft.com/download/dotnet-framework
    echo.
    pause
    exit /b 1
)
echo ✅ .NET Framework 环境检查通过

echo.
echo [3/5] 清理旧的编译文件...
if exist "ToastFish\bin" rmdir /s /q "ToastFish\bin"
if exist "ToastFish\obj" rmdir /s /q "ToastFish\obj"
echo ✅ 清理完成

echo.
echo [4/5] 还原 NuGet 包...
if exist "packages.config" (
    nuget restore ToastFish.sln
    if %errorlevel% neq 0 (
        echo ⚠️  警告：NuGet 包还原失败，尝试继续编译...
    ) else (
        echo ✅ NuGet 包还原成功
    )
) else (
    echo ℹ️  未找到 packages.config，跳过 NuGet 还原
)

echo.
echo [5/5] 编译项目...
echo 正在编译 Release 版本...
msbuild ToastFish\ToastFish.csproj /p:Configuration=Release /p:Platform=AnyCPU /p:OutputPath=bin\Release\ /verbosity:minimal
if %errorlevel% neq 0 (
    echo.
    echo ❌ 编译失败！
    echo.
    echo 可能的解决方案：
    echo 1. 检查是否安装了所需的 .NET Framework 版本
    echo 2. 使用 Visual Studio 打开项目进行编译
    echo 3. 检查项目依赖是否完整
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo           🎉 编译成功！
echo ========================================
echo.
echo 📁 可执行文件位置：
echo    ToastFish\bin\Release\ToastFish.exe
echo.
echo 📋 编译信息：
echo    - 配置：Release
echo    - 平台：AnyCPU
echo    - .NET Framework：4.7.2
echo.
echo 🚀 运行程序：
echo    双击 ToastFish\bin\Release\ToastFish.exe
echo.
echo 📖 使用说明：
echo    1. 首次运行会在系统托盘显示图标
echo    2. 右键托盘图标可进行各种设置
echo    3. 详细使用方法请查看 README.md
echo.

set /p choice="是否立即运行程序？(Y/N): "
if /i "%choice%"=="Y" (
    echo.
    echo 🚀 启动 ToastFish...
    start "" "ToastFish\bin\Release\ToastFish.exe"
    echo.
    echo 程序已启动，请查看系统托盘！
) else (
    echo.
    echo 💡 提示：您可以随时双击 ToastFish\bin\Release\ToastFish.exe 运行程序
)

echo.
echo 按任意键退出...
pause >nul
