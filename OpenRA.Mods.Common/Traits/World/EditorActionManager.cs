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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public class EditorActionManagerInfo : TraitInfo<EditorActionManager> { }

	public class EditorActionManager : IWorldLoaded
	{
		readonly Stack<EditorActionContainer> undoStack = new Stack<EditorActionContainer>();
		readonly Stack<EditorActionContainer> redoStack = new Stack<EditorActionContainer>();

		public event Action<EditorActionContainer> ItemAdded;
		public event Action<EditorActionContainer> ItemRemoved;
		public event Action OnChange;

		int nextId;

		public bool Modified;
		public bool SaveFailed;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			Add(new OpenMapAction());
			Modified = false;
		}

		public void Add(IEditorAction editorAction)
		{
			Modified = true;
			editorAction.Execute();

			if (undoStack.Count > 0)
				undoStack.Peek().Status = EditorActionStatus.History;

			var actionContainer = new EditorActionContainer(nextId++, editorAction);

			ClearRedo();
			undoStack.Push(actionContainer);

			ItemAdded?.Invoke(actionContainer);
		}

		public void Undo()
		{
			if (!HasUndos())
				return;

			Modified = true;

			var editorAction = undoStack.Pop();
			undoStack.Peek().Status = EditorActionStatus.Active;
			editorAction.Action.Undo();
			editorAction.Status = EditorActionStatus.Future;
			redoStack.Push(editorAction);

			OnChange?.Invoke();
		}

		void ClearRedo()
		{
			while (HasRedos())
			{
				var item = redoStack.Pop();

				ItemRemoved?.Invoke(item);
			}
		}

		public void Redo()
		{
			if (!HasRedos())
				return;

			Modified = true;

			var editorAction = redoStack.Pop();

			editorAction.Status = EditorActionStatus.Active;
			editorAction.Action.Do();
			undoStack.Peek().Status = EditorActionStatus.History;
			undoStack.Push(editorAction);

			OnChange?.Invoke();
		}

		public bool HasUndos()
		{
			// Preserve the initial OpenMapAction.
			return undoStack.Count > 1;
		}

		public bool HasRedos()
		{
			return redoStack.Count > 0;
		}

		public void Rewind(int id)
		{
			while (undoStack.Peek().Id != id)
				Undo();
		}

		public void Forward(int id)
		{
			while (undoStack.Peek().Id != id)
				Redo();
		}

		public bool HasUnsavedItems()
		{
			// Modified and last action isn't the OpenMapAction (+ no redos)
			return Modified && !(undoStack.Peek().Action is OpenMapAction && !HasRedos());
		}
	}

	public enum EditorActionStatus
	{
		History,
		Active,
		Future,
	}

	public interface IEditorAction
	{
		void Execute();
		void Do();
		void Undo();

		string Text { get; }
	}

	class OpenMapAction : IEditorAction
	{
		public OpenMapAction()
		{
			Text = "Opened";
		}

		public void Execute()
		{
		}

		public void Do()
		{
		}

		public void Undo()
		{
		}

		public string Text { get; }

		public EditorActionStatus Status { get; set; }
	}

	public class EditorActionContainer
	{
		public int Id { get; }
		public IEditorAction Action { get; }
		public EditorActionStatus Status { get; set; }

		public EditorActionContainer(int id, IEditorAction action)
		{
			Id = id;
			Action = action;
			Status = EditorActionStatus.Active;
		}
	}
}
