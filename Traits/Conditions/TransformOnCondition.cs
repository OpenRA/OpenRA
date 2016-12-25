#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class TransformOnConditionInfo : ConditionalTraitInfo
	{
		[ActorReference]
		public readonly string IntoActor = null;
		public readonly int ForceHealthPercentage = 0;
		public readonly bool SkipMakeAnims = true;

		public override object Create(ActorInitializer init) { return new TransformOnCondition(init, this); }
	}

	public class TransformOnCondition : ConditionalTrait<TransformOnConditionInfo>
	{
		readonly TransformOnConditionInfo info;
		readonly string faction;

		public TransformOnCondition(ActorInitializer init, TransformOnConditionInfo info)
			: base(info)
		{
			this.info = info;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		protected override void TraitEnabled(Actor self)
		{
			var facing = self.TraitOrDefault<IFacing>();
			var transform = new Transform(self, info.IntoActor) { ForceHealthPercentage = info.ForceHealthPercentage, Faction = faction };
			if (facing != null) transform.Facing = facing.Facing;
			transform.SkipMakeAnims = info.SkipMakeAnims;
			self.CancelActivity();
			self.QueueActivity(transform);
		}
	}
}
