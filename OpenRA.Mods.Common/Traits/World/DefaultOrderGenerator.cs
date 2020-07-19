#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DefaultOrderGeneratorInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new DefaultOrderGenerator(); }
	}

	public class DefaultOrderGenerator : IDefaultOrderGenerator
	{
		public DefaultOrderGenerator() { }

		IOrderGenerator IDefaultOrderGenerator.CreateDefaultOrderGenerator()
		{
			return new UnitOrderGenerator();
		}
	}
}
