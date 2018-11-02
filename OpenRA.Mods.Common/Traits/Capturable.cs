#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be captured by a unit with Captures: trait.",
		"This trait should not be disabled if the actor also uses FrozenUnderFog.")]
	public class CapturableInfo : ConditionalTraitInfo, Requires<CaptureManagerInfo>
	{
		[FieldLoader.Require]
		[Desc("CaptureTypes (from the Captures trait) that are able to capture this.")]
		public readonly BitSet<CaptureType> Types = default(BitSet<CaptureType>);

		[Desc("What diplomatic stances can be captured by this actor.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		public readonly bool CancelActivity = false;

		public override object Create(ActorInitializer init) { return new Capturable(init.Self, this); }
	}

	public class Capturable : ConditionalTrait<CapturableInfo>, INotifyCapture
	{
		readonly CaptureManager captureManager;

		public Capturable(Actor self, CapturableInfo info)
			: base(info)
		{
			captureManager = self.Trait<CaptureManager>();
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (Info.CancelActivity)
			{
				var stop = new Order("Stop", self, false);
				foreach (var t in self.TraitsImplementing<IResolveOrder>())
					t.ResolveOrder(self, stop);
			}
		}

		protected override void TraitEnabled(Actor self) { captureManager.RefreshCapturable(self); }
		protected override void TraitDisabled(Actor self) { captureManager.RefreshCapturable(self); }
	}
}
