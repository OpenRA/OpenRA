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

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.FileSystem;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class ContentSourcesModContent : IGlobalModData
	{
		public class Download
		{
			[FluentReference]
			public readonly string Title;
			public readonly string URL;
			public readonly string MirrorList;
			public readonly string SHA1;
			public readonly string Path;

			public Download(MiniYaml yaml)
			{
				Title = yaml.Value;
				FieldLoader.Load(this, yaml);
			}
		}

		[FieldLoader.Require]
		public readonly string Mod;

		public readonly string FaqUrl = null;

		[IncludeFluentReferences(LintDictionaryReference.Values)]
		[FieldLoader.LoadUsing(nameof(LoadContentSources))]
		public readonly FrozenDictionary<string, ContentSource> ContentSources = null;

		static FrozenDictionary<string, ContentSource> LoadContentSources(MiniYaml yaml)
		{
			var ret = new Dictionary<string, ContentSource>();
			var sourcesNode = yaml.Nodes.Single(n => n.Key == "ContentSources");
			foreach (var s in sourcesNode.Value.Nodes)
				ret.Add(s.Key, new ContentSource(s.Value));

			return ret.ToFrozenDictionary();
		}

		[FieldLoader.LoadUsing(nameof(LoadAttributes))]
		public readonly FrozenDictionary<string, FrozenDictionary<string, string>> Attributes;

		static FrozenDictionary<string, FrozenDictionary<string, string>> LoadAttributes(MiniYaml yaml)
		{
			var ret = new Dictionary<string, FrozenDictionary<string, string>>();
			var attributesNode = yaml.Nodes.SingleOrDefault(n => n.Key == "Attributes");
			if (attributesNode != null)
				foreach (var node in attributesNode.Value.Nodes)
					ret.Add(node.Key, node.Value.Nodes.ToFrozenDictionary(n => n.Key, n => n.Value.Value));

			return ret.ToFrozenDictionary();
		}

		[IncludeFluentReferences]
		[FieldLoader.LoadUsing(nameof(LoadQuickInstallDownload))]
		public readonly Download QuickInstall;

		static object LoadQuickInstallDownload(MiniYaml yaml)
		{
			var node = yaml.Nodes.SingleOrDefault(n => n.Key == "QuickInstall");
			return node != null ? new Download(node.Value) : null;
		}
	}
}
