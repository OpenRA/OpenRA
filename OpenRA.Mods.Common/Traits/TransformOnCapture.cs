#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TransformOnCaptureInfo : ITraitInfo
	{
		[ActorReference] public readonly string IntoActor = null;
		public readonly int ForceHealthPercentage = 0;
		public readonly bool SkipMakeAnims = true;

		[Desc("OnCapture triggers only if capturer's CaptureTypes contains this.")]
		public readonly string Type = "husk";

		public virtual object Create(ActorInitializer init) { return new TransformOnCapture(init, this); }
	}

	public class TransformOnCapture : INotifyCapture
	{
		readonly TransformOnCaptureInfo info;
		readonly string faction;

		public TransformOnCapture(ActorInitializer init, TransformOnCaptureInfo info)
		{
			this.info = info;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (!IsValidCaptor(captor))
				return;

			var facing = self.TraitOrDefault<IFacing>();
			var transform = new Transform(self, info.IntoActor) { ForceHealthPercentage = info.ForceHealthPercentage, Faction = faction };
			if (facing != null) transform.Facing = facing.Facing;
			transform.SkipMakeAnims = info.SkipMakeAnims;
			self.CancelActivity();
			self.QueueActivity(transform);
		}

		bool IsValidCaptor(Actor captor)
		{
			var capInfo = captor.Info.TraitInfoOrDefault<CapturesInfo>();
			if (capInfo != null && capInfo.CaptureTypes.Contains(info.Type))
				return true;

			var extCapInfo = captor.Info.TraitInfoOrDefault<ExternalCapturesInfo>();
			if (extCapInfo != null && extCapInfo.CaptureTypes.Contains(info.Type))
				return true;

			return false;
		}
	}
}
