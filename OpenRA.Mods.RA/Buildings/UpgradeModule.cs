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
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Buildings
{
	public class UpgradeModuleInfo : TraitInfo<UpgradeModule>
	{
		public readonly string Type = null;
		public readonly DestroyOrSell DestroyOrSell = DestroyOrSell.Destroy;
	}

	public class UpgradeModule { }

	public enum DestroyOrSell
	{
		Destroy = 1,
		Sell = 2,
	}
}
