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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("When killed, this actor causes the attacking player to receive money.")]
	class GivesBountyInfo : ConditionalTraitInfo
	{
		[Desc("Percentage of the killed actor's Cost or CustomSellValue to be given.")]
		public readonly int Percentage = 10;

		[Desc("Player relationships the attacking player needs to receive the bounty.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Whether to show a floating text announcing the won bounty.")]
		public readonly bool ShowBounty = true;

		[Desc("DeathTypes for which a bounty should be granted.",
			"Use an empty list (the default) to allow all DeathTypes.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		public override object Create(ActorInitializer init) { return new GivesBounty(this); }
	}

	class GivesBounty : ConditionalTrait<GivesBountyInfo>, INotifyKilled, INotifyPassengerEntered, INotifyPassengerExited
	{
		readonly Dictionary<Actor, GivesBounty[]> passengerBounties = new Dictionary<Actor, GivesBounty[]>();

		public GivesBounty(GivesBountyInfo info)
			: base(info) { }

		int GetBountyValue(Actor self)
		{
			return self.GetSellValue() * Info.Percentage / 100;
		}

		int GetDisplayedBountyValue(Actor self)
		{
			var bounty = GetBountyValue(self);
			foreach (var pb in passengerBounties)
				foreach (var b in pb.Value)
					if (!b.IsTraitDisabled)
						bounty += b.GetDisplayedBountyValue(pb.Key);

			return bounty;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed || IsTraitDisabled)
				return;

			if (!Info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			var displayedBounty = GetDisplayedBountyValue(self);
			if (Info.ShowBounty && self.IsInWorld && displayedBounty != 0 && e.Attacker.Owner.IsAlliedWith(self.World.RenderPlayer))
				e.Attacker.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, e.Attacker.Owner.Color, FloatingText.FormatCashTick(displayedBounty), 30)));

			e.Attacker.Owner.PlayerActor.Trait<PlayerResources>().ChangeCash(GetBountyValue(self));
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			passengerBounties.Add(passenger, passenger.TraitsImplementing<GivesBounty>().ToArray());
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			passengerBounties.Remove(passenger);
		}
	}
}
