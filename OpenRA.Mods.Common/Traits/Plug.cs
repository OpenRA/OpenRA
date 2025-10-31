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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PlugInfo : TraitInfo<Plug>
	{
		[FieldLoader.Require]
		[Desc("Plug type (matched against Conditions in Pluggable)")]
		public readonly string Type = null;
	}

	public class Plug { }
}
