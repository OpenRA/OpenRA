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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LobbyBriefingLogic : ChromeLogic
	{
		readonly Func<MapPreview> getMap;
		MapPreview mapPreview;

		readonly LabelWidget description;
		readonly SpriteFont descriptionFont;

		[ObjectCreator.UseCtor]
		internal LobbyBriefingLogic(Widget widget, Func<MapPreview> getMap)
		{
			this.getMap = getMap;

			description = widget.Get<LabelWidget>("MISSION_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[description.Font];

			mapPreview = getMap();
			LoadBriefing();
		}

		public override void Tick()
		{
			var newMapPreview = getMap();
			if (newMapPreview == mapPreview)
				return;

			// We are currently enumerating the widget tree and so can't modify any layout
			// Defer it to the end of tick instead
			Game.RunAfterTick(() =>
			{
				mapPreview = newMapPreview;
				LoadBriefing();
			});
		}

		void LoadBriefing()
		{
			if (mapPreview == null || mapPreview.WorldActorInfo == null)
				return;

			var missionData = mapPreview.WorldActorInfo.TraitInfoOrDefault<MissionDataInfo>();
			if (missionData == null)
				return;

			var briefing = missionData.Briefing != null ? mapPreview.GetString(missionData.Briefing) : "";
			var wrapped = WidgetUtils.WrapText(briefing, description.Bounds.Width, descriptionFont);
			var height = descriptionFont.Measure(wrapped).Y;

			Game.RunAfterTick(() =>
			{
				description.GetText = () => wrapped;
				description.Bounds.Height = height;
			});
		}
	}
}
