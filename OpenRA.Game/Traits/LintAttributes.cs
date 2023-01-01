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

namespace OpenRA.Traits
{
	[Flags]
	public enum LintDictionaryReference
	{
		None = 0,
		Keys = 1,
		Values = 2
	}

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class ActorReferenceAttribute : Attribute
	{
		public readonly Type[] RequiredTraits;
		public readonly LintDictionaryReference DictionaryReference;

		public ActorReferenceAttribute(Type[] requiredTraits,
			LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			RequiredTraits = requiredTraits;
			DictionaryReference = dictionaryReference;
		}

		public ActorReferenceAttribute(Type requiredTrait = null,
			LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			RequiredTraits = requiredTrait != null ? new[] { requiredTrait } : Array.Empty<Type>();
			DictionaryReference = dictionaryReference;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class WeaponReferenceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class SequenceReferenceAttribute : Attribute
	{
		// The field name in the same trait info that contains the image name.
		public readonly string ImageReference;
		public readonly bool Prefix;
		public readonly bool AllowNullImage;
		public readonly LintDictionaryReference DictionaryReference;

		public SequenceReferenceAttribute(string imageReference = null, bool prefix = false, bool allowNullImage = false,
			LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			ImageReference = imageReference;
			Prefix = prefix;
			AllowNullImage = allowNullImage;
			DictionaryReference = dictionaryReference;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class CursorReferenceAttribute : Attribute
	{
		public readonly LintDictionaryReference DictionaryReference;

		public CursorReferenceAttribute(LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			DictionaryReference = dictionaryReference;
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

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class TraitLocationAttribute : Attribute
	{
		public readonly SystemActors SystemActors;
		public TraitLocationAttribute(SystemActors systemActors)
		{
			SystemActors = systemActors;
		}
	}
}
