#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PowerInfo : ConditionalTraitInfo
	{
		[Desc("If negative, it will drain power. If positive, it will provide power.")]
		public readonly int Amount = 0;

		public override object Create(ActorInitializer init) { return new Power(init.Self, this); }
	}

	public class Power : ConditionalTrait<PowerInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged
	{
		readonly Lazy<IPowerModifier[]> powerModifiers;

		public PowerManager PlayerPower { get; private set; }

		public int GetEnabledPower()
		{
			return Util.ApplyPercentageModifiers(Info.Amount, powerModifiers.Value.Select(m => m.GetPowerModifier()));
		}

		public Power(Actor self, PowerInfo info)
			: base(info)
		{
			PlayerPower = self.Owner.PlayerActor.Trait<PowerManager>();
			powerModifiers = Exts.Lazy(() => self.TraitsImplementing<IPowerModifier>().ToArray());
		}

		protected override void TraitEnabled(Actor self) { PlayerPower.UpdateActor(self); }
		protected override void TraitDisabled(Actor self) { PlayerPower.UpdateActor(self); }

		void INotifyAddedToWorld.AddedToWorld(Actor self) { PlayerPower.UpdateActor(self); }
		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { PlayerPower.RemoveActor(self); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			PlayerPower.RemoveActor(self);
			PlayerPower = newOwner.PlayerActor.Trait<PowerManager>();
			PlayerPower.UpdateActor(self);
		}
	}
}
