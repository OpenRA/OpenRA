#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA
{
	public class WebServices : IGlobalModData
	{
		public readonly string ServerList = "http://master.openra.net/";
		public readonly string MapRepository = "http://resource.openra.net/map/";
		public readonly string GameNews = "http://master.openra.net/gamenews";
	}
}
