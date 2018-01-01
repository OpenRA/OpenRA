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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RearmableInfo : ConditionalTraitInfo
	{
		[Desc("Actors that this actor can dock to and get rearmed by.")]
		[FieldLoader.Require]
		[ActorReference] public readonly HashSet<string> RearmActors = new HashSet<string> { };

		public override object Create(ActorInitializer init) { return new Rearmable(this); }
	}

	public class Rearmable : ConditionalTrait<RearmableInfo>
	{
		public Rearmable(RearmableInfo info) : base(info) { }
	}
}
