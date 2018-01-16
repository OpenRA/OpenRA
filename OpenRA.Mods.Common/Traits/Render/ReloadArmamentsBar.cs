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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Visualizes the minimum remaining time for reloading the armaments.")]
	class ReloadArmamentsBarInfo : ITraitInfo
	{
		[Desc("Armament names")]
		public readonly string[] Armaments = { "primary", "secondary" };

		public readonly Color Color = Color.Red;

		public object Create(ActorInitializer init) { return new ReloadArmamentsBar(init.Self, this); }
	}

	class ReloadArmamentsBar : ISelectionBar, INotifyCreated
	{
		readonly ReloadArmamentsBarInfo info;
		readonly Actor self;
		IEnumerable<Armament> armaments;

		public ReloadArmamentsBar(Actor self, ReloadArmamentsBarInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			// Name check can be cached but enabled check can't.
			armaments = self.TraitsImplementing<Armament>().Where(a => info.Armaments.Contains(a.Info.Name)).ToArray().Where(Exts.IsTraitEnabled);
		}

		float ISelectionBar.GetValue()
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return armaments.Min(a => a.FireDelay / (float)a.Weapon.ReloadDelay);
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
