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
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ProvidesPrerequisiteInfo : ConditionalTraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides. If left empty it defaults to the actor's name.")]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = Array.Empty<string>();

		[Desc("Only grant this prerequisite for certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = false;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return new string[] { Prerequisite ?? info.Name };
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisite(init, this); }
	}

	public class ProvidesPrerequisite : ConditionalTrait<ProvidesPrerequisiteInfo>, ITechTreePrerequisite, INotifyOwnerChanged, INotifyCreated
	{
		readonly string prerequisite;

		bool enabled;
		TechTree techTree;
		string faction;

		public ProvidesPrerequisite(ActorInitializer init, ProvidesPrerequisiteInfo info)
			: base(info)
		{
			prerequisite = info.Prerequisite;

			if (string.IsNullOrEmpty(prerequisite))
				prerequisite = init.Self.Info.Name;

			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!enabled)
					yield break;

				yield return prerequisite;
			}
		}

		protected override void Created(Actor self)
		{
			techTree = self.Owner.PlayerActor.Trait<TechTree>();

			Update();

			base.Created(self);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();

			if (Info.ResetOnOwnerChange)
				faction = newOwner.Faction.InternalName;

			Update();
		}

		void Update()
		{
			enabled = !IsTraitDisabled;
			if (IsTraitDisabled)
				return;

			if (Info.Factions.Count > 0)
				enabled = Info.Factions.Contains(faction);

			if (Info.RequiresPrerequisites.Length > 0 && enabled)
				enabled = techTree.HasPrerequisites(Info.RequiresPrerequisites);
		}

		protected override void TraitEnabled(Actor self)
		{
			Update();
			techTree.ActorChanged(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			Update();
			techTree.ActorChanged(self);
		}
	}
}
