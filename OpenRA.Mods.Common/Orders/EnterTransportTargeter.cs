#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class EnterTransportTargeter : EnterAlliedActorTargeter<CargoInfo>
	{
		readonly AlternateTransportsMode mode;

		public EnterTransportTargeter(string order, int priority,
			Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor,
			AlternateTransportsMode mode)
			: base(order, priority, canTarget, useEnterCursor) { this.mode = mode; }

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			switch (mode)
			{
				case AlternateTransportsMode.None:
					break;
				case AlternateTransportsMode.Force:
					if (modifiers.HasModifier(TargetModifiers.ForceMove))
						return false;
					break;
				case AlternateTransportsMode.Default:
					if (!modifiers.HasModifier(TargetModifiers.ForceMove))
						return false;
					break;
				case AlternateTransportsMode.Always:
					return false;
			}

			return base.CanTargetActor(self, target, modifiers, ref cursor);
		}
	}
}
