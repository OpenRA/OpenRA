#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Shown in the build palette widget.")]
	public class TooltipInfo : ITraitInfo, ITooltipInfo
	{
		[Translate] public readonly string Description = "";
		[Translate] public readonly string Name = "";

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

		public virtual object Create(ActorInitializer init) { return new Tooltip(init.self, this); }

		public string TooltipForPlayerStance(Stance stance)
		{
			if (stance == Stance.None || !GenericVisibility.Intersects(stance))
				return Name;

			if (GenericStancePrefix && stance == Stance.Player)
				return "Player " + GenericName;

			if (GenericStancePrefix && stance == Stance.Ally)
				return "Allied " + GenericName;

			if (GenericStancePrefix && stance == Stance.Enemy)
				return "Enemy " + GenericName;

			return GenericName;
		}

		public bool IsOwnerRowVisible { get { return ShowOwnerRow; } }
	}

	public class Tooltip : IToolTip
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