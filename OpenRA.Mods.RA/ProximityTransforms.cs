#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Actor will be transformed by mobile units in a specified proximity.")]
	public class ProxmityTransformsInfo : ITraitInfo, Requires<TransformsInfo>
	{
		[Desc("Cells")]
		public readonly int Range = 3;

		[Desc("Units around have to appear friendly.")]
		public readonly bool CheckFriendly = true;

		[Desc("Trigger when no units are around.")]
		public readonly bool WhenAlone = false;

		public object Create(ActorInitializer init) { return new ProxmityTransforms(init.self, this); }
	}

	public class ProxmityTransforms : ITick
	{
		readonly Transforms transforms;
		readonly ProxmityTransformsInfo info;
		readonly Actor self;

		public ProxmityTransforms(Actor self, ProxmityTransformsInfo info)
		{
			this.info = info;
			this.self = self;

			transforms = self.Trait<Transforms>();
		}

		public void Tick(Actor self)
		{
			if (info.WhenAlone)
			{
				if (info.CheckFriendly)
				{
					if (!UnitsInRange().Any(a => a.AppearsFriendlyTo(self)))
						transforms.DeployTransform(true);
				}
				else
					if (!UnitsInRange().Any())
						transforms.DeployTransform(true);
			}
			else
			{
				if (info.CheckFriendly)
				{
					if (UnitsInRange().Any(a => a.AppearsFriendlyTo(self)))
						transforms.DeployTransform(true);
				}
				else
					if (UnitsInRange().Any())
						transforms.DeployTransform(true);
			}
		}

		IEnumerable<Actor> UnitsInRange()
		{
			return self.World.FindActorsInCircle(self.CenterPosition, WRange.FromCells(info.Range))
				.Where(a => a.IsInWorld && a != self && !a.Destroyed && !a.Owner.NonCombatant && a.HasTrait<Mobile>());
		}
	}
}
