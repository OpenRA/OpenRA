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

using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Replaces the captured actor with a new one.")]
	public class TransformOnCaptureInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly string IntoActor = null;

		public readonly int ForceHealthPercentage = 0;

		public readonly bool SkipMakeAnims = true;

		[Desc("Transform only if the capturer's CaptureTypes overlap with these types. Leave empty to allow all types.")]
		public readonly BitSet<CaptureType> CaptureTypes = default;

		public override object Create(ActorInitializer init) { return new TransformOnCapture(init, this); }
	}

	public class TransformOnCapture : INotifyCapture
	{
		readonly TransformOnCaptureInfo info;
		readonly string faction;

		public TransformOnCapture(ActorInitializer init, TransformOnCaptureInfo info)
		{
			this.info = info;
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (!info.CaptureTypes.IsEmpty && !info.CaptureTypes.Overlaps(captureTypes))
				return;

			var facing = self.TraitOrDefault<IFacing>();
			var transform = new Transform(info.IntoActor) { ForceHealthPercentage = info.ForceHealthPercentage, Faction = faction };
			if (facing != null) transform.Facing = facing.Facing;
			transform.SkipMakeAnims = info.SkipMakeAnims;
			self.QueueActivity(false, transform);
		}
	}
}
