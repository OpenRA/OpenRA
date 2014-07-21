#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Allows the actor to be temporarily disabled by certain warhead types.")]
	class DisabledByWarheadInfo : ITraitInfo
	{
		[Desc("The hitbox radius of the actor. A DisablerWarhead impact must touch this hitbox in order to affect the actor.")]
		public readonly WRange Radius = new WRange(426);

		[Desc("What types does this actor count as.")]
		public readonly string[] TargetTypes = { "EMP" };

		public object Create(ActorInitializer init) { return new DisabledByWarhead(init.self, this); }
	}


	class DisabledByWarhead : BaseWarhead, IWarheadInfo, IDisable, ISelectionBar, ITick, ISync
	{
		[Sync] int remainingTicks;
		[Sync] bool disabled = false;
		public DisabledByWarheadInfo Info;

		public DisabledByWarhead(Actor self, DisabledByWarheadInfo info)
		{
			Info = info;
		}

		public bool Disabled { get { return disabled; } }

		public void Tick(Actor self)
		{
			if (disabled)
			{
				disabled = (--remainingTicks > 0);
				if (!disabled)
					foreach (var nd in self.TraitsImplementing<INotifyDisabledByWarheadState>())
						nd.DisabledByWarheadStateChanged(self, false);
			}
		}

		public void SufferDisableImpact(Actor self, Actor attacker, int ticksLength)
		{
			if ((ticksLength > 0) &&
				(ticksLength > remainingTicks))
			{
				remainingTicks = ticksLength;
				disabled = true;

				foreach (var nd in self.TraitsImplementing<INotifyDisabledByWarheadState>())
					nd.DisabledByWarheadStateChanged(self, true);
			}
		}

		public Color GetColor() { return Color.Blue; }
		public float GetValue() { return .0f; }
	}
}
