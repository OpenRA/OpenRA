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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class RefundsUnitsInfo : ITraitInfo
	{
		[Desc("Should the refunded amount be displayed?",
			"Will not display if amount is 0.")]
		public readonly bool CashTick = true;

		[Desc("Can friendly actors be ordered to enter this actor?", "Refund goes to structure owner, not the entering actor's.")]
		public readonly bool AllowFriendlies = true;

		[Desc("Can self-owned actors be ordered to enter this actor?")]
		public readonly bool AllowSelfOwned = true;

		[Desc("Refundable actor types.")]
		public readonly string[] RefundableTypes = { };

		public object Create(ActorInitializer init) { return new RefundsUnits(this); }
	}

	public class RefundsUnits
	{
		public readonly RefundsUnitsInfo Info;

		public RefundsUnits(RefundsUnitsInfo info) { Info = info; }

		///<summary> Use `CustomRefundValue` instead of `Valued` if possible.</summary>
		public int GetRefundValue(Actor actor)
		{
			var crvi = actor.Info.Traits.GetOrDefault<CustomRefundValueInfo>();
			if (crvi != null)
				return crvi.Value;

			var vi = actor.Info.Traits.GetOrDefault<ValuedInfo>();
			if (vi != null)
			{
				var ri = actor.Info.Traits.Get<RefundableInfo>();
				return (vi.Cost * ri.RefundPercent) / 100;
			}

			return 0;
		}
	}
}
