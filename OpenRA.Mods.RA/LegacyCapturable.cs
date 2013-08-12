#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
	[Desc("This actor can be captured by a unit with LegacyCaptures: trait.")]
	class LegacyCapturableInfo : TraitInfo<LegacyCapturable>
	{
		[Desc("Type of actor (the LegacyCaptures: trait defines what Types it can capture).")]
		public readonly string Type = "building";
		public readonly bool AllowAllies = false;
		public readonly bool AllowNeutral = true;
		public readonly bool AllowEnemies = true;
		[Desc("Health percentage the target must be at (or below) before it can be captured.")]
		public readonly float CaptureThreshold = 0.5f;

		public bool CanBeTargetedBy(Actor captor, Player owner)
		{
			var c = captor.TraitOrDefault<LegacyCaptures>();
			if (c == null)
				return false;

			var playerRelationship = owner.Stances[captor.Owner];
			if (playerRelationship == Stance.Ally && !AllowAllies)
				return false;

			if (playerRelationship == Stance.Enemy && !AllowEnemies)
				return false;

			if (playerRelationship == Stance.Neutral && !AllowNeutral)
				return false;

			if (!c.Info.CaptureTypes.Contains(Type))
				return false;

			return true;
		}
	}

	class LegacyCapturable { }
}
