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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Force a reload of all armaments while this trait isn't disabled.")]
	class ForceReloadInfo : ConditionalTraitInfo, Requires<ArmamentInfo>
	{
		[Desc("Armament names")]
		public readonly string[] Armaments = { "primary", "secondary" };

		public override object Create(ActorInitializer init) { return new ForceReload(init.Self, this); }
	}

	class ForceReload : ConditionalTrait<ForceReloadInfo>, ITick
	{
		readonly Lazy<Armament[]> armaments;

		public ForceReload(Actor self, ForceReloadInfo info)
			: base(info)
		{
			armaments = Exts.Lazy(() => self.TraitsImplementing<Armament>()
				.Where(a => info.Armaments.Contains(a.Info.Name)).ToArray());
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			foreach (var armament in armaments.Value)
			{
				armament.StartReload();
			}
		}
	}
}
