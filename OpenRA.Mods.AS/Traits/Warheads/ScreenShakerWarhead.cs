#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("This warhead shakes the area when detonated.")]
	public class ScreenShakerWarhead : WarheadAS
	{
		[FieldLoader.Require]
		[Desc("The intensity of the shake.")]
		public readonly int Intensity;

		[FieldLoader.Require]
		[Desc("The duration of the shake.")]
		public readonly int Duration;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var screenShaker = firedBy.World.WorldActor.TraitOrDefault<ScreenShaker>();

			if (screenShaker != null)
				screenShaker.AddEffect(Duration, target.CenterPosition, Intensity);
		}
	}
}
