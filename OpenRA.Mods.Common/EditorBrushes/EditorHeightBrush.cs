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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorHeightBrush(
		HeightTool heightTool,
		World world,
		WorldRenderer worldRenderer,
		EditorActionManager editorActionManager)
		: IKeyHandlingEditorBrush
	{
		ChangeHeightEditorAction action;

		CPos? cell;
		int size = heightTool.Info.MinSize;
		HeightTool.Settings settings;

		public CPos? Cell
		{
			get => cell;
			set
			{
				cell = value;
				Update();
			}
		}

		public int Size
		{
			get => size;
			set
			{
				size = value;
				Update();
			}
		}

		public HeightTool.Settings Settings
		{
			get => settings;
			private set
			{
				settings = value;
				Update();
			}
		}

		void Update()
		{
			heightTool.Update(cell, size, settings);
		}

		public bool HandleMouseInput(MouseInput mouseInput)
		{
			var oldCell = cell;
			Cell = worldRenderer.Viewport.ViewToWorld(mouseInput.Location);

			if (mouseInput.Button == MouseButton.Left)
			{
				if (mouseInput.Event == MouseInputEvent.Down)
				{
					editorActionManager.Add(action = new ChangeHeightEditorAction(world));
					action.Add(heightTool.Changes);
					return true;
				}

				if (mouseInput.Event == MouseInputEvent.Up)
				{
					action = null;
					return true;
				}
			}

			if (mouseInput.Event == MouseInputEvent.Move && action != null && oldCell != cell)
				action.Add(heightTool.Changes);

			if (mouseInput.Event == MouseInputEvent.Scroll && mouseInput.Modifiers.HasFlag(Modifiers.Alt))
			{
				// TODO: it seems scrolling cannot be captured by the editor brush, so it doesn't work here.
				Size = Math.Clamp(Size + (mouseInput.Delta.Y > 0 ? 1 : -1), heightTool.Info.MinSize, heightTool.Info.MaxSize);
				return true;
			}

			return false;
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { }
		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { yield break; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr) { yield break; }

		public bool HandleKeyPress(KeyInput keyInput)
		{
			// TODO we might want to set these in the controls?
			switch (keyInput.Key)
			{
				case Keycode.LCTRL or Keycode.RCTRL:
					ToggleSetting(HeightTool.Settings.Lower, keyInput.Event == KeyInputEvent.Down);
					break;

				case Keycode.TAB:
					if (keyInput.Event == KeyInputEvent.Down)
						ToggleSetting(HeightTool.Settings.Circle);
					break;
			}

			return false;
		}

		public void ToggleSetting(HeightTool.Settings setting, bool? active = null)
		{
			switch (active)
			{
				case true:
				case null when !Settings.HasFlag(setting):
					Settings |= setting;
					break;

				case false:
				case null when Settings.HasFlag(setting):
					Settings &= ~setting;
					break;
			}
		}

		public void Tick() { }

		public void Dispose() { }
	}

	sealed class ChangeHeightEditorAction(World world) : IEditorAction
	{
		readonly Dictionary<CPos, HeightTool.Change> changes = [];

		public void Execute()
		{
		}

		public void Do()
		{
			Do(changes);
		}

		void Do(IReadOnlyDictionary<CPos, HeightTool.Change> newChanges)
		{
			foreach (var (cell, change) in newChanges)
			{
				if (change.Old != change.New)
					world.Map.Height[cell] = change.New;
			}
		}

		public void Undo()
		{
			foreach (var (cell, change) in changes)
			{
				if (change.Old != change.New)
					world.Map.Height[cell] = change.Old;
			}
		}

		public string Text { get; }

		public void Add(IReadOnlyDictionary<CPos, HeightTool.Change> newChanges)
		{
			foreach (var (cell, newChange) in newChanges)
			{
				if (changes.TryGetValue(cell, out var oldChange))
					oldChange.New = newChange.New;
				else
					changes.Add(cell, newChange);
			}

			Do(newChanges);
		}
	}
}
