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
	[Desc("This provides a modifier to the player's production speed.", "This goes on a player or an actor.")]
	public class AttributeModifierInfo : ITraitInfo
	{
		[Desc("What type of modifier this is.")]
		public readonly ModifierType ModType = ModifierType.Production;

		[Desc("The types this modifier applies to.")]
		public readonly string[] Types = { "Any" };

		[Desc("The amount of this modifier.", "0 = no adjustment", "-10 = 90% of normal", "10 = 110% of normal")]
		public readonly int Modifier = 0;

		[Desc("Only apply this modifier when these prerequisites are met.")]
		public readonly string[] Prerequisites = { };

		[Desc("Does this affect the player as opposed to just this actor", "true = Manager on PlayerActor is used.")]
		public readonly bool IsGlobal = false;

		[Desc("Is this attached to a player actor", "true = This is on a player")]
		public readonly bool OnPlayerActor = false;

		public object Create(ActorInitializer init) { return new AttributeModifier(init, this); }
	}

	public class AttributeModifier : INotifyAddedToWorld, INotifyRemovedFromWorld, ITechTreeElement
	{
		readonly Actor self;
		public readonly AttributeModifierInfo Info;

		IEnumerable<IAttributeModManager> managers;
		TechTree techTree;

		public AttributeModifier(ActorInitializer init, AttributeModifierInfo info)
		{
			self = init.self;
			Info = info;
		}

		static string MakeKey(Actor self, AttributeModifierInfo info)
		{
			var key = self.ActorID.ToString() + ":" + info.Modifier.ToString();
			if (info.Prerequisites.Any())
				key += String.Join(",",info.Prerequisites);
			return key;
		}

		public void RegisterSelf()
		{
			var key = MakeKey(self, Info);
			foreach (var manager in managers)
				manager.Register(Info.ModType, key, Info.Types, Info.Modifier);
		}

		public void UnregisterSelf()
		{
			var key = MakeKey(self, Info);
			foreach (var manager in managers)
				manager.Unregister(Info.ModType, key, Info.Modifier);
		}

		public void AddedToWorld(Actor self)
		{
			if (Info.OnPlayerActor)
				techTree = self.Trait<TechTree>();
			else
				techTree = self.Owner.PlayerActor.Trait<TechTree>();

			if (Info.IsGlobal && !Info.OnPlayerActor)
				managers = self.Owner.PlayerActor.TraitsImplementing<IAttributeModManager>();
			else
				managers = self.TraitsImplementing<IAttributeModManager>();

			if (Info.Prerequisites.Any())
			{
				techTree.Add(MakeKey(self, Info), Info.Prerequisites, 0, this);
				techTree.Update();
			}
			else
				RegisterSelf();
		}

		public void RemovedFromWorld(Actor self)
		{
			UnregisterSelf();
		}

		public void PrerequisitesAvailable(string key)
		{
			if (MakeKey(self, Info) == key)
				RegisterSelf();
		}

		public void PrerequisitesUnavailable(string key)
		{
			if (MakeKey(self, Info) == key)
				UnregisterSelf();
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
