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

namespace OpenRA.Mods.Common.Traits
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class VoiceSetReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class VoiceReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class LocomotorReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class NotificationReferenceAttribute : Attribute
	{
		public readonly string NotificationTypeFieldName = null;
		public readonly string NotificationType = null;

		public NotificationReferenceAttribute(string type = null, string typeFromField = null)
		{
			NotificationType = type;
			NotificationTypeFieldName = typeFromField;
		}
	}
}
