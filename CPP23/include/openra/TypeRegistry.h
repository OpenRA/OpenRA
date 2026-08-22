// TypeRegistry —— 字符串类型名到工厂的注册表（C++23 移植版）
// TypeRegistry - string type-name to factory registry (C++23 port)
//
// 对应 C# ObjectCreator.CreateObject<T>(string) 的反射工厂能力：
// Replaces C# ObjectCreator.CreateObject<T>(string) reflection factories:
// mod YAML 中 "Health: ..." 之类的键是 trait 类型名，运行时按名实例化。
// mod YAML keys like "Health: ..." are trait type names, instantiated by name at runtime.
//
// C++ 无运行时反射，用显式注册宏替代（每个类型一行，链接期收集）：
// C++ has no runtime reflection, so explicit registration macros replace it
// (one line per type, collected at link time):
//
//   struct HealthInfo : TraitInfo { ... };
//   OPENRA_REGISTER_TYPE(TraitInfo, HealthInfo)
//
// 末尾无须分号亦可（宏内含分号）/ the trailing semicolon is optional (macro includes one)
// 待 C++26 静态反射普后可与 openra_fields 一起自动化
// Automatable together with openra_fields once C++26 reflection is ubiquitous

#pragma once

#include <functional>
#include <map>
#include <memory>
#include <string>
#include <string_view>
#include <type_traits>
#include <vector>

namespace OpenRA
{
	// 每个基类一份注册表 / one registry per base class
	template<class Base>
	class TypeRegistry
	{
	public:
		using Factory = std::function<std::unique_ptr<Base>()>;

		// 注册类型（重名返回 false）/ register a type (false on duplicate names)
		template<class T>
		static bool add(std::string name)
		{
			static_assert(std::is_base_of_v<Base, T>, "T must derive from the registry Base");
			return registry().emplace(std::move(name), [] { return std::make_unique<T>(); }).second;
		}

		// 按名创建 / create by name
		static std::unique_ptr<Base> create(std::string_view name)
		{
			auto& r = registry();
			auto it = r.find(std::string(name));
			if (it == r.end())
				return nullptr;
			return it->second();
		}

		// 已注册类型名列表（lint/文档用）/ registered names (for linting/docs)
		static std::vector<std::string> names()
		{
			std::vector<std::string> result;
			for (const auto& [k, v] : registry())
				result.push_back(k);
			return result;
		}

		static bool contains(std::string_view name)
		{
			return registry().count(std::string(name)) > 0;
		}

	private:
		// 内联单例：头文件包含安全 / inline singleton: header-include safe
		static std::map<std::string, Factory>& registry()
		{
			static std::map<std::string, Factory> instance;
			return instance;
		}
	};
}

// 静态初始化注册；匿名 namespace 防止重名冲突，addr_taken 保证不被链接器丢弃
// Static-init registration; anonymous namespace avoids collisions, and the
// volatile address-take keeps the linker from discarding the initializer
#define OPENRA_REGISTER_TYPE(Base, T) \
	namespace \
	{ \
		struct OpenRaTypeRegistrar_##T \
		{ \
			OpenRaTypeRegistrar_##T() { OpenRA::TypeRegistry<Base>::add<T>(#T); } \
		}; \
		volatile auto OpenRaTypeRegistrarInstance_##T = OpenRaTypeRegistrar_##T{}; \
	}
