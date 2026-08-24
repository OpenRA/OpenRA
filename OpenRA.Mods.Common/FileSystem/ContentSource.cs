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
using System.Collections.Immutable;

namespace OpenRA.Mods.Common.FileSystem
{
	public sealed class ContentSource
	{
		[FieldLoader.Ignore]
		[FluentReference(optional: true)]
		[Desc("Title to show in the source selector dropdown.")]
		public readonly string Title;

		[Desc("Require the Quick Install package in addition to a valid Origin")]
		public readonly bool RequiresQuickInstall = false;

		[Desc("A list of content packages to mount from this source.")]
		public readonly FrozenDictionary<string, string> Packages = FrozenDictionary<string, string>.Empty;

		[FieldLoader.LoadUsing(nameof(LoadOrigins))]
		[Desc("One or more Origins for the content provided by this source.")]
		public readonly ImmutableArray<MiniYamlNode> Origins;

		static object LoadOrigins(MiniYaml yaml)
		{
			return yaml.NodeWithKeyOrDefault("Origins")?.Value.Nodes ?? [];
		}

		public ContentSource(MiniYaml yaml)
		{
			Title = yaml.Value;
			FieldLoader.Load(this, yaml);
		}

		public bool TryMount(OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator,
			out FrozenDictionary<string, ImmutableArray<string>> attributes)
		{
			foreach (var originNode in Origins)
			{
				var origin = LoadOrigin(objectCreator, originNode.Value);
				if (origin.TryMount(fileSystem))
				{
					attributes = origin.GetAttributes();
					return true;
				}
			}

			attributes = FrozenDictionary<string, ImmutableArray<string>>.Empty;
			return false;
		}

		public bool CanMount(OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator,
			out FrozenDictionary<string, ImmutableArray<string>> attributes)
		{
			foreach (var originNode in Origins)
			{
				var origin = LoadOrigin(objectCreator, originNode.Value);
				if (origin.CanMount(fileSystem))
				{
					attributes = origin.GetAttributes();
					return true;
				}
			}

			attributes = FrozenDictionary<string, ImmutableArray<string>>.Empty;
			return false;
		}

		static ContentOrigin LoadOrigin(ObjectCreator objectCreator, MiniYaml yaml)
		{
			if (string.IsNullOrEmpty(yaml.Value))
				return new ContentOrigin();

			var origin = objectCreator.CreateObject<ContentOrigin>($"{yaml.Value}ContentOrigin");
			FieldLoader.Load(origin, yaml);

			return origin;
		}
	}
}
