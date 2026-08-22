@echo off
rem CMake + LLVM Clang（GNU ABI / llvm-mingw）构建脚本
rem CMake + LLVM Clang (GNU ABI / llvm-mingw) build script
rem 用法 / Usage: build.cmd [test]
setlocal
cd /d "%~dp0"

rem llvm-mingw 是 LLVM 官方维护的自包含工具链（clang + libc++ + mingw-w64，GNU ABI）
rem llvm-mingw is LLVM's self-contained toolchain (clang + libc++ + mingw-w64, GNU ABI)
set "CLANG=%LOCALAPPDATA%\Microsoft\WinGet\Packages\MartinStorsjo.LLVM-MinGW.UCRT_Microsoft.Winget.Source_8wekyb3d8bbwe\llvm-mingw-20260519-ucrt-x86_64\bin\clang++.exe"
if not exist "%CLANG%" set CLANG=clang++

cmake -B build -G Ninja -DCMAKE_BUILD_TYPE=Debug -DCMAKE_CXX_COMPILER="%CLANG%" || exit /b 1
cmake --build build || exit /b 1

if "%~1"=="test" (
    ctest --test-dir build --output-on-failure || exit /b 1
)
endlocal
