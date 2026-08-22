// FieldLoader —— YAML 值到 C++ 字段的注入（C++23 移植版）
// FieldLoader - injecting YAML values into C++ fields (C++23 port)
//
// 对应 OpenRA.Game/FieldLoader.cs 的核心能力：
// Mirrors the core of OpenRA.Game/FieldLoader.cs:
//   - GetValue<T>：字符串到类型的解析（数字/布尔/字符串/向量）
//     GetValue<T>: string-to-type parsing (numbers/bools/strings/vectors)
//   - Load：按 openra_fields 元数据把 MiniYaml 子节点注入对象
//     Load: inject MiniYaml child nodes into objects via openra_fields metadata
//   - 未知字段回调（lint 用）/ unknown-field callback (for linting)
//
// 与 C# 版的差异 / Differences from the C# version:
//   - 严禁异常：C# 抛 FieldLoadException 的路径一律返回 std::expected<T, YamlError>
//     No exceptions allowed: paths that throw FieldLoadException in C# return
//     std::expected<T, YamlError> instead (enforced by -fno-exceptions in the build)
//   - C# 按 Type 动态分发；C++ 用 if constexpr 编译期分发
//     C# dispatches on Type at runtime; C++ dispatches via if constexpr at compile time
//   - 首版覆盖核心类型（整型/浮点/布尔/string/vector<T>/嵌套结构体）
//     The first version covers core types (integrals/floats/bool/string/vector<T>/nested structs)
//   - 游戏专用类型（WDist/WAngle/Color 等）随后续模块移植接入
//     Game-specific types (WDist/WAngle/Color, ...) plug in as later modules are ported

#pragma once

#include "openra/Fields.h"
#include "openra/MiniYaml.h"

#include <charconv>
#include <expected>
#include <functional>
#include <iostream>
#include <map>
#include <optional>
#include <string>
#include <string_view>
#include <type_traits>
#include <vector>

namespace OpenRA
{
	namespace FieldLoader
	{
		// 相互递归（嵌套结构），在本命名空间内前向声明
		// Mutually recursive (nested structs); forward-declared inside this namespace
		template<class T>
		std::expected<void, YamlError> load(T& obj, const MiniYaml& yaml);

		// 未知字段处理器（对应 C# UnknownFieldAction）/ unknown field handler (C# UnknownFieldAction)
		inline std::function<void(std::string_view, std::string_view)> unknownFieldAction =
			[](std::string_view field, std::string_view type)
			{
				std::cerr << "FieldLoader: Unknown field `" << field << "` on `" << type << "`\n";
			};

		namespace Detail
		{
			template<class T>
			struct IsVector : std::false_type { };
			template<class E, class A>
			struct IsVector<std::vector<E, A>> : std::true_type { };

			inline void splitSpaces(std::string_view value, std::vector<std::string_view>& parts)
			{
				size_t start = 0;
				while (start <= value.size())
				{
					auto pos = value.find(' ', start);
					if (pos == std::string_view::npos)
					{
						if (start < value.size())
							parts.emplace_back(value.substr(start));
						break;
					}

					if (pos > start)
						parts.emplace_back(value.substr(start, pos - start));
					start = pos + 1;
				}
			}

			// 标量解析：整型/浮点/bool 走字符转换，其余特化处理
			// Scalar parsing: integrals/floats via from_chars; the rest is specialized
			template<class T>
			std::expected<T, YamlError> parseScalar(std::string_view field, std::string_view value)
			{
				if constexpr (std::is_same_v<T, bool>)
				{
					if (value == "true") return true;
					if (value == "false") return false;
					return std::unexpected(yamlError("FieldLoader: Cannot parse `" + std::string(value)
						+ "` as bool for field `" + std::string(field) + "`"));
				}
				else if constexpr (std::is_integral_v<T> || std::is_floating_point_v<T>)
				{
					T result{};
					auto [ptr, ec] = std::from_chars(value.data(), value.data() + value.size(), result);
					if (ec != std::errc{})
						return std::unexpected(yamlError("FieldLoader: Cannot parse `" + std::string(value)
							+ "` as " + (std::is_integral_v<T> ? "integer" : "float")
							+ " for field `" + std::string(field) + "`"));
					return result;
				}
				else
				{
					static_assert(!sizeof(T), "Unsupported scalar type; add a specialization");
				}
			}
		}

		// 值解析入口 / value parsing entry point
		// 支持：标量、std::string、std::vector<E>（空格分隔）、声明了 openra_fields 的嵌套结构
		// Supports: scalars, std::string, std::vector<E> (space separated), nested structs with openra_fields
		template<class T>
		std::expected<T, YamlError> getValue(std::string_view field, const MiniYaml& yaml)
		{
			if constexpr (HasFieldsV<T>)
			{
				// 嵌套结构体从子节点加载 / nested structs load from child nodes
				T result{};
				auto ok = load(result, yaml);
				if (!ok.has_value())
					return std::unexpected(std::move(ok).error());
				return result;
			}
			else if constexpr (std::is_same_v<T, std::string>)
			{
				return yaml.value ? *yaml.value : std::string{};
			}
			else if constexpr (Detail::IsVector<T>::value)
			{
				using E = typename T::value_type;
				T result;
				if (yaml.value && !yaml.value->empty())
				{
					std::vector<std::string_view> parts;
					Detail::splitSpaces(*yaml.value, parts);
					result.reserve(parts.size());
					for (const auto& p : parts)
					{
						auto element = Detail::parseScalar<E>(field, p);
						if (!element.has_value())
							return std::unexpected(std::move(element).error());
						result.push_back(std::move(*element));
					}
				}

				return result;
			}
			else
			{
				return Detail::parseScalar<T>(field, yaml.value ? *yaml.value : std::string_view{});
			}
		}

		// 对象加载：遍历 openra_fields，按名匹配子节点注入
		// Object loading: walk openra_fields, injecting matching child nodes by name
		template<class T>
		std::expected<void, YamlError> load(T& obj, const MiniYaml& yaml)
		{
			static_assert(HasFieldsV<T>, "Type must declare openra_fields metadata");

			// 先按 key 建索引（重复键返回错误）/ index by key first (duplicates surface as errors)
			std::map<std::string, const MiniYaml*> md;
			for (const auto& n : yaml.nodes)
			{
				if (!n.key)
					continue;
				if (!md.emplace(*n.key, n.value.get()).second)
					return std::unexpected(yamlError("Duplicate key '" + *n.key + "' in " + n.location.name
						+ ":" + std::to_string(n.location.line)));
			}

			// forEachField 的 visitor 没有早退通道，首个错误记录后继续（后续错误忽略）
			// forEachField's visitor has no early-exit channel; record the first error and continue
			std::optional<YamlError> firstError;
			forEachField(obj, [&](std::string_view name, auto& member) {
				auto it = md.find(std::string(name));
				if (it == md.end())
					return; // 缺省字段保留默认值 / absent fields keep their defaults

				using Member = std::remove_reference_t<decltype(member)>;
				auto parsed = getValue<Member>(name, *it->second);
				if (parsed.has_value())
					member = std::move(*parsed);
				else if (!firstError.has_value())
					firstError = std::move(parsed).error();
			});

			if (firstError.has_value())
				return std::unexpected(std::move(*firstError));

			// 未知字段上报（对应 C# 的 UnknownFieldAction）/ report unknown fields
			for (const auto& [k, v] : md)
			{
				if (!hasField<T>(k))
					unknownFieldAction(k, "?");
			}

			return {};
		}

		// 便捷形式 / convenience form
		template<class T>
		std::expected<T, YamlError> load(const MiniYaml& yaml)
		{
			T result{};
			auto ok = load(result, yaml);
			if (!ok.has_value())
				return std::unexpected(std::move(ok).error());
			return result;
		}
	}
}
