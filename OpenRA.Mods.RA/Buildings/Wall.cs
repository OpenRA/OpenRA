#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class WallInfo : ITraitInfo, ITraitPrerequisite<BuildingInfo>
	{
		public readonly string[] CrushClasses = { };

		public object Create(ActorInitializer init) { return new Wall(init.self, this); }
	}

	public class Wall : ICrushable, IBlocksBullets
	{
		readonly Actor self;
		readonly WallInfo info;
		
		public Wall(Actor self, WallInfo info)
		{
			this.self = self;
			this.info = info;
			self.World.WorldActor.Trait<UnitInfluence>().Add(self, self.Trait<Building>());
		}
		
		public IEnumerable<string> CrushClasses { get { return info.CrushClasses; } }
		public void OnCrush(Actor crusher) { self.Kill(crusher); }
	}
}
