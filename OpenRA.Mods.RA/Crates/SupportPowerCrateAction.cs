#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA.Crates
{
	class SupportPowerCrateActionInfo : CrateActionInfo
	{
		[ActorReference]
		public readonly string Proxy = null;
		public override object Create(ActorInitializer init) { return new SupportPowerCrateAction(init.self, this); }
	}

	class SupportPowerCrateAction : CrateAction
	{
		SupportPowerCrateActionInfo Info;
		public SupportPowerCrateAction(Actor self, SupportPowerCrateActionInfo info)
			: base(self, info) { Info = info; }

		// The free unit crate requires same race, and the actor to be at least ITeleportable.
		// We want neither of these properties for crate power proxies.
		public override void Activate(Actor collector)
		{
			base.Activate(collector);

			collector.World.AddFrameEndTask(w => w.CreateActor(Info.Proxy, new TypeDictionary
			{
				new OwnerInit(collector.Owner)
			}));
		}
	}
}
