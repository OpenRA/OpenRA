#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.Traits
{
	/* attributes used by RALint to understand the rules */

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class ActorReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class WeaponReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class VoiceReferenceAttribute : Attribute { }
}
