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

using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(typeof(ChangeHeightEditorAction))]
	public sealed class HeightToolLogic : ChromeLogic
	{
		readonly EditorViewportControllerWidget editor;
		readonly EditorHeightBrush heightBrush;

		[ObjectCreator.UseCtor]
		public HeightToolLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			var heightTool = world.WorldActor.Trait<HeightTool>();
			var editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			heightBrush = new EditorHeightBrush(heightTool, world, worldRenderer, editorActionManager);

			editor = widget.Parent.Parent.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.BrushChanged += HandleBrushChanged;

			var sizeDropdown = widget.Get<DropDownButtonWidget>("DROPDOWN");
			sizeDropdown.GetText = () => heightBrush.Size.ToString(CultureInfo.InvariantCulture);
			sizeDropdown.OnClick = () => sizeDropdown.ShowDropDown(
				"LABEL_DROPDOWN_TEMPLATE",
				270,
				Enumerable.Range(heightTool.Info.MinSize, heightTool.Info.MaxSize - heightTool.Info.MinSize + 1),
				SetupItem);

			var direction = widget.Get<ButtonWidget>("DIRECTION");
			direction.OnClick = () => heightBrush.ToggleSetting(HeightTool.Settings.Lower);
			direction.GetText = () => FluentProvider.GetMessage(heightBrush.Settings.HasFlag(HeightTool.Settings.Lower) ? "label-height-down" : "label-height-up");

			var circle = widget.Get<ButtonWidget>("CIRCLE");
			circle.OnClick = () => heightBrush.ToggleSetting(HeightTool.Settings.Circle);
			circle.IsHighlighted = () => heightBrush.Settings.HasFlag(HeightTool.Settings.Circle);

			var paint = widget.Get<ButtonWidget>("PAINT");
			paint.OnClick = () => editor.SetBrush(paint.IsHighlighted() ? null : heightBrush);
			paint.IsHighlighted = () => editor.CurrentBrush is EditorHeightBrush;
		}

		ScrollItemWidget SetupItem(int size, ScrollItemWidget itemTemplate)
		{
			var item = ScrollItemWidget.Setup(itemTemplate, () => heightBrush.Size == size, () => heightBrush.Size = size);
			item.Get<LabelWidget>("LABEL").GetText = () => size.ToString(CultureInfo.InvariantCulture);
			return item;
		}

		void HandleBrushChanged()
		{
			if (editor.CurrentBrush is not EditorHeightBrush)
				heightBrush.Cell = null;
		}

		protected override void Dispose(bool disposing)
		{
			editor.BrushChanged -= HandleBrushChanged;
			base.Dispose(disposing);
		}
	}
}
