#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using OpenRA.FileSystem;

namespace OpenRA
{
	public static class MiniYamlExts
	{
		public static void WriteToFile(this IEnumerable<MiniYamlNode> y, string filename)
		{
			File.WriteAllLines(filename, y.ToLines().Select(x => x.TrimEnd()).ToArray());
		}

		public static string WriteToString(this IEnumerable<MiniYamlNode> y)
		{
			// Remove all trailing newlines and restore the final EOF newline
			return y.ToLines().JoinWith("\n").TrimEnd('\n') + "\n";
		}

		public static IEnumerable<string> ToLines(this IEnumerable<MiniYamlNode> y)
		{
			foreach (var kv in y)
				foreach (var line in kv.Value.ToLines(kv.Key, kv.Comment))
					yield return line;
		}

		public static void WriteToFile(this IEnumerable<MiniYamlNodeBuilder> y, string filename)
		{
			File.WriteAllLines(filename, y.ToLines().Select(x => x.TrimEnd()).ToArray());
		}

		public static string WriteToString(this IEnumerable<MiniYamlNodeBuilder> y)
		{
			// Remove all trailing newlines and restore the final EOF newline
			return y.ToLines().JoinWith("\n").TrimEnd('\n') + "\n";
		}

		public static IEnumerable<string> ToLines(this IEnumerable<MiniYamlNodeBuilder> y)
		{
			foreach (var kv in y)
				foreach (var line in kv.Value.ToLines(kv.Key, kv.Comment))
					yield return line;
		}
	}

	public sealed class MiniYamlNode
	{
		public readonly struct SourceLocation(string name, int line)
		{
			public readonly string Name = name;
			public readonly int Line = line;

			public override string ToString() { return $"{Name}:{Line}"; }
		}

		public readonly SourceLocation Location;
		public readonly string Key;
		public readonly MiniYaml Value;
		public readonly string Comment;

		public MiniYamlNode WithValue(MiniYaml value)
		{
			if (Value == value)
				return this;
			return new MiniYamlNode(Key, value, Comment, Location);
		}

		public MiniYamlNode(string k, MiniYaml v, string c = null)
		{
			Key = k;
			Value = v;
			Comment = c;
		}

		public MiniYamlNode(string k, MiniYaml v, string c, SourceLocation loc)
			: this(k, v, c)
		{
			Location = loc;
		}

		public MiniYamlNode(string k, string v, string c = null)
			: this(k, new MiniYaml(v, []), c) { }

		public MiniYamlNode(string k, string v, IEnumerable<MiniYamlNode> n)
			: this(k, new MiniYaml(v, n), null) { }

		public override string ToString()
		{
			return $"{{YamlNode: {Key} @ {Location}}}";
		}
	}

	public sealed class MiniYaml
	{
		const int SpacesPerLevel = 4;
		static readonly Func<string, string> StringIdentity = s => s;
		static readonly Func<MiniYaml, MiniYaml> MiniYamlIdentity = my => my;
		static readonly Lock SharedMergeBufferLock = new();
		static readonly WeakReference<MergeBuffer> SharedMergeBuffer = new(null);

		public readonly string Value;
		public readonly ImmutableArray<MiniYamlNode> Nodes;

		public MiniYaml WithValue(string value)
		{
			if (Value == value)
				return this;
			return new MiniYaml(value, Nodes);
		}

		public MiniYaml WithNodes(IEnumerable<MiniYamlNode> nodes)
		{
			if (nodes is ImmutableArray<MiniYamlNode> n && Nodes == n)
				return this;
			return new MiniYaml(Value, nodes);
		}

		public MiniYaml WithNodesAppended(IEnumerable<MiniYamlNode> nodes)
		{
			var newNodes = Nodes.AddRange(nodes);
			if (Nodes == newNodes)
				return this;
			return new MiniYaml(Value, newNodes);
		}

		public MiniYamlNode NodeWithKey(string key)
		{
			var result = NodeWithKeyOrDefault(key);
			if (result == null)
				throw new InvalidDataException($"No node with key '{key}'");
			return result;
		}

		public MiniYamlNode NodeWithKeyOrDefault(string key)
		{
			// PERF: Avoid LINQ.
			var first = true;
			MiniYamlNode result = null;
			foreach (var node in Nodes)
			{
				if (node.Key != key)
					continue;

				if (!first)
					throw new InvalidDataException($"Duplicate key '{node.Key}' in {node.Location}");

				first = false;
				result = node;
			}

			return result;
		}

		public Dictionary<string, MiniYaml> ToDictionary()
		{
			return ToDictionary(MiniYamlIdentity);
		}

		public Dictionary<string, TElement> ToDictionary<TElement>(Func<MiniYaml, TElement> elementSelector)
		{
			return ToDictionary(StringIdentity, elementSelector);
		}

		public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(
			Func<string, TKey> keySelector, Func<MiniYaml, TElement> elementSelector)
		{
			var ret = new Dictionary<TKey, TElement>(Nodes.Length);
			foreach (var y in Nodes)
			{
				var key = keySelector(y.Key);
				var element = elementSelector(y.Value);
				if (!ret.TryAdd(key, element))
					throw new InvalidDataException($"Duplicate key '{y.Key}' in {y.Location}");
			}

			return ret;
		}

		public MiniYaml(string value)
			: this(value, []) { }

		public MiniYaml(string value, IEnumerable<MiniYamlNode> nodes)
		{
			Value = value;
			Nodes = nodes.ToImmutableArray();
		}

		static IEnumerable<MiniYamlNode> FromLines(
			IEnumerable<ReadOnlyMemory<char>> lines, string name, bool discardCommentsAndWhitespace, HashSet<string> stringPool)
		{
			// YAML config often contains repeated strings for key, values, comments.
			// Pool these strings so we only need one copy of each unique string.
			// This saves on long-term memory usage as parsed values can often live a long time.
			// A caller can also provide a pool as input, allowing de-duplication across multiple parses.
			stringPool ??= [];
			var stringPoolLookup = stringPool.GetAlternateLookup<ReadOnlySpan<char>>();

			var result = new List<List<MiniYamlNode>>
			{
				new()
			};
			var parsedLines = new List<(int Level, string Key, string Value, string Comment, MiniYamlNode.SourceLocation Location)>();

			var lineNo = 0;
			foreach (var ll in lines)
			{
				var line = ll.Span;
				++lineNo;

				var keyStart = 0;
				var level = 0;
				var spaces = 0;
				var textStart = false;

				ReadOnlySpan<char> key = default;
				ReadOnlySpan<char> value = default;
				ReadOnlySpan<char> comment = default;
				var location = new MiniYamlNode.SourceLocation(name, lineNo);

				if (line.Length > 0)
				{
					var currChar = line[keyStart];

					while (!(currChar == '\n' || currChar == '\r') && keyStart < line.Length && !textStart)
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

					// Extract key, value, comment from line as `<key>: <value>#<comment>`
					// The # character is allowed in the value if escaped (\#).
					// Leading and trailing whitespace is always trimmed from keys.
					// Leading and trailing whitespace is trimmed from values unless they
					// are marked with leading or trailing backslashes
					var keyLength = line.Length - keyStart;
					var valueStart = -1;
					var valueLength = 0;
					var commentStart = -1;
					for (var i = 0; i < line.Length; i++)
					{
						if (valueStart < 0 && line[i] == ':')
						{
							valueStart = i + 1;
							keyLength = i - keyStart;
							valueLength = line.Length - i - 1;
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
						key = line.Slice(keyStart, keyLength).Trim();

					if (valueStart >= 0)
					{
						var trimmed = line.Slice(valueStart, valueLength).Trim();
						if (trimmed.Length > 0)
							value = trimmed;
					}

					if (commentStart >= 0 && !discardCommentsAndWhitespace)
						comment = line[commentStart..];

					if (value.Length > 1)
					{
						// Remove leading/trailing whitespace guards
						var trimLeading = value[0] == '\\' && (value[1] == ' ' || value[1] == '\t') ? 1 : 0;
						var trimTrailing = value[^1] == '\\' && (value[^2] == ' ' || value[^2] == '\t') ? 1 : 0;
						if (trimLeading + trimTrailing > 0)
							value = value.Slice(trimLeading, value.Length - trimLeading - trimTrailing);

						// Remove escape characters from #
						if (value.Contains("\\#", StringComparison.Ordinal))
							value = value.ToString().Replace("\\#", "#");
					}
				}

				if (!key.IsEmpty || !discardCommentsAndWhitespace)
				{
					if (parsedLines.Count > 0 && parsedLines[^1].Level < level - 1)
						throw new YamlException($"Bad indent in miniyaml at {location}");

					while (parsedLines.Count > 0 && parsedLines[^1].Level > level)
						BuildCompletedSubNode(level);

					string GetOrAdd(ReadOnlySpan<char> value)
					{
						if (stringPoolLookup.TryGetValue(value, out var result))
							return result;
						stringPool.Add(result = value.ToString());
						return result;
					}

					var keyString = key.IsEmpty ? null : GetOrAdd(key);
					var valueString = value.IsEmpty ? null : GetOrAdd(value);

					// Note: We need to support empty comments here to ensure that empty comments
					// (i.e. a lone # at the end of a line) can be correctly re-serialized
					var commentString = comment == ReadOnlySpan<char>.Empty ? null : GetOrAdd(comment);

					parsedLines.Add((level, keyString, valueString, commentString, location));
				}

				foreach (var topLevelNode in result[0])
					yield return topLevelNode;
				result[0].Clear();
			}

			if (parsedLines.Count > 0)
			{
				BuildCompletedSubNode(0);
				foreach (var topLevelNode in result[0])
					yield return topLevelNode;
				result[0].Clear();
			}

			void BuildCompletedSubNode(int level)
			{
				var lastLevel = parsedLines[^1].Level;
				while (lastLevel >= result.Count)
					result.Add([]);

				while (parsedLines.Count > 0 && parsedLines[^1].Level >= level)
				{
					var parent = parsedLines[^1];
					var startOfRange = parsedLines.Count - 1;
					while (startOfRange > 0 && parsedLines[startOfRange - 1].Level == parent.Level)
						startOfRange--;

					for (var i = startOfRange; i < parsedLines.Count - 1; i++)
					{
						var sibling = parsedLines[i];
						result[parent.Level].Add(
							new MiniYamlNode(sibling.Key, new MiniYaml(sibling.Value), sibling.Comment, sibling.Location));
					}

					var childNodes = parent.Level + 1 < result.Count ? result[parent.Level + 1] : null;
					result[parent.Level].Add(new MiniYamlNode(
						parent.Key,
						new MiniYaml(parent.Value, childNodes ?? Enumerable.Empty<MiniYamlNode>()),
						parent.Comment,
						parent.Location));
					childNodes?.Clear();

					parsedLines.RemoveRange(startOfRange, parsedLines.Count - startOfRange);
				}
			}
		}

		public static IEnumerable<MiniYamlNode> FromFile(string path, bool discardCommentsAndWhitespace = true, HashSet<string> stringPool = null)
		{
			return FromStream(File.OpenRead(path), path, discardCommentsAndWhitespace, stringPool);
		}

		public static IEnumerable<MiniYamlNode> FromStream(Stream s, string name, bool discardCommentsAndWhitespace = true, HashSet<string> stringPool = null)
		{
			return FromLines(s.ReadAllLinesAsMemory(), name, discardCommentsAndWhitespace, stringPool);
		}

		public static IEnumerable<MiniYamlNode> FromString(string text, string name, bool discardCommentsAndWhitespace = true, HashSet<string> stringPool = null)
		{
			return FromLines(text.Split(["\r\n", "\n"], StringSplitOptions.None).Select(s => s.AsMemory()), name, discardCommentsAndWhitespace, stringPool);
		}

		public static ImmutableArray<MiniYamlNode> Merge(IEnumerable<IEnumerable<MiniYamlNode>> sources)
		{
			// PERF: If a previous buffer hasn't yet been collected, we can reuse it to avoid allocations.
			MergeBuffer buffer;
			lock (SharedMergeBufferLock)
				if (SharedMergeBuffer.TryGetTarget(out buffer))
					SharedMergeBuffer.SetTarget(null);
			buffer ??= MergeBuffer.New();

			// Perform the merge.
			// After merging in the first pass, all top level nodes become available as inheritance targets.
			var merged = MergeFirstPass(buffer, sources).ToImmutableArray();
			var topLevelNodesDict = merged.ToDictionary(n => n.Key);
			var resolved = MergeSecondPass(buffer, merged, [], null, topLevelNodesDict).ToImmutableArray();

			// Allow this buffer to be reused by other calls, until it gets collected.
			buffer.Clear();
			lock (SharedMergeBufferLock)
				SharedMergeBuffer.SetTarget(buffer);

			return resolved;
		}

		/// <summary>
		/// In the first pass we remove comments and perform merges.
		/// </summary>
		static ReadOnlySpan<MiniYamlNode> MergeFirstPass(
			MergeBuffer buffer,
			IEnumerable<IEnumerable<MiniYamlNode>> sources)
		{
			// PERF: Reuse collections as we recurse through the tree.
			var nodes = buffer.Nodes;
			var keys = buffer.Keys;
			var keysLookup = buffer.KeysLookup;
			nodes.Clear();
			keys.Clear();

			var sourceIndex = 0;
			var nodeIndex = 0;
			foreach (var source in sources)
			{
				foreach (var node in source)
				{
					if (node.Key == null)
					{
						// Comment node.
						// Strip these from the source.
					}
					else if (node.Key[0] == '-')
					{
						// Removal node.
						// We clear the key, this will prevent nodes either side of the removal being merged together.
						// We keep the removal node to be enacted in the second pass.
						var key = node.Key.AsSpan()[1..];
						keysLookup.Remove(key);

						nodes.Add(node);
						nodeIndex++;
					}
					else if (keys.TryGetValue(node.Key, out var existingIndex))
					{
						// Merge nodes.
						// Recurse and stitch together the node tree.
						var existingNode = nodes[existingIndex.NodeIndex];

						if (existingIndex.SourceIndex == sourceIndex && node.Value.Nodes.Length == 0 && existingNode.Value.Nodes.Length == 0)
							throw new YamlException(
								$"{nameof(MiniYaml)}.{nameof(Merge)}, duplicate values found for the following keys: " +
								$"{node.Key}: [{existingNode.Key} (at {existingNode.Location}),{node.Key} (at {node.Location})]");

						nodes[existingIndex.NodeIndex] = MergeNode(node, existingNode.Value, node.Value);
					}
					else
					{
						// New node.
						// Append to the end, track which source it came from for duplicate tracking.
						keys.Add(node.Key, (sourceIndex, nodeIndex));
						nodes.Add(node);
						nodeIndex++;
					}
				}

				sourceIndex++;
			}

			// We want to reuse the list from the buffer, don't allow the caller to capture it.
			return CollectionsMarshal.AsSpan(nodes);

			MiniYamlNode MergeNode(
				MiniYamlNode node,
				MiniYaml firstSource,
				MiniYaml secondSource)
			{
				var value = secondSource.Value ?? firstSource.Value;
				var nodes = MergeFirstPass(buffer.Next(), [firstSource.Nodes, secondSource.Nodes]);

				var newValue = node.Value.WithValue(value);
				if (!nodes.SequenceEqual(node.Value.Nodes.AsSpan()))
					newValue = newValue.WithNodes(nodes.ToImmutableArray());
				return node.WithValue(newValue);
			}
		}

		/// <summary>
		/// In the second pass we resolve inheritance and removals.
		/// </summary>
		static ReadOnlySpan<MiniYamlNode> MergeSecondPass(
			MergeBuffer buffer,
			ImmutableArray<MiniYamlNode> firstSource,
			ImmutableArray<MiniYamlNode> secondSource,
			string parentKey,
			Dictionary<string, MiniYamlNode> topLevelNodesDict)
		{
			// PERF: Reuse collections as we recurse through the tree.
			var nodes = buffer.Nodes;
			var keys = buffer.Keys;
			var keysLookup = buffer.KeysLookup;
			var inherited = buffer.Inherited;
			nodes.Clear();
			keys.Clear();
			inherited.Clear();

			// Inheritance is performed as a second pass, so we can inherit the fully merged node from the first pass.
			var inheritSourceIndex = -1;
			var sourceIndex = 0;
			var nodeIndex = 0;
			var source = firstSource;
			while (true)
			{
				foreach (var node in source)
				{
					if (node.Key == "Inherits" || node.Key.StartsWith("Inherits@", StringComparison.Ordinal))
					{
						// Inherits node - resolve target.
						var topLevelKey = node.Value.Value;
						if (!topLevelNodesDict.TryGetValue(topLevelKey, out var topLevelNode))
							throw new YamlException(
								$"{node.Location}: Parent type `{topLevelKey}` not found");

						// Can't duplicate inherits.
						if (!inherited.TryAdd(topLevelKey, node))
							throw new YamlException(
								$"{node.Location}: Parent type `{topLevelKey}` was already inherited by this yaml tree at {inherited[topLevelKey].Location} (note: may be from a derived tree)");

						// Can't duplicate inherits in the tree.
						if (buffer.TopLevelInherited.TryGetValue(topLevelKey, out var tree))
							foreach (var (treeKey, treeNode) in tree)
								if (inherited.TryGetValue(treeKey, out var originalNode))
									throw new YamlException(
										$"{originalNode.Location}: Parent type `{treeKey}` was already inherited by this yaml tree at {treeNode.Location} (note: may be from a derived tree)");

						// Build the inheritance tree.
						if (parentKey != null)
						{
							if (buffer.TopLevelInherited.TryGetValue(parentKey, out var parentTree))
								parentTree.Add((topLevelKey, node));
							else
								buffer.TopLevelInherited.Add(parentKey, [(topLevelKey, node)]);
						}

						// Recurse and stitch together the inherited node tree.
						var inheritedNodes = MergeSecondPass(buffer.Next(), topLevelNode.Value.Nodes, [], parentKey, topLevelNodesDict);
						foreach (var inheritedNode in inheritedNodes.ToImmutableArray())
							ResolveNode(inheritedNode, inheritSourceIndex, ref nodeIndex);

						inheritSourceIndex--;
					}
					else
					{
						ResolveNode(node, sourceIndex, ref nodeIndex);
					}
				}

				sourceIndex++;
				if (sourceIndex == 1)
					source = secondSource;
				else if (sourceIndex == 2)
					break;
			}

			// Remove tombstone slots left empty by removal nodes.
			nodes.RemoveAll(n => n == null);

			// We want to reuse the list from the buffer, don't allow the caller to capture it.
			return CollectionsMarshal.AsSpan(nodes);

			void ResolveNode(MiniYamlNode node, int sourceIndex, ref int nodeIndex)
			{
				MiniYamlNode MergeNode(
					MiniYaml firstSource,
					MiniYaml secondSource)
				{
					var value = secondSource?.Value ?? firstSource.Value;
					var nodes = MergeSecondPass(buffer.Next(), firstSource.Nodes, secondSource?.Nodes ?? [], node.Key, topLevelNodesDict);

					var newValue = node.Value.WithValue(value);
					if (!nodes.SequenceEqual(node.Value.Nodes.AsSpan()))
						newValue = newValue.WithNodes(nodes.ToImmutableArray());
					return node.WithValue(newValue);
				}

				if (node.Key[0] == '-')
				{
					// Removal node.
					// We clear the key, this will prevent nodes either side of the removal being merged together.
					// We tombstone the removed node - we'll remove the tombstones at the end.
					// Tombstoning saves us having to fixup the key lookup indexes.
					var key = node.Key.AsSpan()[1..];
					if (!keysLookup.TryGetValue(key, out var existingIndex))
						throw new YamlException($"{node.Location}: There are no elements with key `{key}` to remove");

					keysLookup.Remove(key);
					nodes[existingIndex.NodeIndex] = null;
				}
				else if (keys.TryGetValue(node.Key, out var existingIndex))
				{
					// Merge nodes.
					// Recurse and stitch together the node tree.
					var existingNode = nodes[existingIndex.NodeIndex];

					if (existingIndex.SourceIndex == sourceIndex && node.Value.Nodes.Length == 0 && existingNode.Value.Nodes.Length == 0)
						throw new YamlException(
							$"{nameof(MiniYaml)}.{nameof(Merge)}, duplicate values found for the following keys: " +
							$"{node.Key}: [{existingNode.Key} (at {existingNode.Location}),{node.Key} (at {node.Location})]");

					nodes[existingIndex.NodeIndex] = MergeNode(existingNode.Value, node.Value);
				}
				else
				{
					// New node.
					// Recurse and append to the end.
					keys.Add(node.Key, (sourceIndex, nodeIndex));
					nodes.Add(MergeNode(node.Value, null));
					nodeIndex++;
				}
			}
		}

		sealed class MergeBuffer
		{
			public Dictionary<string, List<(string Key, MiniYamlNode Node)>> TopLevelInherited { get; private set; }
			public readonly List<MiniYamlNode> Nodes = [];
			public readonly Dictionary<string, (int SourceIndex, int NodeIndex)> Keys = [];
			public readonly Dictionary<string, (int SourceIndex, int NodeIndex)>.AlternateLookup<ReadOnlySpan<char>> KeysLookup;
			public readonly Dictionary<string, MiniYamlNode> Inherited = [];
			MergeBuffer next;

			MergeBuffer()
			{
				KeysLookup = Keys.GetAlternateLookup<ReadOnlySpan<char>>();
			}

			public static MergeBuffer New()
			{
				return new MergeBuffer()
				{
					TopLevelInherited = [] // Only required at the top level.
				};
			}

			public MergeBuffer Next()
			{
				next ??= new MergeBuffer() { TopLevelInherited = TopLevelInherited };
				return next;
			}

			public void Clear()
			{
				TopLevelInherited.Clear();
				ClearInstance();
			}

			void ClearInstance()
			{
				Nodes.Clear();
				Keys.Clear();
				Inherited.Clear();
				next?.ClearInstance();
			}
		}

		public IEnumerable<string> ToLines(string key, string comment = null)
		{
			var hasKey = !string.IsNullOrEmpty(key);
			var hasValue = !string.IsNullOrEmpty(Value);
			var hasComment = comment != null;
			yield return (hasKey ? key + ":" : "")
				+ (hasValue ? " " + Value.Replace("#", "\\#") : "")
				+ (hasComment ? (hasKey || hasValue ? " " : "") + "#" + comment : "");

			if (Nodes != null)
				foreach (var line in Nodes.ToLines())
					yield return "\t" + line;
		}

		public static ImmutableArray<MiniYamlNode> Load(IReadOnlyFileSystem fileSystem, IEnumerable<string> files, MiniYaml mapRules)
		{
			if (mapRules != null && mapRules.Value != null)
			{
				var mapFiles = FieldLoader.GetValue<ImmutableArray<string>>("value", mapRules.Value);
				files = files.Concat(mapFiles);
			}

			var stringPool = new HashSet<string>(); // Reuse common strings in YAML
			var yaml = files.Select(s => FromStream(fileSystem.Open(s), s, stringPool: stringPool));
			if (mapRules != null && mapRules.Nodes.Length > 0)
				yaml = yaml.Append(mapRules.Nodes);

			return Merge(yaml);
		}
	}

	public sealed class MiniYamlNodeBuilder
	{
		public MiniYamlNode.SourceLocation Location;
		public string Key;
		public MiniYamlBuilder Value;
		public string Comment;

		public MiniYamlNodeBuilder(MiniYamlNode node)
		{
			Location = node.Location;
			Key = node.Key;
			Value = new MiniYamlBuilder(node.Value);
			Comment = node.Comment;
		}

		public MiniYamlNodeBuilder(string k, MiniYamlBuilder v, string c = null)
		{
			Key = k;
			Value = v;
			Comment = c;
		}

		public MiniYamlNodeBuilder(string k, MiniYamlBuilder v, string c, MiniYamlNode.SourceLocation loc)
			: this(k, v, c)
		{
			Location = loc;
		}

		public MiniYamlNodeBuilder(string k, string v, string c = null)
			: this(k, new MiniYamlBuilder(v, null), c) { }

		public MiniYamlNodeBuilder(string k, string v, List<MiniYamlNode> n)
			: this(k, new MiniYamlBuilder(v, n), null) { }

		public MiniYamlNode Build()
		{
			return new MiniYamlNode(Key, Value.Build(), Comment, Location);
		}
	}

	public sealed class MiniYamlBuilder
	{
		public string Value;
		public List<MiniYamlNodeBuilder> Nodes;

		public MiniYamlBuilder(MiniYaml yaml)
		{
			Value = yaml.Value;
			Nodes = yaml.Nodes.Select(n => new MiniYamlNodeBuilder(n)).ToList();
		}

		public MiniYamlBuilder(string value)
			: this(value, null) { }

		public MiniYamlBuilder(string value, List<MiniYamlNode> nodes)
		{
			Value = value;
			Nodes = nodes == null ? [] : nodes.ConvertAll(x => new MiniYamlNodeBuilder(x));
		}

		public MiniYaml Build()
		{
			return new MiniYaml(Value, Nodes.Select(n => n.Build()));
		}

		public IEnumerable<string> ToLines(string key, string comment = null)
		{
			var hasKey = !string.IsNullOrEmpty(key);
			var hasValue = !string.IsNullOrEmpty(Value);
			var hasComment = comment != null;
			yield return (hasKey ? key + ":" : "")
				+ (hasValue ? " " + Value.Replace("#", "\\#") : "")
				+ (hasComment ? (hasKey || hasValue ? " " : "") + "#" + comment : "");

			if (Nodes != null)
				foreach (var line in Nodes.ToLines())
					yield return "\t" + line;
		}

		public MiniYamlNodeBuilder NodeWithKeyOrDefault(string key)
		{
			return Nodes.SingleOrDefault(n => n.Key == key);
		}
	}

	public class YamlException : Exception
	{
		public YamlException(string s)
			: base(s) { }
	}
}
