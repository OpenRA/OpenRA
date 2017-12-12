using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class HelpLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public HelpLogic(Widget widget, ModData modData, Action onExit)
		{
			var panel = widget.Get("HELP_PANEL");

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			var scrollPanel = panel.Get<ScrollPanelWidget>("HELP_DISPLAY");
			var template = scrollPanel.Get<LabelWidget>("HELP_TEMPLATE");
			scrollPanel.RemoveChildren();

			var lines = modData.DefaultFileSystem.Open("HELP").ReadAllLines();
			foreach (var l in lines)
			{
				// Improve the formatting
				var line = l.Replace("\t", "    ").Replace("*", "\u2022");
				var label = template.Clone() as LabelWidget;
				label.GetText = () => line;
				scrollPanel.AddChild(label);
			}
		}
	}
}