
// MiniYaml —— OpenRA 的 YAML 子集解析/合并引擎（C++23 移植版）
// MiniYaml - OpenRA's YAML-subset parsing/merging engine (C++23 port)
//
// 从 OpenRA.Game/MiniYaml.cs 逐行为保真翻译，保留全部方言行为：
// Ported line-by-line from OpenRA.Game/MiniYaml.cs, preserving every dialect behavior:
//   - 4 个空格或 1 个 tab 均表示一级缩进 / 4 spaces or 1 tab each mean one indent level
//   - '#' 起注释（'\#' 转义）/ '#' starts a comment ('\#' is escaped)
//   - 值两侧的反斜杠保护空白 / backslash guards preserve whitespace around values
//   - Inherits / Inherits@id 继承与 '-Key' 节点删除 / Inherits / Inherits@id inheritance and '-Key' removals
//
// 与 C# 版的差异（有意为之）/ Differences from the C# version (intentional):
//   - Key/Value/Comment 用 std::optional 表达 C# 的 null 语义
//     Key/Value/Comment use std::optional to express C#'s null semantics
//   - 解析结果直接返回 vector（C# 为 yield 流式）/ parsing returns a vector directly (C# yields lazily)
//   - MergePartial 的重复键告警日志暂未移植（不影响结果数据）
//     MergePartial's duplicate-key warning log is not ported yet (does not affect results)

#pragma once

#include <map>
#include <memory>
#include <optional>
#include <stdexcept>
#include <string>
#include <string_view>
#include <unordered_set>
#include <vector>

namespace OpenRA
{
	// 解析失败异常 / parse failure exception
	struct YamlException : std::runtime_error
	{
		explicit YamlException(const std::string& s)
			: std::runtime_error(s) { }
	};

	// 源位置（文件名:行号）/ source location (filename:line)
	struct YamlSourceLocation
	{
		std::string name;
		int line = 0;
	};

	struct MiniYaml;

	// YAML 键值节点 / a keyed YAML node
	// 注意定义顺序：节点持有 shared_ptr<MiniYaml>（不完整类型可作 shared_ptr 成员），
	// Note the definition order: nodes hold shared_ptr<MiniYaml> (allowed on incomplete types),
	// 而 MiniYaml 的 vector 成员需要本类型完整 / while MiniYaml's vector member needs this type complete
	struct MiniYamlNode
	{
		YamlSourceLocation location;
		std::optional<std::string> key;
		std::shared_ptr<MiniYaml> value;
		std::optional<std::string> comment;

		MiniYamlNode() = default;
		MiniYamlNode(std::optional<std::string> k, std::shared_ptr<MiniYaml> v,
			std::optional<std::string> c = std::nullopt, YamlSourceLocation loc = {})
			: location(std::move(loc)), key(std::move(k)), value(std::move(v)), comment(std::move(c)) { }
	};

	// YAML 标量 + 子节点容器 / a YAML scalar plus child nodes
	struct MiniYaml
	{
		// C# 的 string Value 可为 null —— 用 optional 区分“无值”与“空串”
		// C#'s string Value may be null - optional distinguishes "no value" from ""
		std::optional<std::string> value;
		std::vector<MiniYamlNode> nodes;

		MiniYaml() = default;
		explicit MiniYaml(std::optional<std::string> v)
			: value(std::move(v)) { }
		MiniYaml(std::optional<std::string> v, std::vector<MiniYamlNode> n)
			: value(std::move(v)), nodes(std::move(n)) { }

		// 取首个匹配 key 的子节点 / first child node matching the key
		const MiniYamlNode* nodeWithKeyOrDefault(std::string_view key) const;
		const MiniYamlNode& nodeWithKey(std::string_view key) const;

		// 子节点转字典（重复键抛异常）/ children to a map (duplicate keys throw)
		std::map<std::string, const MiniYaml*> toDictionary() const;
	};

	// ============================== 解析 / Parsing ==============================

	// 字符串池：跨多次解析复用相同字符串（内存去重），可传 nullptr 禁用
	// string pool: deduplicates repeated strings across parses; pass nullptr to disable
	using StringPool = std::unordered_set<std::string>;

	std::vector<MiniYamlNode> miniYamlFromString(std::string_view text, std::string_view name,
		bool discardCommentsAndWhitespace = true, StringPool* pool = nullptr);
	std::vector<MiniYamlNode> miniYamlFromFile(const std::string& path,
		bool discardCommentsAndWhitespace = true, StringPool* pool = nullptr);

	// ============================== 合并 / Merging ==============================

	// 合并多个来源并解析 Inherits 继承与 '-Key' 删除
	// Merges sources, resolving Inherits inheritance and '-Key' removals
	std::vector<MiniYamlNode> miniYamlMerge(const std::vector<std::vector<MiniYamlNode>>& sources);

	// ============================== 序列化 / Serialization ==============================

	// 节点列表序列化为文本（tab 缩进，与 C# 版一致）
	// Serialize node lists back to text (tab-indented, matching the C# version)
	std::vector<std::string> miniYamlToLines(const std::vector<MiniYamlNode>& nodes);
	std::string miniYamlToString(const std::vector<MiniYamlNode>& nodes);

	// ============================== 内部实现 / Internals ==============================

	namespace MiniYamlDetail
	{
		// 同层重复键去重合并（不解析继承/删除）
		// Deduplicate-merge duplicate keys within one level (no inheritance/removal resolution)
		std::vector<MiniYamlNode> mergeSelfPartial(const std::vector<MiniYamlNode>& existingNodes);

		// 弱删除：'-Key' 移除前面出现的同名节点；无可删项时不报错
		// Weak removal: '-Key' drops earlier same-key nodes; missing targets are ignored
		std::vector<MiniYamlNode> weakResolveRemovals(const std::vector<MiniYamlNode>& nodes);

		// 节点级合并：override 覆盖 existing / node-level merge: override wins over existing
		MiniYaml mergePartial(const MiniYaml* existingNodes, const MiniYaml* overrideNodes);
		std::vector<MiniYamlNode> mergePartial(const std::vector<MiniYamlNode>& existingNodes,
			const std::vector<MiniYamlNode>& overrideNodes);

		// 递归解析 Inherits / recursively resolve Inherits
		std::vector<MiniYamlNode> resolveInherits(const MiniYaml& node,
			const std::map<std::string, const MiniYaml*>& tree,
			std::map<std::string, YamlSourceLocation> inherited);
	}
}
