#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class TSWasteRefineryInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<IDockHostInfo>
	{
		public readonly bool ShowTicks = true;
		public readonly int TickRate = 10;

		public override object Create(ActorInitializer init) { return new TSWasteRefinery(init.Self, this); }
	}

	public class TSWasteRefinery : IAcceptResources, ITick
	{
		readonly TSWasteRefineryInfo info;
		readonly TSPlayerResources playerResources;

		int currentDisplayTick = 0;
		int currentDisplayValue = 0;
		public TSWasteRefinery(Actor self, TSWasteRefineryInfo info)
		{
			this.info = info;
			playerResources = self.Owner.PlayerActor.Trait<TSPlayerResources>();
			currentDisplayTick = info.TickRate;
		}

		int IAcceptResources.AcceptResources(Actor self, string resourceType, int count)
		{
			var veinFullAmount = playerResources.TriggerChemicalMissileOnVeinsAmount;
			var currentVeins = playerResources.Veins;

			var amountToFull = veinFullAmount - currentVeins;

			int contribution;

			if (count > amountToFull)
			{
				contribution = amountToFull;
			}
			else
			{
				contribution = count;
			}

			// Debug.WriteLine($"veinFullAmount={veinFullAmount}, currentVeins={currentVeins}, amountToFull={amountToFull}, count={count}, contribution={contribution}");
			playerResources.Veins += contribution;
			currentDisplayValue += contribution;
			return contribution;
		}

		void ITick.Tick(Actor self)
		{
			if (info.ShowTicks && currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.OwnerColor(), FloatingText.FormatCashTick(temp), 30)));
				currentDisplayTick = info.TickRate;
				currentDisplayValue = 0;
			}
		}
	}
}
