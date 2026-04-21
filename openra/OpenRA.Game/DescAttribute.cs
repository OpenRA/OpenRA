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

namespace OpenRA
{
	// Mirrors DescriptionAttribute from System.ComponentModel but we don't want to have to use that everywhere.
	[AttributeUsage(AttributeTargets.All)]
	public sealed class DescAttribute(params string[] lines) : Attribute
	{
		public readonly string[] Lines = lines;
	}
}
