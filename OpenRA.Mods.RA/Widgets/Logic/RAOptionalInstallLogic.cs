#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class RAOptionalInstallMenuLogic
	{
		public static int ContentID = 0;

		[ObjectCreator.UseCtor]
		public RAOptionalInstallMenuLogic(Widget widget)
		{
			var panel = widget.Get("INSTALL_OPTIONAL_CONTENT_PANEL");
			panel.Get<ButtonWidget>("INSTALL_BUTTON").Disabled = true;

			var contentdropdown = panel.Get<DropDownButtonWidget>("CONTENT_DISC");
			contentdropdown.OnMouseDown = _ =>
				{
					var options = new List<DropDownOption>
					{
						new DropDownOption
						{
							Title = "Allies Disc",
							IsSelected = () => ContentID == 0,
							OnClick = () => 
								{
									contentdropdown.GetText = () => "Allies Disc";
									ContentID = 0;
									panel.Get<ButtonWidget>("INSTALL_BUTTON").Disabled = false;
								}
						},
						new DropDownOption
						{
							Title = "Soviet Disc",
							IsSelected = () => ContentID == 1,
							OnClick = () =>
								{
									contentdropdown.GetText = () => "Soviet Disc";
									ContentID = 1;
									panel.Get<ButtonWidget>("INSTALL_BUTTON").Disabled = false;
								}
						},
						new DropDownOption
						{
							Title = "Counterstrike Disc",
							IsSelected = () => ContentID == 1,
							OnClick = () =>
								{
									contentdropdown.GetText = () => "Counterstrike Disc";
									ContentID = 2;
									//panel.Get<ButtonWidget>("INSTALL_BUTTON").Disabled = false;
								}
						},
						new DropDownOption
						{
							Title = "Aftermath Disc",
							IsSelected = () => ContentID == 1,
							OnClick = () =>
								{
									contentdropdown.GetText = () => "Aftermath Disc";
									ContentID = 3;
									//panel.Get<ButtonWidget>("INSTALL_BUTTON").Disabled = false;
								}
						}
					};
				Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};
				contentdropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, setupItem);
				};

			panel.Get<ButtonWidget>("INSTALL_BUTTON").OnClick = () =>
				{
					switch (ContentID)
					{
						case 0:
							Ui.OpenWindow("INSTALL_FROMCD_ALLIES_PANEL");
							break;
						case 1:
							Ui.OpenWindow("INSTALL_FROMCD_SOVIET_PANEL");
							break;
						case 2:
							//Ui.OpenWindow("INSTALL_FROMCD_COUNTERSTRIKE_PANEL");
							break;
						case 3:
							//Ui.OpenWindow("INSTALL_FROMCD_AFTERMATH_PANEL");
							break;
						default:
							break;
					}
				};
			panel.Get<ButtonWidget>("QUIT_BUTTON").OnClick = () => Ui.CloseWindow();
		}

		class DropDownOption
			{
				public string Title;
				public Func<bool> IsSelected;
				public Action OnClick;
			}
		}
	}
