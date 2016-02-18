#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PlugInfo : TraitInfo<Plug>
	{
		[FieldLoader.Require]
		[Desc("Plug type (matched against Upgrades in Pluggable)")]
		public readonly string Type = null;
	}

	public class Plug { }
}
