#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class WithDecorationDisguisedInfo : WithDecorationInfo, Requires<DisguiseInfo>
	{
		[Desc("Require an active disguise to render this decoration?")]
		public readonly bool RequireDisguise = true;

		public override object Create(ActorInitializer init) { return new WithDecorationDisguised(init.Self, this); }
	}

	public class WithDecorationDisguised : WithDecoration
	{
		readonly WithDecorationDisguisedInfo info;
		readonly Disguise disguise;

		public WithDecorationDisguised(Actor self, WithDecorationDisguisedInfo info)
			: base(self, info)
		{
			this.info = info;
			disguise = self.Trait<Disguise>();
		}

		public override bool ShouldRender(Actor self)
		{
			return !info.RequireDisguise || disguise.Disguised;
		}
	}
}