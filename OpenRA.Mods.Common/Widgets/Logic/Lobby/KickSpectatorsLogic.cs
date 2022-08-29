#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class KickSpectatorsLogic : ChromeLogic
	{
		[TranslationReference("count")]
		static readonly string KickSpectators = "kick-spectators";

		[ObjectCreator.UseCtor]
		public KickSpectatorsLogic(ModData modData, Widget widget, int clientCount, Action okPressed, Action cancelPressed)
		{
			var kickMessage = modData.Translation.GetString(KickSpectators, Translation.Arguments("count", clientCount));
			widget.Get<LabelWidget>("TEXT").GetText = () => kickMessage;

			widget.Get<ButtonWidget>("OK_BUTTON").OnClick = () =>
			{
				widget.Parent.RemoveChild(widget);
				okPressed();
			};

			widget.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				widget.Parent.RemoveChild(widget);
				cancelPressed();
			};
		}
	}
}
