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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation.Objects
{
	public class ExtractedClassInfo
	{
		public string Namespace { get; set; }

		public string Name { get; set; }

		public string Filename { get; set; }

		public string Description { get; set; }

		public IEnumerable<string> InheritedTypes { get; set; }

		public IEnumerable<ExtractedClassFieldInfo> Properties { get; set; }
	}
}
