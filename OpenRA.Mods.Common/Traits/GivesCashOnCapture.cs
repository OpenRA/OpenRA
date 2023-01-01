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

using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Lets the actor grant cash when captured.")]
	public class GivesCashOnCaptureInfo : ConditionalTraitInfo
	{
		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("How long to show the Amount tick indicator when enabled.")]
		public readonly int DisplayDuration = 30;

		[Desc("Amount of money awarded for capturing the actor.")]
		public readonly int Amount = 0;

		[Desc("Award cash only if the capturer's CaptureTypes overlap with these types. Leave empty to allow all types.")]
		public readonly BitSet<CaptureType> CaptureTypes = default;

		public override object Create(ActorInitializer init) { return new GivesCashOnCapture(this); }
	}

	public class GivesCashOnCapture : ConditionalTrait<GivesCashOnCaptureInfo>, INotifyCapture
	{
		readonly GivesCashOnCaptureInfo info;

		public GivesCashOnCapture(GivesCashOnCaptureInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (IsTraitDisabled)
				return;

			if (!info.CaptureTypes.IsEmpty && !info.CaptureTypes.Overlaps(captureTypes))
				return;

			var resources = newOwner.PlayerActor.Trait<PlayerResources>();
			var amount = resources.ChangeCash(info.Amount);
			if (!info.ShowTicks && amount != 0)
				return;

			self.World.AddFrameEndTask(w => w.Add(
				new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(amount), info.DisplayDuration)));
		}
	}
}
