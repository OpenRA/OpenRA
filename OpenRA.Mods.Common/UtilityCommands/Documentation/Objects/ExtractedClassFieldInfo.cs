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
	public class ExtractedClassFieldInfo
	{
		public string PropertyName { get; set; }

		public string DefaultValue { get; set; }

		public string InternalType { get; set; }

		public string UserFriendlyType { get; set; }

		public string Description { get; set; }

		public IEnumerable<ExtractedClassFieldAttributeInfo> OtherAttributes { get; set; }
	}
}
