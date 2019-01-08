#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Transform into a different actor type.")]
	class InfiltrateForTransformInfo : ITraitInfo
	{
		[ActorReference, FieldLoader.Require]
		public readonly string IntoActor = null;

		public readonly int ForceHealthPercentage = 0;

		public readonly bool SkipMakeAnims = true;

		public readonly BitSet<TargetableType> Types = default(BitSet<TargetableType>);

		public object Create(ActorInitializer init) { return new InfiltrateForTransform(init, this); }
	}

	class InfiltrateForTransform : INotifyInfiltrated
	{
		readonly InfiltrateForTransformInfo info;
		readonly string faction;

		public InfiltrateForTransform(ActorInitializer init, InfiltrateForTransformInfo info)
		{
			this.info = info;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			var transform = new Transform(self, info.IntoActor)
			{
				ForceHealthPercentage = info.ForceHealthPercentage,
				Faction = faction,
				SkipMakeAnims = info.SkipMakeAnims
			};

			var facing = self.TraitOrDefault<IFacing>();
			if (facing != null)
				transform.Facing = facing.Facing;

			self.CancelActivity();
			self.QueueActivity(transform);
		}
	}
}
