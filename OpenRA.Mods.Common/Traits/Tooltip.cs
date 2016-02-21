#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	public abstract class TooltipInfoBase : ITraitInfo
	{
		[Translate] public readonly string Description = "";
		[Translate] public readonly string Name = "";

		public abstract object Create(ActorInitializer init);
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
		[Translate] public readonly string GenericName = null;

		[Desc("Prefix generic tooltip name with 'Enemy' or 'Allied'.")]
		public readonly bool GenericStancePrefix = true;

		[Desc("Player stances that the generic name should be shown to.")]
		public readonly Stance GenericVisibility = Stance.None;

		[Desc("Show the actor's owner and their faction flag")]
		public readonly bool ShowOwnerRow = true;

		[Desc("Sequence of the actor that contains the cameo.")]
		public readonly string Icon = "icon";

		public override object Create(ActorInitializer init) { return new Tooltip(init.Self, this); }

		public string TooltipForPlayerStance(Stance stance)
		{
			if (stance == Stance.None || !GenericVisibility.HasStance(stance))
				return Name;

			if (GenericStancePrefix && stance == Stance.Ally)
				return "Allied " + GenericName;

			if (GenericStancePrefix && stance == Stance.Enemy)
				return "Enemy " + GenericName;

			return GenericName;
		}

		public bool IsOwnerRowVisible { get { return ShowOwnerRow; } }
	}

	public class Tooltip : ITooltip
	{
		readonly Actor self;
		readonly TooltipInfo info;

		public ITooltipInfo TooltipInfo { get { return info; } }
		public Player Owner { get { return self.Owner; } }

		public Tooltip(Actor self, TooltipInfo info)
		{
			this.self = self;
			this.info = info;
		}
	}
}