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
	class LegacyCapturableInfo : ITraitInfo
	{
		[Desc("Type of actor (the LegacyCaptures: trait defines what Types it can capture).")]
		public readonly string Type = "building";
		public readonly bool AllowAllies = false;
		public readonly bool AllowNeutral = true;
		public readonly bool AllowEnemies = true;
		[Desc("Health percentage the target must be at (or below) before it can be captured.")]
		public readonly double CaptureThreshold = 0.5;

		public object Create(ActorInitializer init) { return new LegacyCapturable(init.self, this); }
	}

	class LegacyCapturable
	{
		[Sync] Actor self;
		public LegacyCapturableInfo Info;

		public LegacyCapturable(Actor self, LegacyCapturableInfo info)
		{
			this.self = self;
			Info = info;
		}

		public bool CanBeTargetedBy(Actor captor)
		{
			var c = captor.TraitOrDefault<LegacyCaptures>();
			if (c == null)
				return false;

			var playerRelationship = self.Owner.Stances[captor.Owner];
			if (playerRelationship == Stance.Ally && !Info.AllowAllies)
				return false;

			if (playerRelationship == Stance.Enemy && !Info.AllowEnemies)
				return false;

			if (playerRelationship == Stance.Neutral && !Info.AllowNeutral)
				return false;

			if (!c.Info.CaptureTypes.Contains(Info.Type))
				return false;

			return true;
		}
	}
}
