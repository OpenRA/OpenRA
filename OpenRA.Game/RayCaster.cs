#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA
{
	// Algorithm obtained from ftp://ftp.isc.org/pub/usenet/comp.sources.unix/volume26/line3d
	public class Raycaster
	{
		public static IEnumerable<CPos> Raycast(CPos source, CPos target)
		{
			var xDelta = target.X - source.X;
			var yDelta = target.Y - source.Y;

			var xAbsolute = Math.Abs(xDelta) << 1;
			var yAbsolute = Math.Abs(yDelta) << 1;

			var xIncrement = (xDelta < 0) ? -1 : xDelta > 0 ? 1 : 0;
			var yIncrement = (yDelta < 0) ? -1 : yDelta > 0 ? 1 : 0;

			var x = source.X;
			var y = source.Y;

			if (xAbsolute >= yAbsolute)
			{
				var error = yAbsolute - (xAbsolute >> 1);

				do
				{
					yield return new CPos(x, y);

					if (error >= 0)
					{
						y += yIncrement;
						error -= xAbsolute;
					}

					x += xIncrement;
					error += yAbsolute;
				}
				while (y != target.Y);
				yield return new CPos(x, y);
			}
			else
			{
				var error = xAbsolute - (yAbsolute >> 1);
				do
				{
					yield return new CPos(x, y);

					if (error >= 0)
					{
						x += xIncrement;
						error -= yAbsolute;
					}

					y += yIncrement;
					error += xAbsolute;
				}
				while (y != target.Y);
				yield return new CPos(x, y);
			}
		}
	}
}
