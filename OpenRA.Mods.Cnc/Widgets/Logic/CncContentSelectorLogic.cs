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

using System.Collections.Generic;
using OpenRA.Mods.Common.FileSystem;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class CncContentSelectorLogic : ContentSelectorLogic
	{
		[FluentReference]
		const string DiscDetected = "modcontent-content-disc-detected";

		[FluentReference]
		const string DiscNotDetected = "modcontent-content-disc-not-detected";

		[ObjectCreator.UseCtor]
		public CncContentSelectorLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, logicArgs)
		{
			var isoButton = widget.Get<ButtonWidget>("ISO_BUTTON");
			var isoPath = Platform.ResolvePath($"^SupportDir|Content/{Content.Mod}");
			isoButton.OnClick = () => Game.Renderer.TryOpenUrl("file://" + isoPath);

			var detected = FluentProvider.GetMessage(DiscDetected);
			var notDetected = FluentProvider.GetMessage(DiscNotDetected);
			if (!logicArgs.TryGetValue("DiscAttributes", out var yaml))
				return;

			foreach (var node in yaml.Nodes)
			{
				var available = new CachedTransform<ContentSource, bool>(
					s => SourceAttributes[s].TryGetValue("ISOVolumes", out var volumes) && volumes.Contains(node.Key));

				widget.Get(node.Value.Value + "_CONTAINER").Get<LabelWidget>("DETECTED_LABEL").GetText =
					() => available.Update(selected) ? detected : notDetected;
			}
		}
	}
}
