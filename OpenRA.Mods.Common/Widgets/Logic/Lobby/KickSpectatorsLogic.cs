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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class KickSpectatorsLogic : ChromeLogic
	{
		[TranslationReference("count")]
		const string KickSpectators = "dialog-kick-spectators.prompt";

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
