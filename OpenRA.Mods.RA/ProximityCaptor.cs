#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Actor can capture ProximityCapturable actors.")]
	public class ProximityCaptorInfo : ITraitInfo
	{
		public readonly string[] Types = {};
		public object Create(ActorInitializer init) { return new ProximityCaptor(this); }
	}

	public class ProximityCaptor
	{
		public readonly ProximityCaptorInfo Info;

		public ProximityCaptor(ProximityCaptorInfo info) { Info = info; }

		public bool HasAny(string[] typesList)
		{
			return typesList.Any(flag => Info.Types.Contains(flag));
		}
	}
}
