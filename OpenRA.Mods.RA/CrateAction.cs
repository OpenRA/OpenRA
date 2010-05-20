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
	public class CrateActionInfo : ITraitInfo
	{
		public int SelectionShares = 10;
		public string Effect = null;
		public string Notification = null;
		public virtual object Create(Actor self) { return new CrateAction(self, this); }
	}

	public class CrateAction
	{
		public Actor self;
		public CrateActionInfo info;
		
		public CrateAction(Actor self, CrateActionInfo info)
		{
			this.self = self;
			this.info = info;
		}
		
		public virtual int GetSelectionShares(Actor collector)
		{
			return info.SelectionShares;
		}
		
		public virtual void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, info.Notification);
			
			collector.World.AddFrameEndTask(w => 
			{
				if (info.Effect != null)
					w.Add(new CrateEffect(collector, info.Effect));
			});
		}
	}
}
