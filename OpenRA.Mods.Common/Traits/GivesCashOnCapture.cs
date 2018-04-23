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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

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
		public readonly HashSet<string> CaptureTypes = new HashSet<string>();

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

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (IsTraitDisabled || !IsValidCaptor(captor))
				return;

			var resources = newOwner.PlayerActor.Trait<PlayerResources>();
			var amount = info.Amount;

			if (amount < 0)
			{
				// Check whether the amount of cash to be removed would exceed available player cash, in that case only remove all the player cash
				amount = Math.Min(resources.Cash + resources.Resources, -amount);
				resources.TakeCash(amount);

				// For correct cash tick display
				amount = -amount;
			}
			else
				resources.GiveCash(amount);

			if (!info.ShowTicks)
				return;

			self.World.AddFrameEndTask(w => w.Add(
				new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(amount), info.DisplayDuration)));
		}

		bool IsValidCaptor(Actor captor)
		{
			if (!info.CaptureTypes.Any())
				return true;

			var capturesInfo = captor.Info.TraitInfoOrDefault<CapturesInfo>();
			if (capturesInfo != null && info.CaptureTypes.Overlaps(capturesInfo.CaptureTypes))
				return true;

			var externalCapturesInfo = captor.Info.TraitInfoOrDefault<ExternalCapturesInfo>();
			if (externalCapturesInfo != null && info.CaptureTypes.Overlaps(externalCapturesInfo.CaptureTypes))
				return true;

			return false;
		}
	}
}
