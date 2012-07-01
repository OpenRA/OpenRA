using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA
{
	/// <summary>
	/// Represents a circular zone on the map that is temporarily potentially harmful to units, e.g. a potential projectile blast zone.
	/// </summary>
	public class DangerZone
	{
		public Actor CreatedBy;
		public int CreatedFrame;
		public CPos CellLocation;
		public PPos PixelLocation;
		public int PixelRadius;
		public int CellRadiusSquared;
	}
}
