#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class PowerInfo : ITraitInfo
	{
		[Desc("If negative, it will drain power. If positive, it will provide power.")]
		public readonly int Amount = 0;

		[Desc("Scale power amount with the current health.")]
		public readonly bool ScaleWithHealth = false;

		public object Create(ActorInitializer init) { return new Power(init.self, this); }
	}

	public class Power : INotifyDamage, INotifyCapture
	{
		readonly PowerInfo info;
		readonly Lazy<Health> health;
		PowerManager playerPower;

		public int CurrentPower
		{
			get
			{
				if (info.Amount <= 0 || health == null || !info.ScaleWithHealth)
					return info.Amount;

				return info.Amount * health.Value.HP / health.Value.MaxHP;
			}
		}

		public Power(Actor self, PowerInfo info)
		{
			this.info = info;
			health = Exts.Lazy(self.TraitOrDefault<Health>);
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (info.ScaleWithHealth)
				playerPower.UpdateActor(self, CurrentPower);
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			 playerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
