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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class TooltipInfoBase : ConditionalTraitInfo, Requires<IMouseBoundsInfo>
	{
		public readonly string Name = "";
	}

	[Desc("Shown in map editor.")]
	public class EditorOnlyTooltipInfo : TooltipInfoBase
	{
		public override object Create(ActorInitializer init) { return this; }
	}

	[Desc("Shown in the build palette widget.")]
	public class TooltipInfo : TooltipInfoBase, ITooltipInfo
	{
		[Desc("An optional generic name (i.e. \"Soldier\" or \"Structure\")" +
			"to be shown to chosen players.")]
		public readonly string GenericName = null;

		[Desc("Prefix generic tooltip name with 'Ally/Neutral/EnemyPrefix'.")]
		public readonly bool GenericStancePrefix = true;

		[Desc("Prefix to display in the tooltip for allied units.")]
		public readonly string AllyPrefix = "Allied";

		[Desc("Prefix to display in the tooltip for neutral units.")]
		public readonly string NeutralPrefix = null;

		[Desc("Prefix to display in the tooltip for enemy units.")]
		public readonly string EnemyPrefix = "Enemy";

		[Desc("Player stances that the generic name should be shown to.")]
		public readonly PlayerRelationship GenericVisibility = PlayerRelationship.None;

		[Desc("Show the actor's owner and their faction flag")]
		public readonly bool ShowOwnerRow = true;

		public override object Create(ActorInitializer init) { return new Tooltip(init.Self, this); }

		public string TooltipForPlayerStance(PlayerRelationship relationship)
		{
			if (relationship == PlayerRelationship.None || !GenericVisibility.HasRelationship(relationship))
				return Name;

			if (GenericStancePrefix && !string.IsNullOrEmpty(AllyPrefix) && relationship == PlayerRelationship.Ally)
				return AllyPrefix + " " + GenericName;

			if (GenericStancePrefix && !string.IsNullOrEmpty(NeutralPrefix) && relationship == PlayerRelationship.Neutral)
				return NeutralPrefix + " " + GenericName;

			if (GenericStancePrefix && !string.IsNullOrEmpty(EnemyPrefix) && relationship == PlayerRelationship.Enemy)
				return EnemyPrefix + " " + GenericName;

			return GenericName;
		}

		public bool IsOwnerRowVisible => ShowOwnerRow;
	}

	public class Tooltip : ConditionalTrait<TooltipInfo>, ITooltip
	{
		readonly Actor self;
		readonly TooltipInfo info;

		public ITooltipInfo TooltipInfo => info;

		public Player Owner => self.EffectiveOwner != null ? self.EffectiveOwner.Owner : self.Owner;

		public Tooltip(Actor self, TooltipInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
		}
	}
}
