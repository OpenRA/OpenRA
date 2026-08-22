
// MiniYaml 移植验证测试 / MiniYaml port verification tests
// 覆盖：解析（缩进/注释/转义）、合并继承删除、序列化回环、真实 mod 文件解析
// Covers: parsing (indent/comments/escapes), merge/inherit/removal, round-trip, real mod files

#include "openra/MiniYaml.h"
#include "openra/FieldLoader.h"
#include "openra/TypeRegistry.h"

#include <cstdio>
#include <filesystem>
#include <iostream>

using namespace OpenRA;

namespace
{
	int failures = 0;

	void check(bool ok, const std::string& what)
	{
		if (!ok)
		{
			std::cerr << "FAIL: " << what << "\n";
			++failures;
		}
		else
			std::cout << "ok: " << what << "\n";
	}

	void testParsing()
	{
		const auto text = R"(# top comment
player: World
	Health: 100
	Name: Generic Value
	Speed: 7
)";
		auto nodes = miniYamlFromString(text, "test");
		check(nodes.size() == 1, "single top-level node");
		check(nodes[0].key && *nodes[0].key == "player", "key parsed");
		check(nodes[0].value->value && *nodes[0].value->value == "World", "value parsed");
		check(nodes[0].value->nodes.size() == 3, "three children");
		check(*nodes[0].value->nodes[0].key == "Health"
			&& *nodes[0].value->nodes[0].value->value == "100", "child key/value");
		check(*nodes[0].value->nodes[1].value->value == "Generic Value", "value with spaces preserved");
		check(nodes[0].location.line == 2, "source location line");
	}

	void testEscapes()
	{
		// '#' 需转义；注释以未转义 '#' 开始
		// '#' must be escaped; comments start at an unescaped '#'
		const auto text = R"(color: value\#withhash # real comment
tag: \ spaced \
)";
		auto nodes = miniYamlFromString(text, "test", false);
		check(nodes.size() == 2, "two nodes");
		check(*nodes[0].value->value == "value#withhash", "escaped hash restored");
		check(nodes[0].comment && *nodes[0].comment == " real comment", "comment captured");
		check(*nodes[1].value->value == " spaced ", "backslash whitespace guards preserved");
	}

	void testTabIndent()
	{
		const auto text = "a:\n\tb: 1\n\t\tc: 2\n";
		auto nodes = miniYamlFromString(text, "test");
		check(nodes.size() == 1, "tab: single root");
		check(nodes[0].value->nodes.size() == 1, "tab: one child");
		check(nodes[0].value->nodes[0].value->nodes.size() == 1, "tab: one grandchild");
		check(*nodes[0].value->nodes[0].value->nodes[0].key == "c", "tab: grandchild key");
	}

	void testBadIndent()
	{
		bool threw = false;
		try
		{
			miniYamlFromString("a:\n\t\tb: 1\n", "test");
		}
		catch (const YamlException&)
		{
			threw = true;
		}

		check(threw, "bad indent throws");
	}

	void testMergeInheritRemoval()
	{
		const auto base = R"(^base:
	Inherits: ^common
	HP: 10
	Traits:
		A: 1
		B: 2
^common:
	HP: 1
	Traits:
		A: 9
^extra:
	Speed: 99
)";
		const auto override = R"(^base:
	Inherits@extra: ^extra
	HP: 20
	Traits:
		-B
)";
		auto baseNodes = miniYamlFromString(base, "base");
		auto overrideNodes = miniYamlFromString(override, "override");
		auto merged = miniYamlMerge({ baseNodes, overrideNodes });

		check(merged.size() == 3, "merge: three top-level entries");
		auto dict = merged[0].value->toDictionary();
		check(dict.count("HP") && *dict.at("HP")->value == "20", "merge: override wins");
		check(dict.count("Speed") && *dict.at("Speed")->value == "99", "inherit@extra applied");
		auto traits = dict.at("Traits")->toDictionary();
		check(traits.count("A") && *traits.at("A")->value == "1", "inherit: A survives from base");
		check(!traits.count("B"), "removal: B removed");
	}

	void testDuplicateInheritThrows()
	{
		// C# 语义：同一父类型不可被继承两次（防循环/冗余）
		// C# semantics: a parent may not be inherited twice (loop/redundancy guard)
		const auto text = R"(^a:
	Inherits: ^common
	Inherits@dup: ^common
^common:
	X: 1
)";
		auto nodes = miniYamlFromString(text, "t");
		bool threw = false;
		try
		{
			miniYamlMerge({ nodes });
		}
		catch (const YamlException&)
		{
			threw = true;
		}

		check(threw, "duplicate inheritance throws");
	}

	void testRoundTrip()
	{
		const auto text = "a: 1\n\tb: hello world\n\t\tc: deep\n";
		auto nodes = miniYamlFromString(text, "test");
		auto out = miniYamlToString(nodes);
		auto reparsed = miniYamlFromString(out, "roundtrip");
		auto out2 = miniYamlToString(reparsed);
		check(out == out2, "round-trip stable");
		check(reparsed.size() == 1, "round-trip: same node count");
		check(*reparsed[0].value->nodes[0].value->nodes[0].key == "c", "round-trip: deep child");
	}

	void testRealModFile(const std::filesystem::path& engineRoot)
	{
		// 用真实 mod 规则文件做冒烟解析 / smoke-parse real mod rule files
		auto rules = engineRoot / "mods" / "ra" / "rules";
		if (!std::filesystem::exists(rules))
		{
			std::cout << "skip: real mod files not found\n";
			return;
		}

		int parsed = 0;
		for (const auto& entry : std::filesystem::recursive_directory_iterator(rules))
		{
			if (entry.path().extension() != ".yaml")
				continue;
			try
			{
				auto nodes = miniYamlFromFile(entry.path().string());
				parsed++;
				for (const auto& n : nodes)
					if (!n.key)
						throw YamlException("null key without comment retention");
			}
			catch (const std::exception& e)
			{
				check(false, "parse " + entry.path().string() + ": " + e.what());
				return;
			}
		}

		check(parsed > 0, "real mod files parsed (" + std::to_string(parsed) + " files)");
	}
}

namespace
{
	// 测试用字段化结构 / field-annotated test structs
	struct WeaponInfo
	{
		int damage = 0;
		float range = 0;
		static constexpr auto openra_fields = std::tuple{
			OpenRA::field("Damage", &WeaponInfo::damage),
			OpenRA::field("Range", &WeaponInfo::range),
		};
	};

	struct TowerInfo
	{
		std::string name;
		bool active = false;
		int level = 1;
		std::vector<int> offsets;
		WeaponInfo weapon;
		static constexpr auto openra_fields = std::tuple{
			OpenRA::field("Name", &TowerInfo::name),
			OpenRA::field("Active", &TowerInfo::active),
			OpenRA::field("Level", &TowerInfo::level),
			OpenRA::field("Offsets", &TowerInfo::offsets),
			OpenRA::field("Weapon", &TowerInfo::weapon),
		};
	};

	// 类型注册测试基类与两个派生 / registry test base and two derived types
	struct TraitInfo
	{
		virtual ~TraitInfo() = default;
	};

	struct HealthInfo : TraitInfo
	{
		int hp = 0;
		static constexpr auto openra_fields = std::tuple{
			OpenRA::field("HP", &HealthInfo::hp),
		};
	};

	struct SpeedInfo : TraitInfo { };
}

OPENRA_REGISTER_TYPE(TraitInfo, HealthInfo)
OPENRA_REGISTER_TYPE(TraitInfo, SpeedInfo)

namespace
{
	void testFieldLoader()
	{
		const auto text = R"(Name: GuardTower
Active: true
Offsets: 1 2 3
Weapon:
	Damage: 50
	Range: 7.5
)";
		auto nodes = miniYamlFromString(text, "fields");
		// 顶层各字段为平级节点，包一层根容器再加载 / top-level fields are siblings; wrap in a root
		auto tower = FieldLoader::load<TowerInfo>(MiniYaml(std::nullopt, std::move(nodes)));

		check(tower.name == "GuardTower", "field: string loaded");
		check(tower.active, "field: bool loaded");
		check(tower.level == 1, "field: absent field keeps default");
		check(tower.offsets == std::vector<int>({ 1, 2, 3 }), "field: vector loaded");
		check(tower.weapon.damage == 50, "field: nested struct loaded");
		check(tower.weapon.range > 7.49f && tower.weapon.range < 7.51f, "field: float loaded");
	}

	void testFieldLoaderErrors()
	{
		bool threw = false;
		try
		{
			const auto text = "Damage: fifty\n";
			auto nodes = miniYamlFromString(text, "bad");
			auto w = FieldLoader::load<WeaponInfo>(MiniYaml(std::nullopt, std::move(nodes)));
		}
		catch (const FieldLoadException&)
		{
			threw = true;
		}

		check(threw, "field: bad numeric value throws");

		threw = false;
		try
		{
			const auto text = "Active: yes\n";
			auto nodes = miniYamlFromString(text, "bad");
			auto t = FieldLoader::load<TowerInfo>(MiniYaml(std::nullopt, std::move(nodes)));
		}
		catch (const FieldLoadException&)
		{
			threw = true;
		}

		check(threw, "field: bad bool value throws");
	}

	void testTypeRegistry()
	{
		// OPENRA_REGISTER_TYPE 宏静态注册的工厂 / factories registered statically by the macro
		auto health = TypeRegistry<TraitInfo>::create("HealthInfo");
		check(health != nullptr, "registry: create by name");
		check(dynamic_cast<HealthInfo*>(health.get()) != nullptr, "registry: correct dynamic type");

		check(TypeRegistry<TraitInfo>::create("NoSuchInfo") == nullptr, "registry: unknown name yields null");
		check(TypeRegistry<TraitInfo>::contains("SpeedInfo"), "registry: second type registered");

		// 注册类型 + 字段加载组合：模拟 trait 的 YAML 实例化
		// Registry + field loading combined: simulates YAML trait instantiation
		const auto text = R"(HealthInfo:
	HP: 123
)";
		auto nodes = miniYamlFromString(text, "traits");
		auto trait = TypeRegistry<TraitInfo>::create(*nodes[0].key);
		check(trait != nullptr, "registry: yaml key drives creation");
		auto* hi = dynamic_cast<HealthInfo*>(trait.get());
		FieldLoader::load(*hi, *nodes[0].value);
		check(hi->hp == 123, "registry+fields: yaml value injected");
	}
}

int main(int argc, char** argv)
{
	testParsing();
	testEscapes();
	testTabIndent();
	testBadIndent();
	testMergeInheritRemoval();
	testDuplicateInheritThrows();
	testRoundTrip();
	testFieldLoader();
	testFieldLoaderErrors();
	testTypeRegistry();

	// 从测试可执行文件位置向上找仓库根 / locate the repo root from the test executable
	auto engineRoot = std::filesystem::current_path();
	if (argc > 1)
		engineRoot = std::filesystem::path(argv[1]);
	testRealModFile(engineRoot);

	if (failures > 0)
	{
		std::cerr << failures << " test(s) failed\n";
		return 1;
	}

	std::cout << "all tests passed\n";
	return 0;
}
