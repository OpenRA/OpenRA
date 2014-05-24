#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	// for some reason I get yelled at for pbox.e1 not having Cargo, but that's a lie?
	class EmitCargoOnSellInfo : TraitInfo<EmitCargoOnSell>//, Requires<Cargo>
	{
	}

	class EmitCargoOnSell : INotifySold
	{
		static void Emit(Actor self)
		{
			// TODO: would like to spill all actors out similar to how we call Unload
		}

		public void Selling(Actor self) { Emit(self); }
		public void Sold(Actor self) { }
	}
}
