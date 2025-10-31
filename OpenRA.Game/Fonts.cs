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

namespace OpenRA
{
	public class FontData
	{
		public readonly string Font;
		public readonly int Size;
		public readonly int Ascender;
	}

	public class Fonts : IGlobalModData
	{
		[FieldLoader.LoadUsing(nameof(LoadFonts))]
		public readonly Dictionary<string, FontData> FontList;

		static object LoadFonts(MiniYaml y)
		{
			var ret = new Dictionary<string, FontData>();
			foreach (var node in y.Nodes)
				ret.Add(node.Key, FieldLoader.Load<FontData>(node.Value));

			return ret;
		}
	}
}
