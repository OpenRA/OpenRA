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

using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TransformOnCaptureInfo : ITraitInfo
	{
		[ActorReference] public readonly string IntoActor = null;
		public readonly int ForceHealthPercentage = 0;
		public readonly bool SkipMakeAnims = true;

		public virtual object Create(ActorInitializer init) { return new TransformOnCapture(this); }
	}

	public class TransformOnCapture : INotifyCapture
	{
		readonly TransformOnCaptureInfo info;

		public TransformOnCapture(TransformOnCaptureInfo info) { this.info = info; }

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			var facing = self.TraitOrDefault<IFacing>();
			var transform = new Transform(self, info.IntoActor) { ForceHealthPercentage = info.ForceHealthPercentage };
			if (facing != null) transform.Facing = facing.Facing;
			transform.SkipMakeAnims = info.SkipMakeAnims;
			self.CancelActivity();
			self.QueueActivity(transform);
		}
	}
}
