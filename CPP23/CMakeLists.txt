# OpenRA C++23 分模块移植 / OpenRA C++23 incremental port
#
# 工具链约定（跨平台一致）：统一使用 LLVM Clang 的 GNU ABI 目标——
# Toolchain convention (cross-platform consistent): always LLVM Clang targeting the GNU ABI.
#   Windows: llvm-mingw 的 clang++（x86_64-w64-windows-gnu，libc++，Itanium ABI）
#            llvm-mingw clang++ (self-contained, libc++, Itanium ABI)
#   Linux:   系统 clang++（目标天然相同）/ system clang++ (identical target)
# 这样避免了 MSVC ABI（名称修饰/异常/std 模板行为差异），三平台编译产物行为一致。
# This avoids the MSVC ABI (mangling/exception/std template differences) for consistent behavior.
#
# 构建 / Build:
#   cmake -B build -G Ninja -DCMAKE_BUILD_TYPE=Debug
#          -DCMAKE_CXX_COMPILER=<llvm-mingw 的 clang++ 路径 / path to clang++>
#   cmake --build build
#   ctest --test-dir build --output-on-failure
cmake_minimum_required(VERSION 3.24)
project(openra_cpp23 LANGUAGES CXX)

if(NOT CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    message(FATAL_ERROR "This project requires LLVM Clang (GNU ABI target, e.g. llvm-mingw on Windows).")
endif()

if(WIN32 AND NOT CMAKE_CXX_COMPILER_ARCHITECTURE_ID STREQUAL "")
    # 附加保护：MSVC-ABI 的官方 clang 会定义 _MSC_VER——拒绝以防误用
    # Extra guard: MSVC-ABI clang defines _MSC_VER - refuse it to prevent accidental use
    if(CMAKE_CXX_SIMULATE_ID STREQUAL "MSVC")
        message(FATAL_ERROR "MSVC-ABI clang detected. Use llvm-mingw clang++ (GNU ABI) instead.")
    endif()
endif()

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# 错误处理策略：严禁异常 —— 全部错误经 std::expected<T, YamlError> 传播
# Error-handling policy: exceptions are forbidden - all errors propagate via std::expected
add_compile_options(-Wall -Wextra -Wpedantic -fno-exceptions)

add_library(openra_yaml
    src/MiniYaml.cpp
)
target_include_directories(openra_yaml PUBLIC include)

enable_testing()
add_executable(openra_yaml_tests tests/test_main.cpp)
target_link_libraries(openra_yaml_tests PRIVATE openra_yaml)
add_test(NAME yaml_tests COMMAND openra_yaml_tests)
