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

using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Tcd.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Tcd.Widgets.Logic
{
	public sealed class SquadCommandBarLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SquadCommandBarLogic(Widget widget, World world)
		{
			var squads = world.WorldActor.TraitOrDefault<SquadManager>();

			var form = widget.Get<ButtonWidget>("FORM_SQUAD");
			form.IsDisabled = () => squads == null || world.Selection.Actors.Count == 0;
			form.OnClick = () =>
			{
				var squad = squads.Form(world.Selection.Actors);
				if (squad != null)
					TextNotificationsManager.Debug($"Squad {squad.Id} formed: {squad.Members.Count} units.");
			};

			var disband = widget.Get<ButtonWidget>("DISBAND_SQUAD");
			disband.IsDisabled = () => squads == null || world.Selection.Actors.Count == 0;
			disband.OnClick = () =>
			{
				var disbanded = squads.DisbandContaining(world.Selection.Actors);
				if (disbanded > 0)
					TextNotificationsManager.Debug($"Disbanded {disbanded} squad(s).");
			};
		}
	}
}
