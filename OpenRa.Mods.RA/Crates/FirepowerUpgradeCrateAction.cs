#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class FirepowerUpgradeCrateActionInfo : ITraitInfo
	{
		public float Multiplier = 2.0f;
		public int SelectionShares = 10;
		public object Create(Actor self) { return new FirepowerUpgradeCrateAction(self); }
	}

	class FirepowerUpgradeCrateAction : ICrateAction
	{
		Actor self;
		public FirepowerUpgradeCrateAction(Actor self)
		{
			this.self = self;
		}

		public int SelectionShares
		{
			get { return self.Info.Traits.Get<FirepowerUpgradeCrateActionInfo>().SelectionShares; }
		}

		public void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, "firepo1.aud");
			collector.World.AddFrameEndTask(w =>
			{
				var multiplier = self.Info.Traits.Get<FirepowerUpgradeCrateActionInfo>().Multiplier;
				collector.traits.Add(new FirepowerUpgrade(multiplier));
				w.Add(new CrateEffect(collector, "fpower"));
			});
		}
	}

	class FirepowerUpgrade : IFirepowerModifier
	{
		float multiplier;
		public FirepowerUpgrade(float multiplier) { this.multiplier = multiplier; }
		public float GetFirepowerModifier() { return multiplier; }
	}
}
