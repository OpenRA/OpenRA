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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	class SupportPowerCrateActionInfo : CrateActionInfo
	{
		public string Power = null;
		public override object Create(ActorInitializer init) { return new SupportPowerCrateAction(init.self, this); }
	}

	class SupportPowerCrateAction : CrateAction
	{
		public SupportPowerCrateAction(Actor self, SupportPowerCrateActionInfo info)
			: base(self, info) { }

		public override void Activate(Actor collector)
		{
			var p = collector.Owner.PlayerActor.traits.WithInterface<SupportPower>()
				.FirstOrDefault(sp => sp.GetType().Name == (info as SupportPowerCrateActionInfo).Power);

			if (p != null) p.Give(1);

			base.Activate(collector);
		}
	}
}
