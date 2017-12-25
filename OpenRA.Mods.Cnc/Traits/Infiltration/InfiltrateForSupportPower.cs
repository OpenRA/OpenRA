#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class InfiltrateForSupportPowerInfo : ITraitInfo
	{
		[ActorReference, FieldLoader.Require] public readonly string Proxy = null;

		public readonly HashSet<string> Types = new HashSet<string>();

		public object Create(ActorInitializer init) { return new InfiltrateForSupportPower(this); }
	}

	class InfiltrateForSupportPower : INotifyInfiltrated
	{
		readonly InfiltrateForSupportPowerInfo info;

		public InfiltrateForSupportPower(InfiltrateForSupportPowerInfo info)
		{
			this.info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, HashSet<string> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			infiltrator.World.AddFrameEndTask(w => w.CreateActor(info.Proxy, new TypeDictionary
			{
				new OwnerInit(infiltrator.Owner)
			}));
		}
	}
}
