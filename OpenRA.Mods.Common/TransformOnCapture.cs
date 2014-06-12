#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class TransformOnCaptureInfo : ITraitInfo
	{
		[ActorReference] public readonly string IntoActor = null;
		public readonly int ForceHealthPercentage = 0;
		public readonly bool SkipMakeAnims = true;

		public virtual object Create(ActorInitializer init) { return new TransformOnCapture(this); }
	}

	class TransformOnCapture : INotifyCapture
	{
		TransformOnCaptureInfo Info;

		public TransformOnCapture(TransformOnCaptureInfo info) { Info = info; }

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			var facing = self.TraitOrDefault<IFacing>();
			var transform = new Transform(self, Info.IntoActor) { ForceHealthPercentage = Info.ForceHealthPercentage };
			if (facing != null) transform.Facing = facing.Facing;
			transform.SkipMakeAnims = Info.SkipMakeAnims;
			self.CancelActivity();
			self.QueueActivity(transform);
		}
	}
}
