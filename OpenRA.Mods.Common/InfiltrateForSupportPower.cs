#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class InfiltrateForSupportPowerInfo : ITraitInfo, Requires<InfiltratableInfo>
	{
		[ActorReference] public readonly string Proxy = null;
		public object Create(ActorInitializer init) { return new InfiltrateForSupportPower(this); }
	}

	class InfiltrateForSupportPower : IAcceptInfiltrator
	{
		InfiltrateForSupportPowerInfo Info;

		public InfiltrateForSupportPower(InfiltrateForSupportPowerInfo info)
		{
			Info = info;
		}

		public void OnInfiltrate(Actor self, Actor infiltrator)
		{
			infiltrator.World.AddFrameEndTask(w => w.CreateActor(Info.Proxy, new TypeDictionary
			{
				new OwnerInit(infiltrator.Owner)
			}));
		}
	}
}
