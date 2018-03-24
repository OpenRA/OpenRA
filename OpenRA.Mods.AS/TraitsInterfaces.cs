#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[RequireExplicitImplementation]
	public interface ISmokeParticleInfo
	{
		string Image { get; }
		string Sequence { get; }
		string Palette { get; }

		int[] Duration { get; }

		WVec[] Gravity { get; }

		WeaponInfo Weapon { get; }
	}
}
