﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	class InfiltrateForSupportPowerInfo : ITraitInfo
	{
		[ActorReference] public readonly string Proxy = null;
		public object Create(ActorInitializer init) { return new InfiltrateForSupportPower(this); }
	}

	class InfiltrateForSupportPower : IAcceptSpy
	{
		InfiltrateForSupportPowerInfo Info;

		public InfiltrateForSupportPower(InfiltrateForSupportPowerInfo info)
		{
			Info = info;
		}

		public void OnInfiltrate(Actor self, Actor spy)
		{
			spy.World.AddFrameEndTask(w => w.CreateActor(Info.Proxy, new TypeDictionary
			{
				new OwnerInit( spy.Owner )
			}));
		}
	}
}
