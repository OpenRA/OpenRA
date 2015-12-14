#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA
{
	using MiniYamlNodes = List<MiniYamlNode>;

	public static class MiniYamlExts
	{
		public static void WriteToFile(this MiniYamlNodes y, string filename)
		{
			File.WriteAllLines(filename, y.ToLines(true).Select(x => x.TrimEnd()).ToArray());
		}

		public static string WriteToString(this MiniYamlNodes y)
		{
			return y.ToLines(true).Select(x => x.TrimEnd()).JoinWith("\n");
		}

		public static IEnumerable<string> ToLines(this MiniYamlNodes y, bool lowest)
		{
			foreach (var kv in y)
			{
				foreach (var line in kv.Value.ToLines(kv.Key))
					yield return line;

				if (lowest)
					yield return "";
			}
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

		public MiniYamlNode(string k, MiniYaml v)
		{
			Key = k;
			Value = v;
		}

		public MiniYamlNode(string k, MiniYaml v, SourceLocation loc)
			: this(k, v)
		{
			Location = loc;
		}

		public MiniYamlNode(string k, string v)
			: this(k, v, null) { }

		public MiniYamlNode(string k, string v, List<MiniYamlNode> n)
			: this(k, new MiniYaml(v, n)) { }

		public MiniYamlNode(string k, string v, List<MiniYamlNode> n, SourceLocation loc)
			: this(k, new MiniYaml(v, n), loc) { }

		public override string ToString()
		{
			return "{{YamlNode: {0} @ {1}}}".F(Key, Location);
		}
	}

	public class MiniYaml
	{
		const int SpacesPerLevel = 4;
		static readonly Func<string, string> StringIdentity = s => s;
		static readonly Func<MiniYaml, MiniYaml> MiniYamlIdentity = my => my;
		public string Value;
		public List<MiniYamlNode> Nodes;

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

		public static MiniYaml FromDictionary<K, V>(Dictionary<K, V> dict)
		{
			return new MiniYaml(null, dict.Select(x => new MiniYamlNode(x.Key.ToString(), new MiniYaml(x.Value.ToString()))).ToList());
		}

		public static MiniYaml FromList<T>(List<T> list)
		{
			return new MiniYaml(null, list.Select(x => new MiniYamlNode(x.ToString(), new MiniYaml(null))).ToList());
		}

		public static List<MiniYamlNode> NodesOrEmpty(MiniYaml y, string s)
		{
			var nd = y.ToDictionary();
			return nd.ContainsKey(s) ? nd[s].Nodes : new List<MiniYamlNode>();
		}

		static List<MiniYamlNode> FromLines(IEnumerable<string> lines, string filename)
		{
			var levels = new List<List<MiniYamlNode>>();
			levels.Add(new List<MiniYamlNode>());

			var lineNo = 0;
			foreach (var ll in lines)
			{
				var line = ll;
				++lineNo;

				var commentIndex = line.IndexOf('#');
				if (commentIndex != -1)
					line = line.Substring(0, commentIndex).TrimEnd(' ', '\t');

				if (line.Length == 0)
					continue;

				var charPosition = 0;
				var level = 0;
				var spaces = 0;
				var textStart = false;
				var currChar = line[charPosition];

				while (!(currChar == '\n' || currChar == '\r') && charPosition < line.Length && !textStart)
				{
					currChar = line[charPosition];
					switch (currChar)
					{
						case ' ':
							spaces++;
							if (spaces >= SpacesPerLevel)
							{
								spaces = 0;
								level++;
							}

							charPosition++;
							break;
						case '\t':
							level++;
							charPosition++;
							break;
						default:
							textStart = true;
							break;
					}
				}

				var realText = line.Substring(charPosition);
				if (realText.Length == 0)
					continue;

				var location = new MiniYamlNode.SourceLocation { Filename = filename, Line = lineNo };

				if (levels.Count <= level)
					throw new YamlException("Bad indent in miniyaml at {0}".F(location));

				while (levels.Count > level + 1)
					levels.RemoveAt(levels.Count - 1);

				var d = new List<MiniYamlNode>();
				var rhs = SplitAtColon(ref realText);
				levels[level].Add(new MiniYamlNode(realText, rhs, d, location));

				levels.Add(d);
			}

			return levels[0];
		}

		static string SplitAtColon(ref string realText)
		{
			var colon = realText.IndexOf(':');
			if (colon == -1)
				return null;

			var ret = realText.Substring(colon + 1).Trim();
			if (ret.Length == 0)
				ret = null;

			realText = realText.Substring(0, colon).Trim();
			return ret;
		}

		public static Dictionary<string, MiniYaml> DictFromFile(string path)
		{
			return FromFile(path).ToDictionary(x => x.Key, x => x.Value);
		}

		public static Dictionary<string, MiniYaml> DictFromStream(Stream stream)
		{
			return FromStream(stream).ToDictionary(x => x.Key, x => x.Value);
		}

		public static List<MiniYamlNode> FromFile(string path)
		{
			return FromLines(File.ReadAllLines(path), path);
		}

		public static List<MiniYamlNode> FromStream(Stream s, string fileName = "<no filename available>")
		{
			using (var reader = new StreamReader(s))
				return FromString(reader.ReadToEnd(), fileName);
		}

		public static List<MiniYamlNode> FromString(string text, string fileName = "<no filename available>")
		{
			return FromLines(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries), fileName);
		}

		public static List<MiniYamlNode> Merge(List<MiniYamlNode> a, List<MiniYamlNode> b)
		{
			return ApplyRemovals(MergePartial(a, b));
		}

		public static List<MiniYamlNode> MergePartial(List<MiniYamlNode> a, List<MiniYamlNode> b)
		{
			if (a.Count == 0)
				return b;

			if (b.Count == 0)
				return a;

			var ret = new List<MiniYamlNode>();

			var dictA = a.ToDictionaryWithConflictLog(x => x.Key, "MiniYaml.Merge", null, x => "{0} (at {1})".F(x.Key, x.Location));
			var dictB = b.ToDictionaryWithConflictLog(x => x.Key, "MiniYaml.Merge", null, x => "{0} (at {1})".F(x.Key, x.Location));
			var allKeys = dictA.Keys.Union(dictB.Keys);

			foreach (var key in allKeys)
			{
				MiniYamlNode aa, bb;
				dictA.TryGetValue(key, out aa);
				dictB.TryGetValue(key, out bb);

				var loc = aa == null ? default(MiniYamlNode.SourceLocation) : aa.Location;
				var merged = (aa == null || bb == null) ? aa ?? bb : new MiniYamlNode(key, MergePartial(aa.Value, bb.Value), loc);
				ret.Add(merged);
			}

			return ret;
		}

		public static List<MiniYamlNode> ApplyRemovals(List<MiniYamlNode> a)
		{
			var removeKeys = a.Select(x => x.Key)
				.Where(x => x.Length > 0 && x[0] == '-')
				.Select(k => k.Substring(1))
				.ToHashSet();

			var ret = new List<MiniYamlNode>();
			foreach (var x in a)
			{
				if (x.Key[0] == '-')
					continue;

				if (removeKeys.Contains(x.Key))
					removeKeys.Remove(x.Key);
				else
				{
					x.Value.Nodes = ApplyRemovals(x.Value.Nodes);
					ret.Add(x);
				}
			}

			if (removeKeys.Any())
				throw new YamlException("Bogus yaml removals: {0}".F(removeKeys.JoinWith(", ")));

			return ret;
		}

		public static MiniYaml MergePartial(MiniYaml a, MiniYaml b)
		{
			if (a == null)
				return b;

			if (b == null)
				return a;

			return new MiniYaml(a.Value ?? b.Value, MergePartial(a.Nodes, b.Nodes));
		}

		public static MiniYaml Merge(MiniYaml a, MiniYaml b)
		{
			if (a == null)
				return b;

			if (b == null)
				return a;

			return new MiniYaml(a.Value ?? b.Value, Merge(a.Nodes, b.Nodes));
		}

		public IEnumerable<string> ToLines(string name)
		{
			yield return name + ": " + Value;

			if (Nodes != null)
				foreach (var line in Nodes.ToLines(false))
					yield return "\t" + line;
		}
	}

	[Serializable]
	public class YamlException : Exception
	{
		public YamlException(string s) : base(s) { }
	}
}
