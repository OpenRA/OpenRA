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
	[Desc("Put this on the actor that gets crushed to replace the crusher with a new actor.")]
	public class TransformCrusherOnCrushInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly string IntoActor = null;

		public readonly bool SkipMakeAnims = true;

		public readonly BitSet<CrushClass> CrushClasses = default;

		public override object Create(ActorInitializer init) { return new TransformCrusherOnCrush(init, this); }
	}

	public class TransformCrusherOnCrush : INotifyCrushed
	{
		readonly TransformCrusherOnCrushInfo info;
		readonly string faction;

		public TransformCrusherOnCrush(ActorInitializer init, TransformCrusherOnCrushInfo info)
		{
			this.info = info;
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses) { }

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			if (!info.CrushClasses.Overlaps(crushClasses))
				return;

			var facing = crusher.TraitOrDefault<IFacing>();
			var transform = new Transform(info.IntoActor) { Faction = faction };
			if (facing != null)
				transform.Facing = facing.Facing;

			transform.SkipMakeAnims = info.SkipMakeAnims;
			crusher.QueueActivity(false, transform);
		}
	}
}
