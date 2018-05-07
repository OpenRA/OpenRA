#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA
{
	using MiniYamlNodes = List<MiniYamlNode>;

	public static class MiniYamlExts
	{
		public static void WriteToFile(this MiniYamlNodes y, string filename)
		{
			File.WriteAllLines(filename, y.ToLines().Select(x => x.TrimEnd()).ToArray());
		}

		public static string WriteToString(this MiniYamlNodes y)
		{
			return y.ToLines().JoinWith("\n");
		}

		public static IEnumerable<string> ToLines(this MiniYamlNodes y)
		{
			foreach (var kv in y)
				foreach (var line in kv.Value.ToLines(kv.Key, kv.Comment))
					yield return line;
		}
	}

	public class MiniYamlNode
	{
		public struct SourceLocation
		{
			public string Filename; public int Line;
			public override string ToString() { return "{0}:{1}".F(Filename, Line); }
		}

		public SourceLocation Location;
		public string Key;
		public MiniYaml Value;
		public string Comment;

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
			: this(k, v, c, null) { }

		public MiniYamlNode(string k, string v, List<MiniYamlNode> n)
			: this(k, new MiniYaml(v, n), null) { }

		public MiniYamlNode(string k, string v, string c, List<MiniYamlNode> n)
			: this(k, new MiniYaml(v, n), c) { }

		public MiniYamlNode(string k, string v, string c, List<MiniYamlNode> n, SourceLocation loc)
			: this(k, new MiniYaml(v, n), c, loc) { }

		public override string ToString()
		{
			return "{{YamlNode: {0} @ {1}}}".F(Key, Location);
		}

		public MiniYamlNode Clone()
		{
			return new MiniYamlNode(Key, Value.Clone());
		}
	}

	public class MiniYaml
	{
		const int SpacesPerLevel = 4;
		static readonly Func<string, string> StringIdentity = s => s;
		static readonly Func<MiniYaml, MiniYaml> MiniYamlIdentity = my => my;
		public string Value;
		public List<MiniYamlNode> Nodes;

		public MiniYaml Clone()
		{
			return new MiniYaml(Value, Nodes.Select(n => n.Clone()).ToList());
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
			var ret = new Dictionary<TKey, TElement>();
			foreach (var y in Nodes)
			{
				var key = keySelector(y.Key);
				var element = elementSelector(y.Value);
				try
				{
					ret.Add(key, element);
				}
				catch (ArgumentException ex)
				{
					throw new InvalidDataException("Duplicate key '{0}' in {1}".F(y.Key, y.Location), ex);
				}
			}

			return ret;
		}

		public MiniYaml(string value) : this(value, null) { }

		public MiniYaml(string value, List<MiniYamlNode> nodes)
		{
			Value = value;
			Nodes = nodes ?? new List<MiniYamlNode>();
		}

		public static List<MiniYamlNode> NodesOrEmpty(MiniYaml y, string s)
		{
			var nd = y.ToDictionary();
			return nd.ContainsKey(s) ? nd[s].Nodes : new List<MiniYamlNode>();
		}

		static List<MiniYamlNode> FromLines(IEnumerable<string> lines, string filename, bool discardCommentsAndWhitespace)
		{
			var levels = new List<List<MiniYamlNode>>();
			levels.Add(new List<MiniYamlNode>());

			var lineNo = 0;
			foreach (var ll in lines)
			{
				var line = ll;
				++lineNo;

				var keyStart = 0;
				var level = 0;
				var spaces = 0;
				var textStart = false;

				string key = null;
				string value = null;
				string comment = null;
				var location = new MiniYamlNode.SourceLocation { Filename = filename, Line = lineNo };

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

					if (levels.Count <= level)
						throw new YamlException("Bad indent in miniyaml at {0}".F(location));

					while (levels.Count > level + 1)
						levels.RemoveAt(levels.Count - 1);

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
							if (commentStart < keyLength)
								keyLength = i - keyStart;
							else
								valueLength = i - valueStart;

							break;
						}
					}

					if (keyLength > 0)
						key = line.Substring(keyStart, keyLength).Trim();

					if (valueStart >= 0)
					{
						var trimmed = line.Substring(valueStart, valueLength).Trim();
						if (trimmed.Length > 0)
							value = trimmed;
					}

					if (commentStart >= 0 && !discardCommentsAndWhitespace)
						comment = line.Substring(commentStart);

					// Remove leading/trailing whitespace guards
					if (value != null && value.Length > 1)
					{
						var trimLeading = value[0] == '\\' && (value[1] == ' ' || value[1] == '\t') ? 1 : 0;
						var trimTrailing = value[value.Length - 1] == '\\' && (value[value.Length - 2] == ' ' || value[value.Length - 2] == '\t') ? 1 : 0;
						if (trimLeading + trimTrailing > 0)
							value = value.Substring(trimLeading, value.Length - trimLeading - trimTrailing);
					}

					// Remove escape characters from #
					if (value != null && value.IndexOf('#') != -1)
						value = value.Replace("\\#", "#");
				}

				if (key != null || !discardCommentsAndWhitespace)
				{
					var nodes = new List<MiniYamlNode>();
					levels[level].Add(new MiniYamlNode(key, value, comment, nodes, location));

					levels.Add(nodes);
				}
			}

			return levels[0];
		}

		public static List<MiniYamlNode> FromFile(string path, bool discardCommentsAndWhitespace = true)
		{
			return FromLines(File.ReadAllLines(path), path, discardCommentsAndWhitespace);
		}

		public static List<MiniYamlNode> FromStream(Stream s, string fileName = "<no filename available>", bool discardCommentsAndWhitespace = true)
		{
			using (var reader = new StreamReader(s))
				return FromString(reader.ReadToEnd(), fileName, discardCommentsAndWhitespace);
		}

		public static List<MiniYamlNode> FromString(string text, string fileName = "<no filename available>", bool discardCommentsAndWhitespace = true)
		{
			return FromLines(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None), fileName, discardCommentsAndWhitespace);
		}

		public static List<MiniYamlNode> Merge(IEnumerable<List<MiniYamlNode>> sources)
		{
			if (!sources.Any())
				return new List<MiniYamlNode>();

			var tree = sources.Where(s => s != null)
				.Select(MergeSelfPartial)
				.Aggregate(MergePartial)
				.Where(n => n.Key != null)
				.ToDictionary(n => n.Key, n => n.Value);

			var resolved = new Dictionary<string, MiniYaml>();
			foreach (var kv in tree)
			{
				var inherited = new Dictionary<string, MiniYamlNode.SourceLocation>();
				inherited.Add(kv.Key, new MiniYamlNode.SourceLocation());

				var children = ResolveInherits(kv.Key, kv.Value, tree, inherited);
				resolved.Add(kv.Key, new MiniYaml(kv.Value.Value, children));
			}

			// Resolve any top-level removals (e.g. removing whole actor blocks)
			var nodes = new MiniYaml("", resolved.Select(kv => new MiniYamlNode(kv.Key, kv.Value)).ToList());
			return ResolveInherits("", nodes, tree, new Dictionary<string, MiniYamlNode.SourceLocation>());
		}

		static void MergeIntoResolved(MiniYamlNode overrideNode, List<MiniYamlNode> existingNodes,
			Dictionary<string, MiniYaml> tree, Dictionary<string, MiniYamlNode.SourceLocation> inherited)
		{
			var existingNode = existingNodes.FirstOrDefault(n => n.Key == overrideNode.Key);
			if (existingNode != null)
			{
				existingNode.Value = MergePartial(existingNode.Value, overrideNode.Value);
				existingNode.Value.Nodes = ResolveInherits(existingNode.Key, existingNode.Value, tree, inherited);
			}
			else
				existingNodes.Add(overrideNode.Clone());
		}

		static List<MiniYamlNode> ResolveInherits(string key, MiniYaml node, Dictionary<string, MiniYaml> tree, Dictionary<string, MiniYamlNode.SourceLocation> inherited)
		{
			var resolved = new List<MiniYamlNode>();

			// Inheritance is tracked from parent->child, but not from child->parentsiblings.
			inherited = new Dictionary<string, MiniYamlNode.SourceLocation>(inherited);

			foreach (var n in node.Nodes)
			{
				if (n.Key == "Inherits" || n.Key.StartsWith("Inherits@", StringComparison.Ordinal))
				{
					MiniYaml parent;
					if (!tree.TryGetValue(n.Value.Value, out parent))
						throw new YamlException(
							"{0}: Parent type `{1}` not found".F(n.Location, n.Value.Value));

					if (inherited.ContainsKey(n.Value.Value))
						throw new YamlException("{0}: Parent type `{1}` was already inherited by this yaml tree at {2} (note: may be from a derived tree)"
							.F(n.Location, n.Value.Value, inherited[n.Value.Value]));

					inherited.Add(n.Value.Value, n.Location);
					foreach (var r in ResolveInherits(n.Key, parent, tree, inherited))
						MergeIntoResolved(r, resolved, tree, inherited);
				}
				else if (n.Key.StartsWith("-", StringComparison.Ordinal))
				{
					var removed = n.Key.Substring(1);
					if (resolved.RemoveAll(r => r.Key == removed) == 0)
						throw new YamlException("{0}: There are no elements with key `{1}` to remove".F(n.Location, removed));
				}
				else
					MergeIntoResolved(n, resolved, tree, inherited);
			}

			return resolved;
		}

		/// <summary>
		/// Merges any duplicate keys that are defined within the same set of nodes.
		/// Does not resolve inheritance or node removals.
		/// </summary>
		static MiniYaml MergeSelfPartial(MiniYaml existingNodes)
		{
			// Nothing to do
			if (existingNodes.Nodes == null || existingNodes.Nodes.Count == 0)
				return existingNodes;

			return new MiniYaml(existingNodes.Value, MergeSelfPartial(existingNodes.Nodes));
		}

		/// <summary>
		/// Merges any duplicate keys that are defined within the same set of nodes.
		/// Does not resolve inheritance or node removals.
		/// </summary>
		static List<MiniYamlNode> MergeSelfPartial(List<MiniYamlNode> existingNodes)
		{
			var keys = new HashSet<string>();
			var ret = new List<MiniYamlNode>();
			foreach (var n in existingNodes)
			{
				if (keys.Add(n.Key))
					ret.Add(n);
				else
				{
					// Node with the same key has already been added: merge new node over the existing one
					var original = ret.First(r => r.Key == n.Key);
					original.Value = MergePartial(original.Value, n.Value);
				}
			}

			return ret;
		}

		static MiniYaml MergePartial(MiniYaml existingNodes, MiniYaml overrideNodes)
		{
			if (existingNodes == null)
				return overrideNodes;

			if (overrideNodes == null)
				return existingNodes;

			return new MiniYaml(overrideNodes.Value ?? existingNodes.Value, MergePartial(existingNodes.Nodes, overrideNodes.Nodes));
		}

		static List<MiniYamlNode> MergePartial(List<MiniYamlNode> existingNodes, List<MiniYamlNode> overrideNodes)
		{
			if (existingNodes.Count == 0)
				return overrideNodes;

			if (overrideNodes.Count == 0)
				return existingNodes;

			var ret = new List<MiniYamlNode>();

			var existingDict = existingNodes.ToDictionaryWithConflictLog(x => x.Key, "MiniYaml.Merge", null, x => "{0} (at {1})".F(x.Key, x.Location));
			var overrideDict = overrideNodes.ToDictionaryWithConflictLog(x => x.Key, "MiniYaml.Merge", null, x => "{0} (at {1})".F(x.Key, x.Location));
			var allKeys = existingDict.Keys.Union(overrideDict.Keys);

			foreach (var key in allKeys)
			{
				MiniYamlNode existingNode, overrideNode;
				existingDict.TryGetValue(key, out existingNode);
				overrideDict.TryGetValue(key, out overrideNode);

				var loc = overrideNode == null ? default(MiniYamlNode.SourceLocation) : overrideNode.Location;
				var comment = (overrideNode ?? existingNode).Comment;
				var merged = (existingNode == null || overrideNode == null) ? overrideNode ?? existingNode :
					new MiniYamlNode(key, MergePartial(existingNode.Value, overrideNode.Value), comment, loc);
				ret.Add(merged);
			}

			return ret;
		}

		public IEnumerable<string> ToLines(string key, string comment = null)
		{
			var hasKey = !string.IsNullOrEmpty(key);
			var hasValue = !string.IsNullOrEmpty(Value);
			var hasComment = !string.IsNullOrEmpty(comment);
			yield return (hasKey ? key + ":" : "")
				+ (hasValue ? " " + Value.Replace("#", "\\#") : "")
				+ (hasComment ? (hasKey || hasValue ? " " : "") + "#" + comment : "");

			if (Nodes != null)
				foreach (var line in Nodes.ToLines())
					yield return "\t" + line;
		}

		public static List<MiniYamlNode> Load(IReadOnlyFileSystem fileSystem, IEnumerable<string> files, MiniYaml mapRules)
		{
			if (mapRules != null && mapRules.Value != null)
			{
				var mapFiles = FieldLoader.GetValue<string[]>("value", mapRules.Value);
				files = files.Append(mapFiles);
			}

			var yaml = files.Select(s => FromStream(fileSystem.Open(s), s));
			if (mapRules != null && mapRules.Nodes.Any())
				yaml = yaml.Append(mapRules.Nodes);

			return Merge(yaml);
		}
	}

	[Serializable]
	public class YamlException : Exception
	{
		public YamlException(string s) : base(s) { }
	}
}
