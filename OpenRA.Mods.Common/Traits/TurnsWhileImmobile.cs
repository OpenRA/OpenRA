#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	[Desc("This unit can turn even when Mobile trait is disabled, given condition.")]
	public class TurnsWhileImmobileInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new TurnsWhileImmobile(this); }
	}

	public class TurnsWhileImmobile : ConditionalTrait<TurnsWhileImmobileInfo>
	{
		public TurnsWhileImmobile(TurnsWhileImmobileInfo info) : base(info) { }
	}
}
