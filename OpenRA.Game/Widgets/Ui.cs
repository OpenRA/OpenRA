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
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Network;

namespace OpenRA.Widgets
{
	public static class Ui
	{
		public const int Timestep = 40;

		public static Widget Root = new ContainerWidget();

		public static TickTime LastTickTime = new(() => Timestep, Game.RunTime);

		static readonly Stack<Widget> WindowList = new();

		public static Widget MouseFocusWidget;
		public static Widget KeyboardFocusWidget;
		public static Widget MouseOverWidget;

		static readonly Mediator Mediator = new();

		public static void CloseWindow()
		{
			if (WindowList.Count > 0)
			{
				var hidden = WindowList.Pop();
				Root.RemoveChild(hidden);
				if (hidden.LogicObjects != null)
					foreach (var l in hidden.LogicObjects)
						l.BecameHidden();
			}

			if (WindowList.Count > 0)
			{
				var restore = WindowList.Peek();
				Root.AddChild(restore);

				if (restore.LogicObjects != null)
					foreach (var l in restore.LogicObjects)
						l.BecameVisible();
			}
		}

		public static Widget OpenWindow(string id)
		{
			return OpenWindow(id, new WidgetArgs());
		}

		public static Widget OpenWindow(string id, WidgetArgs args)
		{
			var window = Game.ModData.WidgetLoader.LoadWidget(args, Root, id);
			if (WindowList.Count > 0)
				Root.HideChild(WindowList.Peek());
			WindowList.Push(window);
			return window;
		}

		public static Widget CurrentWindow()
		{
			return WindowList.Count > 0 ? WindowList.Peek() : null;
		}

		public static T LoadWidget<T>(string id, Widget parent, WidgetArgs args) where T : Widget
		{
			if (LoadWidget(id, parent, args) is T widget)
				return widget;

			throw new InvalidOperationException($"Widget {id} is not of type {typeof(T).Name}");
		}

		public static Widget LoadWidget(string id, Widget parent, WidgetArgs args)
		{
			return Game.ModData.WidgetLoader.LoadWidget(args, parent, id);
		}

		public static void Tick() { Root.TickOuter(); }

		public static void PrepareRenderables() { Root.PrepareRenderablesOuter(); }

		public static void Draw() { Root.DrawOuter(); }

		public static bool HandleInput(MouseInput mi)
		{
			var wasMouseOver = MouseOverWidget;

			if (mi.Event == MouseInputEvent.Move)
				MouseOverWidget = null;

			var handled = false;
			if (MouseFocusWidget != null && MouseFocusWidget.HandleMouseInputOuter(mi))
				handled = true;

			if (!handled && Root.HandleMouseInputOuter(mi))
				handled = true;

			if (mi.Event == MouseInputEvent.Move)
			{
				Viewport.LastMousePos = mi.Location;
				Viewport.LastMoveRunTime = Game.RunTime;
			}

			if (wasMouseOver != MouseOverWidget)
			{
				wasMouseOver?.MouseExited();

				MouseOverWidget?.MouseEntered();
			}

			return handled;
		}

		/// <summary>Possibly handle keyboard input (if this widget has keyboard focus).</summary>
		/// <returns><c>true</c>, if keyboard input was handled, <c>false</c> if the input should bubble to the parent widget.</returns>
		/// <param name="e">Key input data.</param>
		public static bool HandleKeyPress(KeyInput e)
		{
			if (KeyboardFocusWidget != null)
				return KeyboardFocusWidget.HandleKeyPressOuter(e);

			return Root.HandleKeyPressOuter(e);
		}

		public static bool HandleTextInput(string text)
		{
			if (KeyboardFocusWidget != null)
				return KeyboardFocusWidget.HandleTextInputOuter(text);

			return Root.HandleTextInputOuter(text);
		}

		public static void ResetAll()
		{
			Root.RemoveChildren();

			while (WindowList.Count > 0)
				CloseWindow();
		}

		public static void ResetTooltips()
		{
			// Issue a no-op mouse move to force any tooltips to be recalculated
			HandleInput(new MouseInput(MouseInputEvent.Move, MouseButton.None,
				Viewport.LastMousePos, int2.Zero, Modifiers.None, 0));
		}

		public static void Subscribe<T>(T instance)
		{
			Mediator.Subscribe(instance);
		}

		public static void Unsubscribe<T>(T instance)
		{
			Mediator.Unsubscribe(instance);
		}

		public static void Send<T>(T notification) => Mediator.Send(notification);
	}
}
