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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Tag trait for actors with `DeliversCash`.")]
	public class AcceptsDeliveredCashInfo : ConditionalTraitInfo
	{
		[Desc("Accepted `DeliversCash` types. Leave empty to accept all types.")]
		public readonly HashSet<string> ValidTypes = new HashSet<string>();

		[Desc("Stance the delivering actor needs to enter.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("Play a randomly selected sound from this list when accepting cash.")]
		public readonly string[] Sounds = { };

		public override object Create(ActorInitializer init) { return new AcceptsDeliveredCash(init.Self, this); }
	}

	public class AcceptsDeliveredCash : ConditionalTrait<AcceptsDeliveredCashInfo>, INotifyCashTransfer
	{
		public AcceptsDeliveredCash(Actor self, AcceptsDeliveredCashInfo info)
			: base(info) { }

		void INotifyCashTransfer.OnAcceptingCash(Actor self, Actor donor)
		{
			if (Info.Sounds.Length > 0)
				Game.Sound.Play(SoundType.World, Info.Sounds, self.World, self.CenterPosition);
		}

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor) { }
	}
}
