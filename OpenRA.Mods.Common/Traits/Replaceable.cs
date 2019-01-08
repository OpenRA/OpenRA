#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class ReplaceableInfo : ConditionalTraitInfo, ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Replacement types this Relpaceable actor accepts.")]
		public readonly HashSet<string> Types = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new Replaceable(init, this); }
	}

	public class Replaceable : ConditionalTrait<ReplaceableInfo>
	{
		public Replaceable(ActorInitializer init, ReplaceableInfo info)
			: base(info) { }
	}
}
