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

using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class SellableProperties : ScriptActorProperties, Requires<SellableInfo>
	{
		public SellableProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Start selling the actor.")]
		public void Sell()
		{
			// PERF: No trait lookup cache in the constructor to avoid doing it for all buildings except just the ones getting sold.
			Self.Trait<Sellable>().Sell(Self);
		}
	}
}
