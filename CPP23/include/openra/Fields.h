// 字段元数据 —— 替代 C# 反射的编译期字段描述
// Field metadata - compile-time field descriptions replacing C# reflection
//
// 对应 C# 侧 FieldLoader.GetTypeLoadInfo(Type)（反射枚举字段）的能力。
// Replaces what C# does via FieldLoader.GetTypeLoadInfo(Type) reflection.
//
// 用法 / Usage:
//   struct WeaponInfo
//   {
//       int Damage;
//       float Range;
//       static constexpr auto openra_fields = std::tuple{
//           OpenRA::field("Damage", &WeaponInfo::Damage),
//           OpenRA::field("Range", &WeaponInfo::Range),
//       };
//   };
//
// YAML 字段名与 C++ 成员名可以不同（对应 C# 的 YamlName 特性）
// YAML names may differ from C++ member names (mirroring C#'s YamlName attribute)
//
// 待 C++26 静态反射在三大编译器普及后，可改为自动枚举聚合字段并移除这些声明
// Once C++26 static reflection is ubiquitous these declarations can be replaced
// by automatic aggregate field enumeration

#pragma once

#include <functional>
#include <string_view>
#include <tuple>
#include <type_traits>

namespace OpenRA
{
	// 字段描述：名字 + 成员指针 / field descriptor: name plus member pointer
	template<class C, class M>
	struct FieldDesc
	{
		std::string_view name;
		M C::* ptr;
	};

	template<class C, class M>
	constexpr FieldDesc<C, M> field(std::string_view name, M C::* ptr)
	{
		return { name, ptr };
	}

	// 判断类型是否声明了字段元数据 / whether a type declares field metadata
	template<class T, class = void>
	struct HasFields : std::false_type { };

	template<class T>
	struct HasFields<T, std::void_t<decltype(T::openra_fields)>> : std::true_type { };

	template<class T>
	inline constexpr bool HasFieldsV = HasFields<T>::value;

	namespace FieldsDetail
	{
		// 成员指针的类与值类型 / class and value types of a member pointer
		template<class T>
		struct MemberTraits;
		template<class C, class M>
		struct MemberTraits<M C::*> { using Class = C; using Member = M; };
	}

	// 遍历字段：visitor(std::string_view name, Member& value)
	// Visit fields: visitor(std::string_view name, Member& value)
	template<class T, class Visitor>
	void forEachField(T&& obj, Visitor&& visitor)
	{
		using Plain = std::remove_reference_t<T>;
		static_assert(HasFieldsV<Plain>, "Type must declare openra_fields metadata");
		std::apply([&](const auto&... f) {
			(visitor(f.name, obj.*(f.ptr)), ...);
		}, Plain::openra_fields);
	}

	// 字段数 / field count
	template<class T>
	constexpr size_t fieldCount()
	{
		static_assert(HasFieldsV<T>, "Type must declare openra_fields metadata");
		return std::tuple_size_v<decltype(T::openra_fields)>;
	}

	// 查找字段名是否存在 / whether a field with the given name exists
	template<class T>
	bool hasField(std::string_view name)
	{
		bool found = false;
		std::apply([&](const auto&... f) {
			((found = found || f.name == name), ...);
		}, T::openra_fields);
		return found;
	}
}
