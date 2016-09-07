#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Tag trait for Lua scripts.")]
	public class ActorTagInfo : TraitInfo<ActorTag>
	{
		[FieldLoader.Require]
		public readonly HashSet<string> Type = new HashSet<string>();
	}

	public class ActorTag { }
}
