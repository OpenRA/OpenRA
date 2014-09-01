#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using System.Drawing;

namespace OpenRA.ObserverUIEditor
{
	public class YamlSprite
	{
		public string Collection;
		public string Bitmap;
		public string Name;
		public string RectStr;
		public Rectangle Rect;

		public override string ToString()
		{
			return Collection + "." + Name;
		}
	}
}
