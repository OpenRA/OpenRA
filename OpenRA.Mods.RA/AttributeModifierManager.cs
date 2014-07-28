#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This keeps track of player's modifier to production speed.", "This goes on a player or an actor.")]
	public class AttributeModifierManagerInfo : ITraitInfo
	{
		[Desc("What type of modifiers this keeps track of.")]
		public readonly ModifierType ModType = ModifierType.Production;

		[Desc("The type of production this manager keeps track of.", "Adding 'Any' will apply this to all types.")]
		public readonly string Type = "Any";

		public object Create(ActorInitializer init) { return new AttributeModifierManager(init, this); }
	}

	public class AttributeModifierManager : IAttributeModManager, ISync
	{
		public readonly AttributeModifierManagerInfo Info;

		public List<string> Modifiers = new List<string>();
		[Sync] public int ModValue = 0;

		public AttributeModifierManager(ActorInitializer init, AttributeModifierManagerInfo info)
		{
			Info = info;
		}

		public ModifierType ModType
		{
			get
			{
				return Info.ModType;
			}
		}

		public int GetModifier(ModifierType modType, string Type)
		{
			if (Info.Type == Type && Info.ModType == modType)
				return ModValue;

			return 0;
		}

		public void Register(ModifierType modType, string id, string[] types, int mod)
		{
			if (Info.ModType == modType &&
				(types.Contains("Any") || types.Contains(Info.Type)) &&
				!Modifiers.Contains(id))
			{
				Modifiers.Add(id);
				ModValue += mod;
			}
		}

		public void Unregister(ModifierType modType, string id, int mod)
		{
			if (Info.ModType == modType && Modifiers.Contains(id))
			{
				Modifiers.Remove(id);
				ModValue -= mod;
			}
		}
	}
}
