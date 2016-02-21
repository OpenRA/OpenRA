#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Traits
{
	public class EditorTilesetFilterInfo : TraitInfo<EditorTilesetFilter>
	{
		public readonly HashSet<string> RequireTilesets = null;
		public readonly HashSet<string> ExcludeTilesets = null;
	}

	public class EditorTilesetFilter { }
}
