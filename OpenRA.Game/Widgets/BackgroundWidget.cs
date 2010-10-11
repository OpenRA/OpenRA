#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	class BackgroundWidget : Widget
	{
		public readonly string Background = "dialog";
		
		public override void DrawInner( WorldRenderer wr )
		{
			WidgetUtils.DrawPanel(Background, RenderBounds);
		}
		
		public BackgroundWidget() : base() { }

		protected BackgroundWidget(BackgroundWidget other)
			: base(other)
		{
			Background = other.Background;
		}

		public override Widget Clone() { return new BackgroundWidget(this); }
	}
}