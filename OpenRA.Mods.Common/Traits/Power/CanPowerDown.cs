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

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The player can disable the power individually on this actor.")]
	public class CanPowerDownInfo : UpgradableTraitInfo, Requires<PowerInfo>
	{
		[Desc("Restore power when this trait is disabled.")]
		public readonly bool CancelWhenDisabled = false;

		public readonly string IndicatorImage = "poweroff";
		[SequenceReference("IndicatorImage")] public readonly string IndicatorSequence = "offline";

		[PaletteReference] public readonly string IndicatorPalette = "chrome";

		public readonly string PowerupSound = null;
		public readonly string PowerdownSound = null;

		public readonly string PowerupSpeech = null;
		public readonly string PowerdownSpeech = null;

		public override object Create(ActorInitializer init) { return new CanPowerDown(init.Self, this); }
	}

	public class CanPowerDown : UpgradableTrait<CanPowerDownInfo>, IPowerModifier, IResolveOrder, IDisable, INotifyOwnerChanged
	{
		[Sync] bool disabled = false;
		PowerManager power;

		public CanPowerDown(Actor self, CanPowerDownInfo info)
			: base(info)
		{
			power = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public bool Disabled { get { return disabled; } }

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsTraitDisabled && order.OrderString == "PowerDown")
			{
				disabled = !disabled;

				if (Info.PowerupSound != null && disabled)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.PowerupSound, self.Owner.Faction.InternalName);

				if (Info.PowerdownSound != null && !disabled)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.PowerdownSound, self.Owner.Faction.InternalName);

				if (Info.PowerupSpeech != null && disabled)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.PowerupSpeech, self.Owner.Faction.InternalName);

				if (Info.PowerdownSpeech != null && !disabled)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.PowerdownSpeech, self.Owner.Faction.InternalName);

				power.UpdateActor(self);

				if (disabled)
					self.World.AddFrameEndTask(w => w.Add(new PowerdownIndicator(self)));
			}
		}

		public int GetPowerModifier()
		{
			return !IsTraitDisabled && disabled ? 0 : 100;
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}

		protected override void UpgradeDisabled(Actor self)
		{
			if (!disabled || !Info.CancelWhenDisabled)
				return;
			disabled = false;

			if (Info.PowerupSound != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sound", Info.PowerupSound, self.Owner.Faction.InternalName);

			if (Info.PowerupSpeech != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.PowerupSpeech, self.Owner.Faction.InternalName);

			power.UpdateActor(self);
		}
	}
}
