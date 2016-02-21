#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA
{
	public class NullInputHandler : IInputHandler
	{
		// ignore all input
		public void ModifierKeys(Modifiers mods) { }
		public void OnKeyInput(KeyInput input) { }
		public void OnTextInput(string text) { }
		public void OnMouseInput(MouseInput input) { }
	}

	public class DefaultInputHandler : IInputHandler
	{
		readonly World world;
		public DefaultInputHandler(World world)
		{
			this.world = world;
		}

		public void ModifierKeys(Modifiers mods)
		{
			Game.HandleModifierKeys(mods);
		}

		public void OnKeyInput(KeyInput input)
		{
			Sync.CheckSyncUnchanged(world, () => Ui.HandleKeyPress(input));
		}

		public void OnTextInput(string text)
		{
			Sync.CheckSyncUnchanged(world, () => Ui.HandleTextInput(text));
		}

		public void OnMouseInput(MouseInput input)
		{
			Sync.CheckSyncUnchanged(world, () => Ui.HandleInput(input));
		}
	}

	public class MouseButtonPreference
	{
		public MouseButton Action
		{
			get
			{
				return Game.Settings.Game.UseClassicMouseStyle ? MouseButton.Left : MouseButton.Right;
			}
		}

		public MouseButton Cancel
		{
			get
			{
				return Game.Settings.Game.UseClassicMouseStyle ? MouseButton.Right : MouseButton.Left;
			}
		}
	}
}
