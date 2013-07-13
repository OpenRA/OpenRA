﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class MPStartUnitsInfo : TraitInfo<MPStartUnits>
	{
		public readonly string Class = "default";
		public readonly string[] Races = { };

		public readonly string BaseActor = null;
		public readonly string[] SupportActors = { };

		[Desc("Inner radius for spawning support actors")]
		public readonly int InnerSupportRadius = 2;

		[Desc("Outer radius for spawning support actors")]
		public readonly int OuterSupportRadius = 4;
	}

	public class MPStartUnits { }
}
