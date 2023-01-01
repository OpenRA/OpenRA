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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Gives a supportpower to the collector.")]
	class SupportPowerCrateActionInfo : CrateActionInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Which proxy actor, which grants the support power, to spawn.")]
		public readonly string Proxy = null;

		public override object Create(ActorInitializer init) { return new SupportPowerCrateAction(init.Self, this); }
	}

	class SupportPowerCrateAction : CrateAction
	{
		readonly SupportPowerCrateActionInfo info;
		public SupportPowerCrateAction(Actor self, SupportPowerCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		// The free unit crate requires same faction and the actor needs to be mobile.
		// We want neither of these properties for crate power proxies.
		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w => w.CreateActor(info.Proxy, new TypeDictionary
			{
				new OwnerInit(collector.Owner)
			}));

			base.Activate(collector);
		}
	}
}
