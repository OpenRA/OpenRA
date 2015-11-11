#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Gives resources to the collector.")]
	class GiveResourcesCrateActionInfo : CrateActionInfo
	{
		[Desc("Type of resource to give.")]
		[FieldLoader.Require]
		public readonly string ResourceType;

		[Desc("Amount of cash to give.")]
		public readonly int Amount = 2000;

		[Desc("Should the collected amount be displayed as a cash tick?")]
		public readonly bool UseCashTick = false;

		public override object Create(ActorInitializer init) { return new GiveResourcesCrateAction(init.Self, this); }
	}

	class GiveResourcesCrateAction : CrateAction
	{
		readonly GiveResourcesCrateActionInfo info;
		public GiveResourcesCrateAction(Actor self, GiveResourcesCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				collector.Owner.PlayerActor.Trait<PlayerResources>().GiveResource(info.ResourceType, info.Amount);

				if (info.UseCashTick)
					w.Add(new FloatingText(collector.CenterPosition, collector.Owner.Color.RGB, FloatingText.FormatCashTick(info.Amount), 30));
			});

			base.Activate(collector);
		}
	}
}
