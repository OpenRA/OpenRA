#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.Common
{
	[Desc("Handle demolitions from C4 explosives.")]
	public class DemolishableInfo : TraitInfo<Demolishable> { }

	public class Demolishable : IDemolishable
	{
		public Demolishable() { }

		public void Demolish(Actor self, Actor saboteur)
		{
			self.Kill(saboteur);
		}

		public bool IsValidTarget(Actor self, Actor saboteur)
		{
			return true;
		}
	}
}

