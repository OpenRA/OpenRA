#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class InfiltrateForCashInfo : ITraitInfo, Requires<InfiltratableInfo>
	{
		public readonly int Percentage = 50;
		public readonly int Minimum = 500;
		public readonly string SoundToVictim = "credit1.aud";
		
		public object Create(ActorInitializer init) { return new InfiltrateForCash(this); }
	}

	class InfiltrateForCash : IAcceptInfiltrator
	{
		InfiltrateForCashInfo info;

		public InfiltrateForCash(InfiltrateForCashInfo info) { this.info = info; }

		public void OnInfiltrate(Actor self, Actor infiltrator)
		{
			var targetResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var spyResources = infiltrator.Owner.PlayerActor.Trait<PlayerResources>();

			var toTake = (targetResources.Cash + targetResources.Ore) * info.Percentage / 100;
			var toGive = Math.Max(toTake, info.Minimum);
			
			targetResources.TakeCash(toTake);
			spyResources.GiveCash(toGive);

			Sound.PlayToPlayer(self.Owner, info.SoundToVictim);

			self.World.AddFrameEndTask(w => w.Add(new CashTick(self.CenterPosition, infiltrator.Owner.Color.RGB, toGive)));
		}
	}
}

