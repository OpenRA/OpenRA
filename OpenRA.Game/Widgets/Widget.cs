#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
    public abstract class Widget
    {
        // Info defined in YAML
        public string Id = null;
        public string X = "0";
        public string Y = "0";
        public string Width = "0";
        public string Height = "0";
        public string Logic = null;
		public object LogicObject { get; private set; }
        public bool Visible = true;

        protected readonly List<Widget> Children = new List<Widget>();

        // Calculated internally
        public Rectangle Bounds;
        public Widget Parent = null;

        static Stack<Widget> WindowList = new Stack<Widget>();

        // Common Funcs that most widgets will want
        public Func<MouseInput, bool> OnMouseUp = mi => false;

        public Func<bool> IsVisible;

        public Widget() { IsVisible = () => Visible; }

        public static Widget RootWidget
        {
            get { return rootWidget; }
            set { rootWidget = value; }
        }
        static Widget rootWidget = new ContainerWidget();

        public Widget(Widget widget)
        {
            Id = widget.Id;
            X = widget.X;
            Y = widget.Y;
            Width = widget.Width;
            Height = widget.Height;
            Logic = widget.Logic;
            Visible = widget.Visible;

            Bounds = widget.Bounds;
            Parent = widget.Parent;

            OnMouseUp = widget.OnMouseUp;

            IsVisible = widget.IsVisible;

            foreach (var child in widget.Children)
                AddChild(child.Clone());
        }

        public virtual Widget Clone()
        {
            throw new InvalidOperationException("Widget type `{0}` is not cloneable.".F(GetType().Name));
        }

        public virtual int2 RenderOrigin
        {
            get
            {
                var offset = (Parent == null) ? int2.Zero : Parent.ChildOrigin;
                return new int2(Bounds.X, Bounds.Y) + offset;
            }
        }

        public virtual int2 ChildOrigin { get { return RenderOrigin; } }
        public virtual Rectangle RenderBounds { get { return new Rectangle(RenderOrigin.X, RenderOrigin.Y, Bounds.Width, Bounds.Height); } }

        public virtual void Initialize(WidgetArgs args)
        {
            // Parse the YAML equations to find the widget bounds
            var parentBounds = (Parent == null)
                ? new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height)
                : Parent.Bounds;

			var substitutions = args.ContainsKey("substitutions") ?
				new Dictionary<string, int>((Dictionary<string, int>)args["substitutions"]) :
				new Dictionary<string, int>();

            substitutions.Add("WINDOW_RIGHT", Game.viewport.Width);
            substitutions.Add("WINDOW_BOTTOM", Game.viewport.Height);
            substitutions.Add("PARENT_RIGHT", parentBounds.Width);
            substitutions.Add("PARENT_LEFT", parentBounds.Left);
            substitutions.Add("PARENT_TOP", parentBounds.Top);
            substitutions.Add("PARENT_BOTTOM", parentBounds.Height);
            int width = Evaluator.Evaluate(Width, substitutions);
            int height = Evaluator.Evaluate(Height, substitutions);

            substitutions.Add("WIDTH", width);
            substitutions.Add("HEIGHT", height);

            Bounds = new Rectangle(Evaluator.Evaluate(X, substitutions),
                                   Evaluator.Evaluate(Y, substitutions),
                                   width,
                                   height);
        }

        public void PostInit(WidgetArgs args)
        {
            if (Logic == null)
                return;

            args["widget"] = this;

            LogicObject = Game.modData.ObjectCreator.CreateObject<object>(Logic, args);
            var iwd = LogicObject as ILogicWithInit;
            if (iwd != null)
                iwd.Init();

            args.Remove("widget");
        }

        public virtual Rectangle EventBounds { get { return RenderBounds; } }
        public virtual Rectangle GetEventBounds()
        {
            return Children
                .Where(c => c.IsVisible())
                .Select(c => c.GetEventBounds())
                .Aggregate(EventBounds, Rectangle.Union);
        }

        public static Widget SelectedWidget;
        public bool Focused { get { return SelectedWidget == this; } }
        public virtual bool TakeFocus(MouseInput mi)
        {
            if (Focused)
                return true;

            if (SelectedWidget != null && !SelectedWidget.LoseFocus(mi))
                return false;
            SelectedWidget = this;
            return true;
        }

        // Remove focus from this widget; return false if you don't want to give it up
        public virtual bool LoseFocus(MouseInput mi)
        {
            // Some widgets may need to override focus depending on mouse click
            return LoseFocus();
        }

        public virtual bool LoseFocus()
        {
            if (SelectedWidget == this)
                SelectedWidget = null;

            return true;
        }

        public virtual string GetCursor(int2 pos) { return "default"; }
        public string GetCursorOuter(int2 pos)
        {
            // Is the cursor on top of us?
            if (!(IsVisible() && GetEventBounds().Contains(pos)))
                return null;

            // Do any of our children specify a cursor?
            foreach (var child in Children.OfType<Widget>().Reverse())
            {
                var cc = child.GetCursorOuter(pos);
                if (cc != null)
                    return cc;
            }

            return EventBounds.Contains(pos) ? GetCursor(pos) : null;
        }

        public static bool HandleInput(MouseInput mi)
        {
            bool handled = false;
            if (SelectedWidget != null && SelectedWidget.HandleMouseInputOuter(mi))
                handled = true;

            if (!handled && RootWidget.HandleMouseInputOuter(mi))
                handled = true;

            if (mi.Event == MouseInputEvent.Move)
            {
                Viewport.LastMousePos = mi.Location;
                Viewport.TicksSinceLastMove = 0;
            }
            return handled;
        }

        public bool HandleMouseInputOuter(MouseInput mi)
        {
            // Are we able to handle this event?
            if (!(Focused || (IsVisible() && GetEventBounds().Contains(mi.Location.X, mi.Location.Y))))
                return false;

            // Send the event to the deepest children first and bubble up if unhandled
            foreach (var child in Children.OfType<Widget>().Reverse())
                if (child.HandleMouseInputOuter(mi))
                    return true;

            return HandleMouseInput(mi);
        }

        // Hack: Don't eat mouse input that others want
        // TODO: Solve this properly
        public virtual bool HandleMouseInput(MouseInput mi)
        {
            // Apply any special logic added by event handlers; they return true if they caught the input
            if (mi.Event == MouseInputEvent.Up && OnMouseUp(mi)) return true;

            return false;
        }

        public virtual bool HandleKeyPressInner(KeyInput e) { return false; }
        public virtual bool HandleKeyPressOuter(KeyInput e)
        {
            if (!IsVisible())
                return false;

            // Can any of our children handle this?
			foreach (var child in Children.OfType<Widget>().Reverse())
                if (child.HandleKeyPressOuter(e))
                    return true;

            // Do any widgety behavior (enter text etc)
            var handled = HandleKeyPressInner(e);

            return handled;
        }

        public static bool HandleKeyPress(KeyInput e)
        {
            if (SelectedWidget != null)
                return SelectedWidget.HandleKeyPressOuter(e);

            if (RootWidget.HandleKeyPressOuter(e))
                return true;
            return false;
        }

        public abstract void DrawInner();

        public virtual void Draw()
        {
            if (IsVisible())
            {
                DrawInner();
                foreach (var child in Children)
                    child.Draw();
            }
        }

        public virtual void Tick()
        {
            if (IsVisible())
                foreach (var child in Children)
                    child.Tick();
        }

        public virtual void AddChild(Widget child)
        {
            child.Parent = this;
            Children.Add(child);
        }
		
        public virtual void RemoveChild(Widget child)
		{
			Children.Remove(child);
			child.Removed();
		}
        
		public virtual void RemoveChildren()
		{
			while (Children.Count > 0)
				RemoveChild(Children[Children.Count-1]);
		}
		
		public virtual void Removed()
		{
			foreach (var c in Children.OfType<Widget>().Reverse())
				c.Removed();
		}
		
        public Widget GetWidget(string id)
        {
            if (this.Id == id)
                return this;

            foreach (var child in Children)
            {
                var w = child.GetWidget(id);
                if (w != null)
                    return w;
            }
            return null;
        }

        public T GetWidget<T>(string id) where T : Widget
        {
            var widget = GetWidget(id);
            return (widget != null) ? (T)widget : null;
        }

        public static void CloseWindow()
        {
            if (WindowList.Count > 0)
				RootWidget.RemoveChild(WindowList.Pop());
            if (WindowList.Count > 0)
                rootWidget.AddChild(WindowList.Peek());
        }

        public static Widget OpenWindow(string id)
        {
            return OpenWindow(id, new WidgetArgs());
        }

        public static Widget OpenWindow(string id, WidgetArgs args)
        {
            var window = Game.modData.WidgetLoader.LoadWidget(args, rootWidget, id);
            if (WindowList.Count > 0)
                rootWidget.RemoveChild(WindowList.Peek());
            WindowList.Push(window);
            return window;
        }
		
		public static Widget LoadWidget(string id, Widget parent, WidgetArgs args)
        {
            return Game.modData.WidgetLoader.LoadWidget(args, parent, id);
        }

        public static void DoTick()
        {
            RootWidget.Tick();
        }

        public static void DoDraw()
        {
            RootWidget.Draw();
        }
		
		public static void ResetAll()
		{
			RootWidget.RemoveChildren();
			
			while (Widget.WindowList.Count > 0)
				Widget.CloseWindow();
		}
    }

    public class ContainerWidget : Widget
    {
        public ContainerWidget() : base() { }
        public ContainerWidget(ContainerWidget other)
            : base(other) { }

        public override void DrawInner() { }
        public override string GetCursor(int2 pos) { return null; }
        public override Widget Clone() { return new ContainerWidget(this); }
    }
	
	public class WidgetArgs : Dictionary<string, object>
	{
		public WidgetArgs() : base() { }
		public WidgetArgs(Dictionary<string, object> args) : base(args) { }
		public void Add(string key, Action val) { base.Add(key, val); }
	}
	
	// TODO: you should use this anywhere you want to do 
	// something in a logic ctor, but retain debuggability.
    public interface ILogicWithInit
    {
        void Init();
    }
}
