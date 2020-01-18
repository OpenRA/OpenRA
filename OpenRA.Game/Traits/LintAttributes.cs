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

using System;

namespace OpenRA.Traits
{
	/* attributes used by OpenRA.Lint to understand the rules */

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class ActorReferenceAttribute : Attribute
	{
		public Type[] RequiredTraits;
		public ActorReferenceAttribute(params Type[] requiredTraits)
		{
			RequiredTraits = requiredTraits;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class WeaponReferenceAttribute : Attribute { }

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

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class SequenceReferenceAttribute : Attribute
	{
		public readonly string ImageReference; // The field name in the same trait info that contains the image name.
		public readonly bool Prefix;
		public SequenceReferenceAttribute(string imageReference = null, bool prefix = false)
		{
			ImageReference = imageReference;
			Prefix = prefix;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class GrantedConditionReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class ConsumedConditionReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class PaletteDefinitionAttribute : Attribute
	{
		public readonly bool IsPlayerPalette;
		public PaletteDefinitionAttribute(bool isPlayerPalette = false)
		{
			IsPlayerPalette = isPlayerPalette;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class PaletteReferenceAttribute : Attribute
	{
		public readonly bool IsPlayerPalette;
		public PaletteReferenceAttribute(bool isPlayerPalette = false)
		{
			IsPlayerPalette = isPlayerPalette;
		}

		public readonly string PlayerPaletteReferenceSwitch;
		public PaletteReferenceAttribute(string playerPaletteReferenceSwitch)
		{
			PlayerPaletteReferenceSwitch = playerPaletteReferenceSwitch;
		}
	}
}
