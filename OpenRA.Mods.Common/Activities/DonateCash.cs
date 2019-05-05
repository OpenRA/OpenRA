#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class DonateCash : Enter
	{
		readonly DeliversCash deliversCash;

		public DonateCash(Actor self, Target target, DeliversCash deliversCash)
			: base(self, target, Color.Yellow)
		{
			this.deliversCash = deliversCash;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			var enterAcceptsCash = targetActor.TraitsImplementing<AcceptsDeliveredCash>().FirstEnabledTraitOrDefault();
			if (enterAcceptsCash == null || enterAcceptsCash.IsTraitDisabled || deliversCash.IsTraitDisabled)
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			var targetOwner = targetActor.Owner;
			var donated = targetOwner.PlayerActor.Trait<PlayerResources>().ChangeCash(deliversCash.Info.Payload);

			var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
			if (exp != null && targetOwner != self.Owner)
				exp.GiveExperience(deliversCash.Info.PlayerExperience);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(targetActor.CenterPosition, targetOwner.Color, FloatingText.FormatCashTick(donated), 30)));

			foreach (var nct in targetActor.TraitsImplementing<INotifyCashTransfer>())
				nct.OnAcceptingCash(targetActor, self);

			foreach (var nct in self.TraitsImplementing<INotifyCashTransfer>())
				nct.OnDeliveringCash(self, targetActor);

			self.Dispose();
		}
	}
}
