#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.Player | SystemActors.EditorPlayer)]
	public class TSPlayerResourcesInfo : PlayerResourcesInfo
	{
		[Desc("Amount of veins needed to trigger the chemical missile")]
		public readonly int TriggerChemicalMissileOnVeinsAmount = 56;

		public override object Create(ActorInitializer init) { return new TSPlayerResources(init.Self, this); }
	}

	public class TSPlayerResources : PlayerResources
	{
		public TSPlayerResources(Actor self, PlayerResourcesInfo info)
		: base(self, info)
		{
		}

		[VerifySync]
		public int Veins;

		public int TriggerChemicalMissileOnVeinsAmount
		{
			get
			{
				var tsPlayerResourcesInfo = (TSPlayerResourcesInfo) Info;
				return tsPlayerResourcesInfo.TriggerChemicalMissileOnVeinsAmount;
			}
		}
	}
}
