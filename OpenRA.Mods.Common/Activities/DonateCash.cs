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
		readonly int payload;
		readonly int playerExperience;

		Actor enterActor;

		public DonateCash(Actor self, in Target target, int payload, int playerExperience, Color? targetLineColor)
			: base(self, target, targetLineColor)
		{
			this.payload = payload;
			this.playerExperience = playerExperience;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;

			// Make sure the target actor is still owned by a player with eligible relationship
			var isTargetStillValid = true;
			var targetInfo = targetActor.Info.TraitInfoOrDefault<AcceptsDeliveredCashInfo>();
			if (targetInfo == null
				|| !targetInfo.ValidRelationships.HasRelationship(targetActor.Owner.RelationshipWith(self.Owner))
				|| (targetInfo.SamePlayerOnly && targetActor.Owner != self.Owner))
				isTargetStillValid = false;

			if (!isTargetStillValid)
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			// Make sure the target hasn't changed while entering
			// OnEnterComplete is only called if targetActor is alive
			if (targetActor != enterActor)
				return;

			var targetOwner = targetActor.Owner;

			// Make sure the target actor is still owned by a player with eligible relationship
			var targetInfo = targetActor.Info.TraitInfoOrDefault<AcceptsDeliveredCashInfo>();
			if (targetInfo == null
				|| !targetInfo.ValidRelationships.HasRelationship(targetOwner.RelationshipWith(self.Owner))
				|| (targetInfo.SamePlayerOnly && targetOwner != self.Owner))
				return;

			var donated = targetOwner.PlayerActor.Trait<PlayerResources>().ChangeCash(payload);

			var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
			if (exp != null && targetOwner != self.Owner)
				exp.GiveExperience(playerExperience);

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
