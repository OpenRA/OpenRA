#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Traits
{
	public enum TraitMultiplicity
	{
		None = 0,
		OnePerActor,
		ManyPerActor,
	}

	/// <summary>
	/// Declare wether one or multiple instances of a trait can be associated with a single actor. If only a single instance
	/// of a trait can be associated with an actor - the traits of this type will be stored in dictionary rather than a list
	/// allowing for faster an linear lookup times per actor.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
	public sealed class TraitMultiplicityAttribute : Attribute
	{
		public readonly TraitMultiplicity Multiplicity;
		public TraitMultiplicityAttribute(TraitMultiplicity multiplicity) { Multiplicity = multiplicity; }
	}
}
