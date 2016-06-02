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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the building animation when it repairs a unit.")]
	public class WithRepairAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		public readonly bool PauseOnLowPower = false;

		public object Create(ActorInitializer init) { return new WithRepairAnimation(init.Self, this); }
	}

	public class WithRepairAnimation : INotifyRepair
	{
		readonly WithRepairAnimationInfo info;

		public WithRepairAnimation(Actor self, WithRepairAnimationInfo info)
		{
			this.info = info;
		}

		public void Repairing(Actor self, Actor host)
		{
			var spriteBody = host.TraitOrDefault<WithSpriteBody>();
			if (spriteBody != null && !(info.PauseOnLowPower && self.IsDisabled()))
				spriteBody.PlayCustomAnimation(host, info.Sequence, () => spriteBody.CancelCustomAnimation(host));
		}
	}
}