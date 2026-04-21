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
using OpenRA.Primitives;

namespace OpenRA
{
	public class MapGenerationArgs
	{
		[FieldLoader.Require]
		public string Uid = null;

		[FieldLoader.Require]
		public string Generator = null;

		[FieldLoader.Require]
		public string Tileset = null;

		[FieldLoader.Require]
		public Size Size = default;

		[FieldLoader.Require]
		public string Title = null;

		[FieldLoader.Require]
		public string Author = null;

		[FieldLoader.LoadUsing(nameof(LoadSettings))]
		public MiniYaml Settings = null;
		static MiniYaml LoadSettings(MiniYaml yaml)
		{
			return yaml.NodeWithKey("Settings").Value;
		}

		public List<MiniYamlNode> Serialize()
		{
			return
			[
				new("Uid", Uid),
				new("Generator", Generator),
				new("Tileset", Tileset),
				new("Size", FieldSaver.FormatValue(Size)),
				new("Settings", Settings),
				new("Title", Title),
				new("Author", Author)
			];
		}
	}
}
