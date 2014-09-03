#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using System.Text;

namespace OpenRA.Widgets
{
	public class TableRowWidget : SpacingWidget
	{
		public TableRowWidget() : base()
		{
			Layout = new GridLayout(this, true);
		}

		public TableRowWidget(TableRowWidget widget) : base(widget)
		{
			Layout = new GridLayout(this, true);
		}

		public void MatchHeader(TableHeaderWidget header)
		{
			Bounds.Height = header.Bounds.Height;
			Bounds.Width = header.Bounds.Width;
			ItemSpacing = header.ItemSpacing;
			ItemSpacingH = header.ItemSpacingH;

			for (int i = 0; i < Children.Count; i++)
			{
				var w = Children[i];
				w.Bounds.Width = header.GetWidth(i);

				if (w.Bounds.Height == 0)
				{
					w.Bounds.Height = Bounds.Height;
				}
			}

			Layout.AdjustChildren();
		}

		public void SetTextColor(Color c)
		{
			foreach (var w in Children)
			{
				if (w is LabelWidget)
				{
					var label = (LabelWidget)w;
					label.GetColor = () => c;
				}
			}
		}
	}

	public class TableHeaderWidget : SpacingWidget
	{
		public TableHeaderWidget()
			: base()
		{
			Layout = new GridLayout(this, true);
		}

		public int GetWidth(int col)
		{
			return Children[col].Bounds.Width;
		}

		public int GetWidth()
		{
			// determines and sets the sum of the children's widths and spacing as header row width
			// (gridlayout creates new rows if your width is too small to contain all the children, and that's great, but in this particular case we don't want that)
			this.Bounds.Width = this.ItemSpacing + Children.Sum(w => w.Bounds.Width) + (Children.Count * (this.ItemSpacing + this.ItemSpacingH));

			return this.Bounds.Width;
		}

		public void AdjustHeights()
		{
			foreach (var w in Children)
			{
				w.Bounds.Height = this.Bounds.Height;
			}
		}
	}

	public class TableWidget : SpacingWidget
	{
		protected TableHeaderWidget header;
		protected TableRowWidget rowTemplate;
		protected List<TableRowWidget> rows;
		public int ColumnSpacing = 0;
		public int RowSpacing = 0;
		public string Background = "button";
		public string Title = "";

		public TableWidget() : base()
		{
			Layout = new ListLayout(this);
		}

		public override void Draw()
		{
			WidgetUtils.DrawPanel(Background, Bounds);

			base.Draw();
		}

		public void InitTable()
		{
			foreach (var w in Children)
			{
				if (w is TableHeaderWidget)
				{
					header = (TableHeaderWidget)w;
				} else if (w is TableRowWidget)
				{
					rowTemplate = (TableRowWidget)w;
				}
			}

			Children.Clear();

			ItemSpacing = RowSpacing;

			header.ItemSpacing = RowSpacing;
			header.ItemSpacingH = ColumnSpacing;
			header.Bounds.Height = this.Bounds.Height;
			Bounds.Width = header.GetWidth();

			header.Layout.AdjustChildren();
			header.AdjustHeights();

			AddChild(header);

			Bounds.Height = this.ContentHeight + (int)Math.Round(header.Bounds.Height * 0.3);
		}

		public TableRowWidget NewRow()
		{
			var row = new TableRowWidget(rowTemplate);
			row.MatchHeader(header);

			this.AddChild(row);
			Bounds.Height = this.ContentHeight + (int)Math.Round(header.Bounds.Height * 0.3);
			
			return row;
		}
	}
}
