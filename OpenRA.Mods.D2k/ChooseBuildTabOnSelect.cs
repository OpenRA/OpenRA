#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.D2k.Widgets;
using OpenRA.Mods.RA;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k
{
	class ChooseBuildTabOnSelectInfo : ITraitInfo
	{
		public readonly string BuildPaletteWidgetName = "INGAME_BUILD_PALETTE";

		public object Create(ActorInitializer init) { return new ChooseBuildTabOnSelect(init, this); }
	}

	class ChooseBuildTabOnSelect : INotifySelection
	{
		readonly World world;
		readonly ChooseBuildTabOnSelectInfo info;

		public ChooseBuildTabOnSelect(ActorInitializer init, ChooseBuildTabOnSelectInfo info)
		{
			world = init.world;
			this.info = info;
		}

		public void SelectionChanged()
		{
			var palette = Ui.Root.GetOrNull<BuildPaletteWidget>(info.BuildPaletteWidgetName);
			if (palette == null)
				return;

			// Queue-per-structure
			var perqueue = world.Selection.Actors.FirstOrDefault(a => a.IsInWorld && a.World.LocalPlayer == a.Owner
				&& a.TraitsImplementing<ProductionQueue>().Any(q => q.Enabled));

			if (perqueue != null)
			{
				palette.SetCurrentTab(perqueue.TraitsImplementing<ProductionQueue>().First(q => q.Enabled));
				return;
			}

			// Queue-per-player
			var types = world.Selection.Actors.Where(a => a.IsInWorld && (a.World.LocalPlayer == a.Owner))
				.SelectMany(a => a.TraitsImplementing<Production>())
				.SelectMany(t => t.Info.Produces)
				.ToArray();

			if (types.Length == 0)
				return;

			palette.SetCurrentTab(world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => q.Enabled && types.Contains(q.Info.Type)));
		}
	}
}
