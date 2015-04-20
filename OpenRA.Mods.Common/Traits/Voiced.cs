#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class VoicedInfo : ITraitInfo
	{
		[VoiceReference] public readonly string VoiceSet = null;

		public object Create(ActorInitializer init) { return new Voiced(init.Self, this); }
	}

	public class Voiced : IVoiced
	{
		public readonly VoicedInfo Info;

		public Voiced(Actor self, VoicedInfo info)
		{
			Info = info;
		}

		public string VoiceSet { get { return Info.VoiceSet; } }
	}
}
