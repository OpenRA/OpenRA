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

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HideMapCrateActionInfo : CrateActionInfo
	{
		public override object Create(Actor self) { return new HideMapCrateAction(self, this); }
	}

	class HideMapCrateAction : CrateAction
	{
		public HideMapCrateAction(Actor self, HideMapCrateActionInfo info)
			: base(self, info) {}

		public override void Activate(Actor collector)
		{
			collector.Owner.Shroud.ResetExplored();
			base.Activate(collector);
		}
	}
}
