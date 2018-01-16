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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class EditorTilesetFilterInfo : TraitInfo<EditorTilesetFilter>
	{
		public readonly HashSet<string> RequireTilesets = null;
		public readonly HashSet<string> ExcludeTilesets = null;
		public readonly string[] Categories;
	}

	public class EditorTilesetFilter { }
}
