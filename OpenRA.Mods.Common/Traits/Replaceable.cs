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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ReplaceableInfo : ITraitInfo
	{
		// TODO: Currently a Replacement can only replace one Type of Replaceable. Implement possibility for a as in Pluggable
		[FieldLoader.Require]
		[Desc("Identifyer for this replaceable Actor (matched against Types in Replacement)")]
		public readonly string Type = null;

		public object Create(ActorInitializer init) { return new Replaceable(init, this); }
	}

	public class Replaceable
	{
		public readonly ReplaceableInfo Info;

		public Replaceable(ActorInitializer init, ReplaceableInfo info)
		{
			this.Info = info;
		}

		public bool AcceptsReplacement(Actor self, string replaces)
		{
			return Info.Type.Equals(replaces);
		}
	}
}
