#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Grants the collector the ability to cloak.")]
	public class CloakCrateActionInfo : CrateActionInfo
	{
		[Desc("The range to search for extra collectors in.", "Extra collectors will also be granted the crate action.")]
		public readonly WRange Range = new WRange(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new CloakCrateAction(init.self, this); }
	}

	public class CloakCrateAction : CrateAction
	{
		CloakCrateActionInfo Info;

		public CloakCrateAction(Actor self, CloakCrateActionInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override int GetSelectionShares(Actor collector)
		{
			var cloak = collector.TraitOrDefault<Cloak>();
			if (cloak == null || !cloak.AcceptsCloakCrate)
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			collector.Trait<Cloak>().ReceivedCloakCrate(collector);

			var inRange = self.World.FindActorsInCircle(self.CenterPosition, Info.Range);
			inRange = inRange.Where(a =>
				(a.Owner == collector.Owner) &&
				(a != collector) &&
				(a.TraitOrDefault<Cloak>() != null) &&
				(a.TraitOrDefault<Cloak>().AcceptsCloakCrate));

			if (inRange.Any())
			{
				if (Info.MaxExtraCollectors > -1)
					inRange = inRange.Take(Info.MaxExtraCollectors);

				if (inRange.Any())
					foreach (var actor in inRange)
						actor.Trait<Cloak>().ReceivedCloakCrate(actor);
			}

			base.Activate(collector);
		}
	}
}
