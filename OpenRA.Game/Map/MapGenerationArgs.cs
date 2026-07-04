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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA
{
	public class MapGenerationArgs
	{
		[FieldLoader.Require]
		public string Uid = null;

		[FieldLoader.Require]
		public string Generator = null;

		// Tileset, Size are treated separately to simplify the front-end logic:
		// the editor tool must keep the existing map tileset/size, and the available
		// (generator-specific) Options may change based on these values.
		[FieldLoader.Require]
		public string Tileset = null;

		[FieldLoader.Require]
		public Size Size = default;

		// Title and author are baked into the map.yaml
		// and must agree across all clients, regardless
		// of the local client's language
		[FieldLoader.Require]
		public string Title = null;

		[FieldLoader.Require]
		public string Author = null;

		public Dictionary<string, string> Options = [];

		public List<MiniYamlNode> Serialize()
		{
			return
			[
				new("Uid", Uid),
				new("Generator", Generator),
				new("Tileset", Tileset),
				new("Size", FieldSaver.FormatValue(Size)),
				new("Options", new MiniYaml(null, Options.Select(o => new MiniYamlNode(o.Key, o.Value)))),
				new("Title", Title),
				new("Author", Author)
			];
		}
	}
}
