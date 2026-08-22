
// MiniYaml 实现 —— 与 OpenRA.Game/MiniYaml.cs 逐段对应
// MiniYaml implementation - mirrors OpenRA.Game/MiniYaml.cs section by section

#include "openra/MiniYaml.h"

#include <algorithm>
#include <cstdio>
#include <fstream>
#include <stdexcept>

namespace OpenRA
{
	namespace
	{
		constexpr int SpacesPerLevel = 4;

		std::string_view trim(std::string_view s)
		{
			while (!s.empty() && (s.front() == ' ' || s.front() == '\t'
				|| s.front() == '\r' || s.front() == '\n'))
				s.remove_prefix(1);
			while (!s.empty() && (s.back() == ' ' || s.back() == '\t'
				|| s.back() == '\r' || s.back() == '\n'))
				s.remove_suffix(1);
			return s;
		}

		bool startsWith(std::string_view s, std::string_view prefix)
		{
			return s.size() >= prefix.size() && s.substr(0, prefix.size()) == prefix;
		}

		// 池化：返回池内稳定字符串 / pooled: return the stable copy held by the pool
		std::string pooled(StringPool* pool, std::string&& s)
		{
			if (pool == nullptr)
				return std::move(s);
			return *pool->insert(std::move(s)).first;
		}

		// 解析中的行缓冲 / in-flight parsed line buffer (C# parsedLines tuple)
		struct ParsedLine
		{
			int level = 0;
			std::optional<std::string> key;
			std::optional<std::string> value;
			std::optional<std::string> comment;
			YamlSourceLocation location;
		};

		int indexOfKey(const std::vector<MiniYamlNode>& nodes, std::string_view key)
		{
			for (size_t i = 0; i < nodes.size(); i++)
				if (nodes[i].key && *nodes[i].key == key)
					return static_cast<int>(i);
			return -1;
		}

		int lastIndexOfKey(const std::vector<MiniYamlNode>& nodes, std::string_view key)
		{
			for (size_t i = nodes.size(); i-- > 0;)
				if (nodes[i].key && *nodes[i].key == key)
					return static_cast<int>(i);
			return -1;
		}

		// 按 \r\n 或 \n 切行（保留空行）/ split on \r\n or \n (keeping empty lines)
		std::vector<std::string_view> splitLines(std::string_view text)
		{
			std::vector<std::string_view> lines;
			size_t start = 0;
			for (size_t i = 0; i < text.size(); i++)
			{
				if (text[i] == '\n')
				{
					auto end = i;
					if (end > start && text[end - 1] == '\r')
						end--;
					lines.emplace_back(text.substr(start, end - start));
					start = i + 1;
				}
			}

			if (start < text.size())
				lines.emplace_back(text.substr(start));
			return lines;
		}
	}

	// ============================== 访问 / Access ==============================

	const MiniYamlNode* MiniYaml::nodeWithKeyOrDefault(std::string_view key) const
	{
		// C# 版对重复键抛异常 / the C# version throws on duplicate keys
		const MiniYamlNode* result = nullptr;
		for (const auto& node : nodes)
		{
			if (!node.key || *node.key != key)
				continue;
			if (result != nullptr)
				throw YamlException("Duplicate key '" + std::string(key) + "' in " + node.location.name + ":" + std::to_string(node.location.line));
			result = &node;
		}

		return result;
	}

	const MiniYamlNode& MiniYaml::nodeWithKey(std::string_view key) const
	{
		auto result = nodeWithKeyOrDefault(key);
		if (result == nullptr)
			throw YamlException("No node with key '" + std::string(key) + "'");
		return *result;
	}

	std::map<std::string, const MiniYaml*> MiniYaml::toDictionary() const
	{
		std::map<std::string, const MiniYaml*> ret;
		for (const auto& y : nodes)
		{
			if (!y.key)
				continue;
			if (!ret.emplace(*y.key, y.value.get()).second)
				throw YamlException("Duplicate key '" + *y.key + "' in " + y.location.name + ":" + std::to_string(y.location.line));
		}

		return ret;
	}

	// ============================== 解析 / Parsing ==============================

	namespace
	{
		// C# FromLines 的核心：缩进栈组装 / the core of C# FromLines: indent-stack assembly
		std::vector<MiniYamlNode> fromLines(const std::vector<std::string_view>& lines,
			std::string_view name, bool discardCommentsAndWhitespace, StringPool* pool)
		{
			// result[level] 收集该层完成的节点 / result[level] collects completed nodes per level
			std::vector<std::vector<MiniYamlNode>> result{ {} };
			std::vector<ParsedLine> parsedLines;

			// 把 parsedLines 顶层已完成的部分组装为节点树
			// Assemble the completed top of parsedLines into the node tree
			auto buildCompletedSubNode = [&](int level)
			{
				auto lastLevel = parsedLines.back().level;
				while (lastLevel >= static_cast<int>(result.size()))
					result.emplace_back();

				while (!parsedLines.empty() && parsedLines.back().level >= level)
				{
					const auto& parent = parsedLines.back();
					auto startOfRange = static_cast<int>(parsedLines.size()) - 1;
					while (startOfRange > 0 && parsedLines[startOfRange - 1].level == parent.level)
						startOfRange--;

					// 同层先出现的行先成为无子节点兄弟 / earlier same-level lines become childless siblings
					for (auto i = startOfRange; i < static_cast<int>(parsedLines.size()) - 1; i++)
					{
						const auto& sibling = parsedLines[i];
						result[parent.level].emplace_back(sibling.key,
							std::make_shared<MiniYaml>(sibling.value), sibling.comment, sibling.location);
					}

					// 最后一行持有下一层作为子节点 / the last line takes the next level as children
					std::vector<MiniYamlNode>* childNodes = parent.level + 1 < static_cast<int>(result.size())
						? &result[parent.level + 1] : nullptr;
					result[parent.level].emplace_back(parent.key,
						std::make_shared<MiniYaml>(parent.value, childNodes ? std::move(*childNodes) : std::vector<MiniYamlNode>{}),
						parent.comment, parent.location);
					if (childNodes)
						childNodes->clear();

					parsedLines.erase(parsedLines.begin() + startOfRange, parsedLines.end());
				}
			};

			auto lineNo = 0;
			for (auto ll : lines)
			{
				const auto line = ll;
				++lineNo;

				auto keyStart = 0;
				auto level = 0;
				auto spaces = 0;
				auto textStart = false;

				std::string_view key;
				std::string_view value;
				std::string_view comment;
				auto location = YamlSourceLocation{ std::string(name), lineNo };

				if (!line.empty())
				{
					// 缩进计数：4 空格或 1 tab 为一级 / indent counting: 4 spaces or 1 tab per level
					auto currChar = line[keyStart];
					while (keyStart < static_cast<int>(line.size()) && !textStart)
					{
						currChar = line[keyStart];
						switch (currChar)
						{
							case ' ':
								spaces++;
								if (spaces >= SpacesPerLevel)
								{
									spaces = 0;
									level++;
								}

								keyStart++;
								break;
							case '\t':
								level++;
								keyStart++;
								break;
							default:
								textStart = true;
								break;
						}
					}

					// 按 `<key>: <value>#<comment>` 切分；'#' 在值内需 '\#' 转义
					// Split as `<key>: <value>#<comment>`; '#' inside values needs '\#' escaping
					auto keyLength = static_cast<int>(line.size()) - keyStart;
					auto valueStart = -1;
					auto valueLength = 0;
					auto commentStart = -1;
					for (auto i = 0; i < static_cast<int>(line.size()); i++)
					{
						if (valueStart < 0 && line[i] == ':')
						{
							valueStart = i + 1;
							keyLength = i - keyStart;
							valueLength = static_cast<int>(line.size()) - i - 1;
						}

						if (commentStart < 0 && line[i] == '#' && (i == 0 || line[i - 1] != '\\'))
						{
							commentStart = i + 1;
							if (i <= keyStart + keyLength)
								keyLength = i - keyStart;
							else
								valueLength = i - valueStart;

							break;
						}
					}

					if (keyLength > 0)
						key = trim(line.substr(keyStart, keyLength));

					if (valueStart >= 0)
					{
						auto trimmed = trim(line.substr(valueStart, valueLength));
						if (!trimmed.empty())
							value = trimmed;
					}

					if (commentStart >= 0 && !discardCommentsAndWhitespace)
						comment = line.substr(commentStart);

					if (value.size() > 1)
					{
						// 前后反斜杠空白保护 / leading/trailing backslash whitespace guards
						auto trimLeading = value[0] == '\\' && (value[1] == ' ' || value[1] == '\t') ? 1 : 0;
						auto trimTrailing = value[value.size() - 1] == '\\' && (value[value.size() - 2] == ' ' || value[value.size() - 2] == '\t') ? 1 : 0;
						if (trimLeading + trimTrailing > 0)
							value = value.substr(trimLeading, value.size() - trimLeading - trimTrailing);

						// 还原被转义的 '#' / restore escaped '#'
						if (value.find("\\#") != std::string_view::npos)
						{
							std::string unescaped{ value };
							for (auto pos = unescaped.find("\\#"); pos != std::string::npos; pos = unescaped.find("\\#", pos))
							{
								unescaped.erase(pos, 1);
								pos++;
							}

							value = unescaped;
						}
					}
				}

				if (!key.empty() || !discardCommentsAndWhitespace)
				{
					if (!parsedLines.empty() && parsedLines.back().level < level - 1)
						throw YamlException("Bad indent in miniyaml at " + location.name + ":" + std::to_string(location.line));

					while (!parsedLines.empty() && parsedLines.back().level > level)
						buildCompletedSubNode(level);

					ParsedLine parsed{ level,
						key.empty() ? std::nullopt : std::optional<std::string>{ pooled(pool, std::string(key)) },
						value.empty() ? std::nullopt : std::optional<std::string>{ pooled(pool, std::string(value)) },
						comment.empty() ? std::nullopt : std::optional<std::string>{ pooled(pool, std::string(comment)) },
						location };
					parsedLines.emplace_back(std::move(parsed));
				}

				// C# 逐行 yield 顶层结果；C++ 版在末尾统一返回
				// C# yields top-level nodes per line; the C++ port returns them all at the end
			}

			if (!parsedLines.empty())
				buildCompletedSubNode(0);

			return std::move(result[0]);
		}
	}

	std::vector<MiniYamlNode> miniYamlFromString(std::string_view text, std::string_view name,
		bool discardCommentsAndWhitespace, StringPool* pool)
	{
		return fromLines(splitLines(text), name, discardCommentsAndWhitespace, pool);
	}

	std::vector<MiniYamlNode> miniYamlFromFile(const std::string& path,
		bool discardCommentsAndWhitespace, StringPool* pool)
	{
		std::ifstream file(path, std::ios::binary);
		if (!file)
			throw YamlException("Cannot open file: " + path);

		std::string text{ std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>() };
		return fromLines(splitLines(text), path, discardCommentsAndWhitespace, pool);
	}

	// ============================== 合并 / Merging ==============================

	namespace MiniYamlDetail
	{
		// 相互递归，先作前向声明 / mutually recursive; forward-declared first
		std::vector<MiniYamlNode> resolveInherits(const MiniYaml& node,
			const std::map<std::string, const MiniYaml*>& tree,
			std::map<std::string, YamlSourceLocation> inherited);

		std::vector<MiniYamlNode> mergeSelfPartial(const std::vector<MiniYamlNode>& existingNodes)
		{
			if (existingNodes.empty())
				return existingNodes;

			std::vector<std::string> keys;
			std::vector<MiniYamlNode> ret;
			ret.reserve(existingNodes.size());
			for (const auto& n : existingNodes)
			{
				if (!n.key)
					continue;

				if (std::find(keys.begin(), keys.end(), *n.key) == keys.end())
				{
					keys.push_back(*n.key);
					ret.push_back(n);
				}
				else
				{
					// 同键节点已存在：把新节点并入旧节点 / key seen before: merge the new node over it
					auto originalIndex = indexOfKey(ret, *n.key);
					ret[originalIndex].value = std::make_shared<MiniYaml>(
						mergePartial(ret[originalIndex].value.get(), n.value.get()));
				}
			}

			return ret;
		}

		std::vector<MiniYamlNode> weakResolveRemovals(const std::vector<MiniYamlNode>& nodes)
		{
			if (nodes.empty())
				return nodes;

			std::vector<MiniYamlNode> ret;
			bool copying = false;
			for (size_t i = 0; i < nodes.size(); i++)
			{
				const auto& node = nodes[i];
				if (!node.key)
					continue;

				if (startsWith(*node.key, "-"))
				{
					if (!copying)
					{
						copying = true;
						ret.assign(nodes.begin(), nodes.begin() + i);
					}

					// 弱删除：无可删项时静默跳过 / weak removal: silently skip if nothing matches
					auto removed = node.key->substr(1);
					std::erase_if(ret, [&](const MiniYamlNode& r) { return r.key && *r.key == removed; });
				}
				else if (copying)
					ret.push_back(node);
			}

			return copying ? ret : nodes;
		}

		MiniYaml mergePartial(const MiniYaml* existingNodes, const MiniYaml* overrideNodes)
		{
			auto resolvedExistingNodes = weakResolveRemovals(existingNodes ? existingNodes->nodes : std::vector<MiniYamlNode>{});
			auto resolvedOverrideNodes = weakResolveRemovals(overrideNodes ? overrideNodes->nodes : std::vector<MiniYamlNode>{});
			(void)resolvedExistingNodes;
			(void)resolvedOverrideNodes;
			// 注：C# 在此处用 ConflictScratch 做重复键诊断日志，此处未移植（不影响结果数据）
			// Note: C# logs duplicate-key diagnostics here via ConflictScratch; not ported (results unaffected)

			if (existingNodes == nullptr)
				return *overrideNodes;
			if (overrideNodes == nullptr)
				return *existingNodes;

			return MiniYaml(overrideNodes->value ? overrideNodes->value : existingNodes->value,
				mergePartial(existingNodes->nodes, overrideNodes->nodes));
		}

		std::vector<MiniYamlNode> mergePartial(const std::vector<MiniYamlNode>& existingNodes,
			const std::vector<MiniYamlNode>& overrideNodes)
		{
			if (existingNodes.empty())
				return overrideNodes;
			if (overrideNodes.empty())
				return existingNodes;

			std::vector<MiniYamlNode> ret;
			ret.reserve(existingNodes.size() + overrideNodes.size());
			std::vector<std::string> plainKeys;

			auto mergeNode = [&](const MiniYamlNode& node)
			{
				if (!node.key)
					return;

				// '-Key' 删除节点直接追加 / '-Key' removal nodes are appended as-is
				if (startsWith(*node.key, "-"))
				{
					ret.push_back(node);
					return;
				}

				// 新键直接追加 / unseen keys are appended
				if (std::find(plainKeys.begin(), plainKeys.end(), *node.key) == plainKeys.end())
				{
					plainKeys.push_back(*node.key);
					ret.push_back(node);
					return;
				}

				// 若比上一次出现更近的位置存在删除节点，改为追加（保证 应用→删除→再应用 的顺序）
				// If a removal node sits closer than the previous occurrence, append instead
				// (preserving the apply -> remove -> re-apply ordering)
				auto previousNodeIndex = lastIndexOfKey(ret, *node.key);
				auto previousRemovalNodeIndex = lastIndexOfKey(ret, "-" + *node.key);
				if (previousRemovalNodeIndex != -1 && previousRemovalNodeIndex > previousNodeIndex)
				{
					ret.push_back(node);
					return;
				}

				ret[previousNodeIndex].value = std::make_shared<MiniYaml>(
					mergePartial(ret[previousNodeIndex].value.get(), node.value.get()));
			};

			for (const auto& node : existingNodes)
				mergeNode(node);
			for (const auto& node : overrideNodes)
				mergeNode(node);

			return ret;
		}

		// 覆盖节点并入已解析列表 / merge an override node into the resolved list
		void mergeIntoResolved(const MiniYamlNode& overrideNode, std::vector<MiniYamlNode>& existingNodes,
			std::vector<std::string>& existingNodeKeys, const std::map<std::string, const MiniYaml*>& tree,
			const std::map<std::string, YamlSourceLocation>& inherited)
		{
			// 注意：C# 在此处用 ResolveInherits 解析覆盖值的继承，但传入的是已解析树，
			// Note: C# runs ResolveInherits on the override value here, but the tree members are
			// 上层已解析过的节点 —— 此处按原版语义递归处理
			// already resolved at this level - we recurse per the original semantics
			const MiniYamlNode* existingNode = nullptr;
			int existingNodeIndex = -1;
			if (std::find(existingNodeKeys.begin(), existingNodeKeys.end(), *overrideNode.key) != existingNodeKeys.end())
			{
				existingNodeIndex = indexOfKey(existingNodes, *overrideNode.key);
				existingNode = &existingNodes[existingNodeIndex];
			}
			else
				existingNodeKeys.push_back(*overrideNode.key);

			auto value = mergePartial(existingNode ? existingNode->value.get() : nullptr, overrideNode.value.get());
			auto nodes = resolveInherits(value, tree, inherited);
			value.nodes = std::move(nodes);

			if (existingNode != nullptr)
			{
				auto merged = *existingNode;
				merged.value = std::make_shared<MiniYaml>(std::move(value));
				existingNodes[existingNodeIndex] = std::move(merged);
			}
			else
				existingNodes.push_back(overrideNode);
		}

		std::vector<MiniYamlNode> resolveInherits(const MiniYaml& node,
			const std::map<std::string, const MiniYaml*>& tree,
			std::map<std::string, YamlSourceLocation> inherited)
		{
			if (node.nodes.empty())
				return node.nodes;

			std::vector<MiniYamlNode> resolved;
			resolved.reserve(node.nodes.size());
			std::vector<std::string> resolvedKeys;

			for (const auto& n : node.nodes)
			{
				if (n.key && (*n.key == "Inherits" || startsWith(*n.key, "Inherits@")))
				{
					auto it = tree.find(*n.value->value);
					if (it == tree.end())
						throw YamlException(n.location.name + ":" + std::to_string(n.location.line)
							+ ": Parent type `" + *n.value->value + "` not found");

					auto [inserted, ok] = inherited.emplace(*n.value->value, n.location);
					if (!ok)
						throw YamlException(n.location.name + ":" + std::to_string(n.location.line)
							+ ": Parent type `" + *n.value->value + "` was already inherited by this yaml tree at "
							+ inserted->second.name + ":" + std::to_string(inserted->second.line)
							+ " (note: may be from a derived tree)");

					// 注意：C# 里 inherited 在循环内持续累积；C++ 按值传参+修改局部副本等价
					// Note: C# keeps accumulating `inherited` across loop iterations;
					// the by-value parameter with local mutation is equivalent
					for (const auto& r : resolveInherits(*it->second, tree, inherited))
						mergeIntoResolved(r, resolved, resolvedKeys, tree, inherited);
				}
				else if (n.key && startsWith(*n.key, "-"))
				{
					auto removed = n.key->substr(1);
					auto before = resolved.size();
					std::erase_if(resolved, [&](const MiniYamlNode& r) { return r.key && *r.key == removed; });
					if (resolved.size() == before)
						throw YamlException(n.location.name + ":" + std::to_string(n.location.line)
							+ ": There are no elements with key `" + removed + "` to remove");
					std::erase(resolvedKeys, removed);
				}
				else if (n.key)
					mergeIntoResolved(n, resolved, resolvedKeys, tree, inherited);
			}

			return resolved;
		}
	}

	std::vector<MiniYamlNode> miniYamlMerge(const std::vector<std::vector<MiniYamlNode>>& sources)
	{
		if (sources.empty())
			return {};

		// 各源自合并后逐个归并 / self-merge each source, then fold them together
		std::optional<std::vector<MiniYamlNode>> accumulated;
		std::map<std::string, std::shared_ptr<MiniYaml>> tree;
		for (const auto& s : sources)
		{
			auto selfMerged = MiniYamlDetail::mergeSelfPartial(s);
			if (!accumulated)
				accumulated = std::move(selfMerged);
			else
				accumulated = MiniYamlDetail::mergePartial(*accumulated, selfMerged);
		}

		for (const auto& n : *accumulated)
			if (n.key && !tree.emplace(*n.key, n.value).second)
				throw YamlException("Duplicate top-level key `" + *n.key + "` in merge");

		std::map<std::string, const MiniYaml*> treeView;
		for (const auto& [k, v] : tree)
			treeView.emplace(k, v.get());

		std::map<std::string, std::vector<MiniYamlNode>> resolved;
		for (const auto& [k, v] : tree)
		{
			// 继承沿父→子追踪，不沿子→父的兄弟扩散
			// Inheritance is tracked parent->child, not child->parentsiblings
			std::map<std::string, YamlSourceLocation> inherited{ { k, {} } };
			resolved.emplace(k, MiniYamlDetail::resolveInherits(*v, treeView, inherited));
		}

		// 解析顶层删除（如整块 actor 移除）/ resolve top-level removals (e.g. whole actor blocks)
		std::vector<MiniYamlNode> rootNodes;
		for (const auto& [k, v] : resolved)
			rootNodes.emplace_back(k, std::make_shared<MiniYaml>(std::nullopt, v));
		auto root = MiniYaml(std::nullopt, std::move(rootNodes));
		return MiniYamlDetail::resolveInherits(root, treeView, {});
	}

	// ============================== 序列化 / Serialization ==============================

	namespace
	{
		void nodeToLines(const MiniYamlNode& node, std::vector<std::string>& lines, int depth)
		{
			const auto& y = *node.value;
			auto hasKey = node.key && !node.key->empty();
			auto hasValue = y.value && !y.value->empty();
			auto hasComment = node.comment.has_value();

			std::string line;
			if (hasKey)
				line += *node.key + ":";
			if (hasValue)
			{
				auto escaped = *y.value;
				for (auto pos = escaped.find('#'); pos != std::string::npos; pos = escaped.find('#', pos + 2))
					escaped.insert(pos, "\\");
				line += " " + escaped;
			}

			if (hasComment)
			{
				if (hasKey || hasValue)
					line += " ";
				line += "#" + *node.comment;
			}

			// C# 版子行以 "\t" 前缀递归 / child lines are "\t"-prefixed in the C# version
			lines.push_back(std::string(static_cast<size_t>(depth), '\t') + std::move(line));

			for (const auto& child : y.nodes)
				nodeToLines(child, lines, depth + 1);
		}
	}

	std::vector<std::string> miniYamlToLines(const std::vector<MiniYamlNode>& nodes)
	{
		std::vector<std::string> lines;
		for (const auto& node : nodes)
			nodeToLines(node, lines, 0);
		return lines;
	}

	std::string miniYamlToString(const std::vector<MiniYamlNode>& nodes)
	{
		auto lines = miniYamlToLines(nodes);
		std::string text;
		for (const auto& l : lines)
		{
			// C# 版 TrimEnd 只去行尾空白并保留 tab 缩进（两端 trim 会破坏重解析）
			// C# TrimEnd removes trailing whitespace only, keeping tab indentation
			// (trimming both ends would break re-parsing)
			auto end = l.size();
			while (end > 0 && (l[end - 1] == ' ' || l[end - 1] == '\t' || l[end - 1] == '\r' || l[end - 1] == '\n'))
				end--;
			text += l.substr(0, end);
			text += "\n";
		}

		return text;
	}
}
