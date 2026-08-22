# OpenRA C++23 分模块移植 / OpenRA Incremental C++23 Port

按模块把引擎从 C#/.NET 移植到 C++23。本目录独立于 C# 代码树构建，与主工程互不影响。
Incrementally ports the engine from C#/.NET to C++23. This directory builds independently of the C# tree and does not affect the main project.

## 工具链 / Toolchain

CMake + Ninja + **LLVM Clang（GNU ABI）**，跨平台统一：
CMake + Ninja + **LLVM Clang (GNU ABI)**, consistent across platforms:

- Windows：[llvm-mingw](https://github.com/mstorsjo/llvm-mingw) 的 clang++（自包含：clang + libc++ + mingw-w64，Itanium ABI）
  Windows: clang++ from [llvm-mingw](https://github.com/mstorsjo/llvm-mingw) (self-contained: clang + libc++ + mingw-w64, Itanium ABI)
- Linux：系统 clang++（目标天然一致）
  Linux: the system clang++ (naturally the same target)

刻意不使用 MSVC-ABI 的官方 clang 发行版：名称修饰、异常与 std 模板行为差异会破坏三平台一致性。
The MSVC-ABI official clang distribution is deliberately avoided: mangling, exception and std template behavior differences would break three-platform consistency.

```sh
# 构建 / Build
cmake -B build -G Ninja -DCMAKE_BUILD_TYPE=Debug -DCMAKE_CXX_COMPILER=<clang++ 路径 / path to clang++>
cmake --build build

# 测试（第二个参数指向仓库根，用于真实 mod 文件冒烟测试）
# Tests (arg 2 points at the repo root for real-mod smoke tests)
./build/openra_yaml_tests.exe ..
```

Windows 下可直接运行 `build.cmd [test]`。
On Windows just run `build.cmd [test]`.

## 移植进度总览 / Port Status Overview

模块规模按 C# 侧文件数/行数标注，作为工作量参考。
Module sizes are annotated with the C#-side file/line counts as an effort reference.

### ✅ 已完成 / Completed

| 模块 / Module | 内容 / Content | 位置 / Location | 对应 C# / C# equivalent |
|---|---|---|---|
| MiniYaml 解析与合并 / parsing & merging | YAML 子集解析、`Inherits@` 继承、`-Key` 删除、多源合并、序列化 / YAML-subset parsing, `Inherits@` inheritance, `-Key` removal, multi-source merging, serialization | `include/openra/MiniYaml.h` + `src/MiniYaml.cpp` | `OpenRA.Game/MiniYaml.cs`（786 行，逐段保真翻译 / faithful section-by-section port） |
| 字段绑定 / field binding | 编译期字段元数据（成员指针，零宏）/ compile-time field metadata (member pointers, no macros) | `include/openra/Fields.h` | `FieldLoader.GetTypeLoadInfo` 的反射枚举 / its reflection-based field enumeration |
| 值注入 / value injection | 标量/string/vector/嵌套结构的 YAML→对象注入，坏值与未知字段处理 / YAML→object injection for scalars/string/vector/nested structs, invalid-value and unknown-field handling | `include/openra/FieldLoader.h` | `FieldLoader.cs` 核心（910 行）/ the core of `FieldLoader.cs` |
| 类型工厂 / type factory | 类型名→工厂注册（`OPENRA_REGISTER_TYPE` 宏，替代运行时反射）/ type-name → factory registration (macro, replacing runtime reflection) | `include/openra/TypeRegistry.h` | `ObjectCreator.CreateObject` 反射工厂 / its reflection factory |
| 构建与测试 / build & tests | CMake/Ninja 配置、37 项断言测试、真实 mod 文件冒烟 / CMake/Ninja setup, 37 assertion tests, real-mod smoke tests | `CMakeLists.txt`、`tests/test_main.cpp` | — |

方言行为与 C# 版一致：4 空格或 1 tab 一级缩进、`#` 注释（`\#` 转义）、反斜杠空白保护、
同父重复继承报错、弱删除语义。
Dialect behavior matches the C# version: 4 spaces or 1 tab per indent level, `#` comments
(`\#` escaped), backslash whitespace guards, duplicate-inheritance errors, weak removal semantics.

### ⬜ 待移植 / Pending

按依赖顺序分四个阶段，每阶段内条目可并行。
Four phases in dependency order; items within a phase can proceed in parallel.

**阶段一：基础层（引擎的可移植地基）/ Phase 1: Foundation**

| 模块 / Module | 对应 C# / C# equivalent | 规模 / Size | 备注 / Notes |
|---|---|---|---|
| 游戏数值类型 / game value types | `Primitives/`（WDist/WAngle/WPos/int2/float2/Color/Size/Rectangle 等） | ~30 文件 | 接入 `FieldLoader` 特化后规则数据即可完整加载 / plug into `FieldLoader` specializations to complete rule-data loading |
| 支持设施 / support utilities | `Support/`（Log/PerfTimer/Arguments）+ 根目录 Exts | ~16 文件 | 日志先行，后续模块依赖 / logging first, other modules depend on it |
| 文件系统 / file system | `FileSystem/`（Mix 包/Folder/Zip） | ~4 文件 + 格式 | Mix 解析是 mod 加载的前提 / Mix parsing is a prerequisite for mod loading |
| 规则装配入口 / rules assembly entry | `MiniYaml.Load(filesystem, files, mapRules)` 多文件合并 | — | 已有 Merge，补文件系统遍历 / Merge exists; add filesystem traversal |

**阶段二：引擎核心 / Phase 2: Engine Core**

| 模块 / Module | 对应 C# / C# equivalent | 规模 / Size | 备注 / Notes |
|---|---|---|---|
| Actor/Trait 系统 / actor-trait system | `World.cs`/`Actor.cs`/`TraitDictionary.cs` + 规则加载 | ~49 根文件 | 直接复用 TypeRegistry/Fields / reuses TypeRegistry/Fields directly |
| 平台层 / platform layer | `Platforms.Default`（窗口/输入/上下文，SDL3 + OpenGL 或 SDL_GPU） | ~18 文件 | C# 侧 SDL3 迁移经验（全屏/线程亲和坑）已有沉淀 / carries over the SDL3 fullscreen/thread-affinity lessons |
| 图形渲染 / rendering | `Graphics/`（Renderer/Sprite/Sheet/Palette/着色器抽象） | ~37 文件 | 最大单体；管线模型选型（GL 状态机 vs SDL_GPU）在此定 / biggest single piece; GL state-machine vs SDL_GPU decision happens here |
| Widget 框架 / widget framework | `Widgets/` 基类 + `WidgetLoader`（YAML 布局） | 框架 ~10 文件 | 布局加载复用 Fields/FieldLoader / layout loading reuses Fields/FieldLoader |
| 网络与同步 / networking & sync | `Network/`（OrderManager/Order 序列化/锁步） | ~17 文件 | Order 序列化需逐条接入字段元数据 / Order serialization wires into field metadata |
| 声音 / audio | `Sound/` + OpenAL | ~2 文件 | openal-soft 原生库直连 / direct openal-soft usage |
| 本地化 / localization | Fluent（Linguini） | — | C++ 实现薄弱，需自研子集 / weak C++ ecosystem; a subset must be built |
| Lua 脚本 / Lua scripting | `Scripting/`（Eluant） | ~7 文件 | sol2/LuaJit 替代 / replaced by sol2/LuaJit |
| 地图格式 / map format | `Map/` | ~19 文件 | 依赖文件系统与数值类型 / depends on the file system and value types |

**阶段三：游戏内容 / Phase 3: Game Content**

| 模块 / Module | 对应 C# / C# equivalent | 规模 / Size | 备注 / Notes |
|---|---|---|---|
| 通用 traits / common traits | `Mods.Common/Traits/` | ~501 文件 | 量最大；机械翻译 + 字段标注 / the bulk; mechanical porting plus field annotation |
| 通用 widgets / common widgets | `Mods.Common/Widgets/` | ~192 文件 | 依赖 Widget 框架 / depends on the widget framework |
| Cnc/D2k 特化 / Cnc & D2k specifics | `Mods.Cnc` + `Mods.D2k` | ~163 文件 | 最后收尾 / final stretch |

**阶段四：外围 / Phase 4: Periphery**

| 模块 / Module | 对应 C# / C# equivalent | 备注 / Notes |
|---|---|---|
| 工具 / utility | `Utility`（地图/lint/资源工具） | lint 依赖 trait 注册完整性 / linting depends on complete trait registration |
| 专用服务器 / dedicated server | `Server`（~124 行入口 + `Game/Server`） | 依赖网络同步层 / depends on the networking layer |
| 启动器与打包 / launcher & packaging | `WindowsLauncher` + 各平台安装包 | CMake 安装目标重做 / redo as CMake install targets |

## C++ 侧替代反射的写法 / Replacing reflection on the C++ side

```cpp
struct WeaponInfo
{
    int damage = 0;
    float range = 0;
    static constexpr auto openra_fields = std::tuple{
        OpenRA::field("Damage", &WeaponInfo::damage),   // YAML 名可与成员名不同 / YAML name may differ from the member name
        OpenRA::field("Range", &WeaponInfo::range),
    };
};

// 注册工厂（替代 ObjectCreator 的反射查找）
// Register a factory (replaces ObjectCreator's reflective lookup)
OPENRA_REGISTER_TYPE(TraitInfo, HealthInfo)

// 从合并后的规则树实例化并注入
// Instantiate from the merged rules tree and inject values
auto trait = TypeRegistry<TraitInfo>::create("HealthInfo");
auto* hi = dynamic_cast<HealthInfo*>(trait.get());
FieldLoader::load(*hi, *node.value);
```

待 C++26 静态反射在三大编译器普及后，`openra_fields` 声明可改为自动枚举，业务代码不变。
Once C++26 static reflection is available in all three major compilers, the `openra_fields`
declarations can be replaced by automatic enumeration without touching business code.

## 测试 / Tests

`tests/test_main.cpp` 覆盖：解析（缩进/注释/转义/tab/坏缩进）、合并继承删除、重复继承异常、
序列化回环、字段注入（含缺省/错误值）、类型注册，以及 `mods/ra/rules` 下全部真实 YAML 的冒烟解析。
`tests/test_main.cpp` covers: parsing (indentation/comments/escapes/tabs/bad indents), merge/inherit/removal,
duplicate-inheritance errors, serialization round-trips, field injection (defaults and invalid values),
type registration, plus smoke-parsing every real YAML file under `mods/ra/rules`.
