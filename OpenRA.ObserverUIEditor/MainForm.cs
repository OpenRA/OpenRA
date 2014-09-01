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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenRA.FileFormats;
using System.Collections;
using OpenRA.Widgets;
using OpenRA.Graphics;
using OpenRA.FileSystem;

namespace OpenRA.ObserverUIEditor {
	public partial class MainForm : Form
	{
		private List<MiniYamlNode> currentYaml;
		private List<DropDownOption> dropdownOptions;
		private MiniYamlNode root;
		private string currentMod;

		private string currentYamlPath;

		private MiniYamlNode currentYamlTable;
		private MiniYamlNode currentYamlBarstats;
		private DropDownOption currentDropdownOption;

		public MainForm()
		{
			InitializeComponent();
		}

		private void LoadMod(string name)
		{
			LoadFromFile(".\\mods\\" + name + "\\chrome\\ingame-observerstats.yaml");
			currentMod = name;
			this.Text = "Observer UI editor - " + name;

			ChromeProvider.InitializeOutOfGame(".\\mods\\" + name + "\\chrome.yaml");
			GlobalFileSystem.UnmountAll();
			GlobalFileSystem.Mount(".\\mods\\" + name + "\\uibits");

			PopulateStuff();
			InitFromYaml();
	
			currentYamlTable = null;
			currentYamlBarstats = null;
			currentDropdownOption = null;

			cbConfigureView_SelectedIndexChanged(cbConfigureView, null);
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			Log.AddChannel("ObserverUIEditor", "ObserverUIEditor.txt");

			LoadMod("cnc");
		}

		private void DoSave()
		{
			MiniYamlExts.WriteToFile(currentYaml, currentYamlPath, false);
		}

		private void DoRestore()
		{
			LoadFromFile(currentYamlPath);
			InitFromYaml();
			currentYamlTable = null;
			currentYamlBarstats = null;
			currentDropdownOption = null;

			cbConfigureView_SelectedIndexChanged(cbConfigureView, null);
		}

		private void AskToSave()
		{
			DialogResult yesnocancel = MessageBox.Show("Do you want to save first before closing the editor?", "Save first..", MessageBoxButtons.YesNoCancel);
			if (yesnocancel == DialogResult.Yes) {
				DoSave();
			}
			else if (yesnocancel == DialogResult.No)
			{
				// do nothing
			}
			else
			{
				throw new OperationCanceledException("Canceled");
			}
		}

		private void InitFromYaml()
		{
			cbDefaultView.SelectedItem = dropdownOptions.Find(o => o.Caption == NodeUtils.GetTextValue(root, "DefaultSelectedOption"));

			string s;
			
			s = NodeUtils.GetTextValue(root, "BarsTeamEmphasizeColor1");
			btnOddColor.FillColor = (Color)FieldLoader.GetValue("BarsTeamEmphasizeColor1", typeof(Color), s);

			s = NodeUtils.GetTextValue(root, "BarsTeamEmphasizeColor2");
			btnEvenColor.FillColor = (Color)FieldLoader.GetValue("BarsTeamEmphasizeColor2", typeof(Color), s);

			edThickness.Value = NodeUtils.GetIntValue(root, "BarsTeamEmphasizeThickness");
			edSpacing.Value = NodeUtils.GetIntValue(root, "BarsTeamsSpacing");
		}

		private MiniYamlNode GetKids(MiniYamlNode node)
		{
			return node.Value.Nodes.Find(n => n.Key == "Children");
		}

		private void InitBarstatsFromYaml(MiniYamlNode barstatsnode)
		{
			grpTableEdit.Visible = false;

			if (barstatsnode == null)
			{
				return;
			}

			currentYamlBarstats = barstatsnode;
			currentDropdownOption = dropdownOptions.Find(o => o.YamlTitle == currentYamlBarstats.Key);

			var kids = GetKids(currentYamlBarstats);
			var headtemplate = NodeUtils.GetValueNode(kids, "Spacing@OBSSTATSBOTTOM_TEMPLATE");
			kids = GetKids(headtemplate);

			var container = NodeUtils.GetValueNode(kids, "Background@OBSSTATSBOTTOM_TEMPLATE_CONTAINER");
			var columns = GetKids(container);

			lstColumns.Items.Clear();

			edTitle.Text = "";
			edTitle.Enabled = false;

			edX.Value = NodeUtils.GetIntValue(currentYamlBarstats, "X");
			edY.Value = NodeUtils.GetIntValue(currentYamlBarstats, "Y");
			edRowHeight.Value = NodeUtils.GetIntValue(headtemplate, "Height");
			
			edColumnSpacing.Value = 0;
			edColumnSpacing.Enabled = false;

			edRowSpacing.Value = NodeUtils.GetIntValue(currentYamlBarstats, "ItemSpacing");

			var background = NodeUtils.GetTextValue(container, "Background");
			cbBackground.SelectedIndex = cbBackground.Items.IndexOf(background);

			for (int i = 0; i < columns.Value.Nodes.Count; i++)
			{
				var column = columns.Value.Nodes[i];

				var tcol = new BarstatsColumn();
				tcol.RowNode = column;

				lstColumns.Items.Add(tcol);
			}

			grpTableEdit.Visible = true;
		}

		private void InitTableFromYaml(MiniYamlNode tablenode)
		{
			grpTableEdit.Visible = false;

			if (tablenode == null)
			{
				return;
			}

			var kids = GetKids(tablenode);

			lstColumns.Items.Clear();

			currentYamlTable = tablenode;
			currentDropdownOption = dropdownOptions.Find(o => o.YamlTitle == tablenode.Key);

			edTitle.Text = NodeUtils.GetTextValue(tablenode, "Title");
			edX.Value = NodeUtils.GetIntValue(tablenode, "X");
			edY.Value = NodeUtils.GetIntValue(tablenode, "Y");
			edRowHeight.Value = NodeUtils.GetIntValue(tablenode, "Height");
			edColumnSpacing.Value = NodeUtils.GetIntValue(tablenode, "ColumnSpacing");
			edRowSpacing.Value = NodeUtils.GetIntValue(tablenode, "RowSpacing");

			var background = NodeUtils.GetTextValue(tablenode, "Background");
			if (background == "")
			{
				background = "button";
			}

			cbBackground.SelectedIndex = cbBackground.Items.IndexOf(background);

			var headers = kids.Value.Nodes.Find(n => n.Key == "TableHeader");
			var headercolumns = GetKids(headers);

			var rowtemplate = kids.Value.Nodes.Find(n => n.Key == "TableRow");
			var rowcolumns = GetKids(rowtemplate);

			for (int i = 0; i < headercolumns.Value.Nodes.Count; i++)
			{
				var column = headercolumns.Value.Nodes[i];
				var rowvalue = rowcolumns.Value.Nodes[i];

				var tcol = new TableColumn();
				tcol.HeaderNode = column;
				tcol.RowNode = rowvalue;

				lstColumns.Items.Add(tcol);
			}

			grpTableEdit.Visible = true;

			/*
			X:25
			Y:70
			Height:20
			ColumnSpacing:5
			RowSpacing:2

			 Children:
			  TableHeader:
					Children:
						Label:
							Width:80
							Height:PARENT_BOTTOM
							Align:Center
							Text:Cash
				TableRow:
					Children:
						Image@FLAG:
							Y:5
							ImageName:random
							ImageCollection:flags
						Label@CASH:
							BackgroundColor:255,0,0,0
							Align:Center
			 * */
		}

		private void PopulateStuff()
		{
			dropdownOptions = new List<DropDownOption>();

			dropdownOptions.Clear();
			dropdownOptions.Add(new DropDownOption("", ""));
			dropdownOptions.Add(new DropDownOption("Control", "Table@CONTROLTABLE"));
			dropdownOptions.Add(new DropDownOption("Combat", "Table@COMBATTABLE"));
			dropdownOptions.Add(new DropDownOption("Economy", "Table@ECOTABLE"));
			dropdownOptions.Add(new DropDownOption("Production", "Spacing@PRODUCTION_PANEL"));
			dropdownOptions.Add(new DropDownOption("Support Power", "Spacing@SUPPORTPWR_PANEL"));
			dropdownOptions.Add(new DropDownOption("Earnings Graph", "Background@EARNED_THIS_MIN_GRAPH_PANEL"));
			dropdownOptions.Add(new DropDownOption("Summary", "Spacing@OBSERVER_STATS_BOTTOM_PANEL"));

			cbConfigureView.Items.Clear();
			cbConfigureView.Items.AddRange(dropdownOptions.ToArray<DropDownOption>());

			cbDefaultView.Items.Clear();
			cbDefaultView.Items.AddRange(dropdownOptions.ToArray<DropDownOption>());

			cbBackground.Items.Clear();
			cbBackground.Items.AddRange(GetBackgroundOptions().ToArray<string>());
		}

		private void LoadFromFile(string path)
		{
			currentYaml = MiniYaml.FromFile(path);

			root = currentYaml.First();
			if (root.Key != "ObserverStats@OBSERVER_STATS")
			{
				throw new Exception("Not an observerstats yaml file");
			}

			currentYamlPath = path;
		}

		private void cbConfigureView_SelectedIndexChanged(object sender, EventArgs e)
		{
			currentYamlTable = null;
			currentYamlBarstats = null;

			DropDownOption selected = (DropDownOption)cbConfigureView.SelectedItem;
			if (selected != null)
			{
				var kids = GetKids(root);

				var node = kids.Value.Nodes.Find(n => n.Key == selected.YamlTitle);
				if (node != null)
				{
					if (selected.IsTable)
					{
						InitTableFromYaml(node);
					}
					else if (selected.IsStatsbar)
					{
						InitBarstatsFromYaml(node);
					}
					else
					{
						InitTableFromYaml(null);
					}
				}
				else
				{
					InitTableFromYaml(null);
				}
			}
			else
			{
				InitTableFromYaml(null);
			}
		}

		private void cbDefaultView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cbDefaultView.SelectedItem != null)
			{
				NodeUtils.SetTextValue(root, "DefaultSelectedOption", ((DropDownOption)cbDefaultView.SelectedItem).Caption);
			}
			else
			{
				NodeUtils.SetTextValue(root, "DefaultSelectedOption", "");
			}
		}

		private void edX_ValueChanged(object sender, EventArgs e)
		{
			if (currentYamlTable != null)
			{
				NodeUtils.SetTextValue(currentYamlTable, "X", "" + edX.Value);
			}
			else if (currentYamlBarstats != null)
			{
				NodeUtils.SetTextValue(currentYamlBarstats, "X", "" + edX.Value);
			}
		}

		private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			DoSave();
		}

		private void lstColumns_DoubleClick(object sender, EventArgs e)
		{
			btnEditColumn_Click(sender, e);
		}

		private void btnEditColumn_Click(object sender, EventArgs e)
		{
			BaseColumn col = (BaseColumn)lstColumns.SelectedItem;
			if (col != null)
			{
				if (currentYamlTable != null)
				{
					var frm = new ColumnEdit();
					frm.LoadFromNode(((TableColumn)col).HeaderNode, col.RowNode);
					if (frm.ShowDialog() == DialogResult.OK)
					{
						frm.SaveToNodes();

						InitTableFromYaml(currentYamlTable);

						reselectCol(col);
					}
				}
				else if (currentYamlBarstats != null)
				{
					var frm = new frmFieldEdit();
					frm.InitYamlImageOptions(currentMod);
					frm.LoadFromNode(col.RowNode);
					if (frm.ShowDialog() == DialogResult.OK)
					{
						frm.SaveToNodes();

						InitBarstatsFromYaml(currentYamlBarstats);

						reselectCol(col);
					}
				}
			}
		}

		private void btnAddColumn_Click(object sender, EventArgs e)
		{
			AddColumn();
		}

		private void MoveColumn(TableColumn col,  int iBy)
		{
			var kids = GetKids(currentYamlTable);
			var headers = kids.Value.Nodes.Find(n => n.Key == "TableHeader");
			var headercolumns = GetKids(headers);

			var rowtemplate = kids.Value.Nodes.Find(n => n.Key == "TableRow");
			var rowcolumns = GetKids(rowtemplate);

			int iFrom = headercolumns.Value.Nodes.IndexOf(col.HeaderNode);
			if ((iFrom + iBy >= 0) && (iFrom + iBy < headercolumns.Value.Nodes.Count))
			{
				headercolumns.Value.Nodes.RemoveAt(iFrom);
				headercolumns.Value.Nodes.Insert(iFrom + iBy, col.HeaderNode);

				rowcolumns.Value.Nodes.RemoveAt(iFrom);
				rowcolumns.Value.Nodes.Insert(iFrom + iBy, col.RowNode);
			}
		}

		private void MoveColumn(BarstatsColumn col, int iBy)
		{
			var kids = GetKids(currentYamlBarstats);
			var headtemplate = NodeUtils.GetValueNode(kids, "Spacing@OBSSTATSBOTTOM_TEMPLATE");
			kids = GetKids(headtemplate);

			var container = NodeUtils.GetValueNode(kids, "Background@OBSSTATSBOTTOM_TEMPLATE_CONTAINER");
			var columns = GetKids(container);

			int iFrom = columns.Value.Nodes.IndexOf(col.RowNode);
			if ((iFrom + iBy >= 0) && (iFrom + iBy < columns.Value.Nodes.Count))
			{
				columns.Value.Nodes.RemoveAt(iFrom);
				columns.Value.Nodes.Insert(iFrom + iBy, col.RowNode);
			}
		}

		private void DeleteColumn(TableColumn col)
		{
			var kids = GetKids(currentYamlTable);
			var headers = kids.Value.Nodes.Find(n => n.Key == "TableHeader");
			var headercolumns = GetKids(headers);

			var rowtemplate = kids.Value.Nodes.Find(n => n.Key == "TableRow");
			var rowcolumns = GetKids(rowtemplate);

			headercolumns.Value.Nodes.Remove(col.HeaderNode);
			rowcolumns.Value.Nodes.Remove(col.RowNode);
		}

		private void DeleteColumn(BarstatsColumn col)
		{
			var kids = GetKids(currentYamlBarstats);
			var headtemplate = NodeUtils.GetValueNode(kids, "Spacing@OBSSTATSBOTTOM_TEMPLATE");
			kids = GetKids(headtemplate);

			var container = NodeUtils.GetValueNode(kids, "Background@OBSSTATSBOTTOM_TEMPLATE_CONTAINER");
			var columns = GetKids(container);

			columns.Value.Nodes.Remove(col.RowNode);
		}

		private BaseColumn AddColumn()
		{
			if (currentYamlTable != null)
			{
				var kids = GetKids(currentYamlTable);
				var headers = kids.Value.Nodes.Find(n => n.Key == "TableHeader");
				var headercolumns = GetKids(headers);

				var rowtemplate = kids.Value.Nodes.Find(n => n.Key == "TableRow");
				var rowcolumns = GetKids(rowtemplate);

				TableColumn col = new TableColumn();
				col.HeaderNode = new MiniYamlNode("Label", "");
				NodeUtils.SetTextValue(col.HeaderNode, "Width", "80");
				NodeUtils.SetTextValue(col.HeaderNode, "Align", "Center");
				col.RowNode = new MiniYamlNode("Label", "");
				NodeUtils.SetTextValue(col.RowNode, "BackgroundColor", "255,0,0,0");
				NodeUtils.SetTextValue(col.RowNode, "Align", "Center");

				var frm = new ColumnEdit();
				frm.LoadFromNode(col.HeaderNode, col.RowNode);
				if (frm.ShowDialog() == DialogResult.OK)
				{
					frm.SaveToNodes();

					headercolumns.Value.Nodes.Add(col.HeaderNode);
					rowcolumns.Value.Nodes.Add(col.RowNode);

					lstColumns.Items.Add(col);
					lstColumns.Refresh();
					InitTableFromYaml(currentYamlTable);

					return col;
				}
			}
			else if (currentYamlBarstats != null)
			{
				var kids = GetKids(currentYamlBarstats);
				var headtemplate = NodeUtils.GetValueNode(kids, "Spacing@OBSSTATSBOTTOM_TEMPLATE");
				kids = GetKids(headtemplate);

				var container = NodeUtils.GetValueNode(kids, "Background@OBSSTATSBOTTOM_TEMPLATE_CONTAINER");
				var columns = GetKids(container);

				BarstatsColumn col = new BarstatsColumn();
				col.RowNode = new MiniYamlNode("Label", "");
				NodeUtils.SetTextValue(col.RowNode, "X", "" + GuessCurrentBarstatsWidth());
				NodeUtils.SetTextValue(col.RowNode, "Height", "0");
				NodeUtils.SetTextValue(col.RowNode, "Align", "Left");

				var frm = new frmFieldEdit();
				frm.InitYamlImageOptions(currentMod);
				frm.LoadFromNode(col.RowNode);
				if (frm.ShowDialog() == DialogResult.OK)
				{
					frm.SaveToNodes();

					columns.Value.Nodes.Add(col.RowNode);
					InitBarstatsFromYaml(currentYamlBarstats);

					return col;
				}
			}

			return null;
		}

		private int GuessCurrentBarstatsWidth()
		{
			int x = 0;

			foreach (BaseColumn col in lstColumns.Items)
			{
				x = Math.Max(x, NodeUtils.GetIntValue(col.RowNode, "X") + NodeUtils.GetIntValue(col.RowNode, "Width"));
			}

			return x;
		}

		private void reselectCol(BaseColumn col)
		{
			lstColumns.SelectedItem = col;

			if (lstColumns.SelectedItem != col)
			{
				var q = lstColumns.Items.Cast<BaseColumn>();
				var obj = from BaseColumn c in q
						  where c.RowNode == col.RowNode
						  select c;
				var item = obj.First<BaseColumn>();
				lstColumns.SelectedItem = item;
			}
		}

		private void btnUp_Click(object sender, EventArgs e)
		{
			BaseColumn col = (BaseColumn)lstColumns.SelectedItem;

			if (currentYamlTable != null)
			{
				MoveColumn((TableColumn)col, -1);
				InitTableFromYaml(currentYamlTable);
			}
			else if (currentYamlBarstats != null)
			{
				MoveColumn((BarstatsColumn)col, -1);
				InitBarstatsFromYaml(currentYamlBarstats);
			}

			reselectCol(col);
		}

		private void btnDown_Click(object sender, EventArgs e)
		{
			BaseColumn col = (BaseColumn)lstColumns.SelectedItem;

			if (currentYamlTable != null)
			{
				MoveColumn((TableColumn)col, 1);
				InitTableFromYaml(currentYamlTable);
			}
			else if (currentYamlBarstats != null)
			{
				MoveColumn((BarstatsColumn)col, 1);
				InitBarstatsFromYaml(currentYamlBarstats);
			}

			reselectCol(col);
		}

		private void btnDeleteColumn_Click(object sender, EventArgs e)
		{
			BaseColumn col = (BaseColumn)lstColumns.SelectedItem;
			if (currentYamlTable != null)
			{
				DeleteColumn((TableColumn)col);
				InitTableFromYaml(currentYamlTable);
			}
			else if (currentYamlBarstats != null)
			{
				DeleteColumn((BarstatsColumn)col);
				InitBarstatsFromYaml(currentYamlBarstats);
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DoRestore();
		}

		private void btnOddColor_Click(object sender, EventArgs e)
		{
			dlgColor.Color = btnOddColor.FillColor;
			if (dlgColor.ShowDialog() == DialogResult.OK)
			{
				btnOddColor.FillColor = dlgColor.Color;
			}
		}

		private void btnEvenColor_Click(object sender, EventArgs e)
		{
			dlgColor.Color = btnEvenColor.FillColor;
			if (dlgColor.ShowDialog() == DialogResult.OK)
			{
				btnEvenColor.FillColor = dlgColor.Color;
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			AskToSave();
		}

		private void edTitle_TextChanged(object sender, EventArgs e)
		{
			if (currentYamlTable != null)
			{
				NodeUtils.SetTextValue(currentYamlTable, "Title", edTitle.Text);
			}
		}

		private void edY_ValueChanged(object sender, EventArgs e)
		{
			if (currentYamlTable != null)
			{
				NodeUtils.SetTextValue(currentYamlTable, "Y", "" + edY.Value);
			}
			else if (currentYamlBarstats != null)
			{
				NodeUtils.SetTextValue(currentYamlBarstats, "Y", "" + edY.Value);
			}

			picExample.Refresh();
		}

		private void edRowHeight_ValueChanged(object sender, EventArgs e)
		{
			if (currentYamlTable != null)
			{
				NodeUtils.SetTextValue(currentYamlTable, "Height", "" + edRowHeight.Value);
			}
			else if (currentYamlBarstats != null)
			{
				var kids = GetKids(currentYamlBarstats);
				var headtemplate = NodeUtils.GetValueNode(kids, "Spacing@OBSSTATSBOTTOM_TEMPLATE");
				NodeUtils.SetTextValue(headtemplate, "Height", "" + edRowHeight.Value);
			}

			picExample.Refresh();
		}

		private void edColumnSpacing_ValueChanged(object sender, EventArgs e)
		{
			if (currentYamlTable != null)
			{
				NodeUtils.SetTextValue(currentYamlTable, "ColumnSpacing", "" + edColumnSpacing.Value);
			}
		}

		private void edRowSpacing_ValueChanged(object sender, EventArgs e)
		{
			if (currentYamlTable != null)
			{
				NodeUtils.SetTextValue(currentYamlTable, "RowSpacing", "" + edRowSpacing.Value);
			}
			else if (currentYamlBarstats != null)
			{
				NodeUtils.SetTextValue(currentYamlBarstats, "ItemSpacing", "" + edRowSpacing.Value);
			}

			picExample.Refresh();
		}

		private void edThickness_ValueChanged(object sender, EventArgs e)
		{
			NodeUtils.SetTextValue(root, "BarsTeamEmphasizeThickness", "" + edThickness.Value);
			picExample.Refresh();
		}

		private void edSpacing_ValueChanged(object sender, EventArgs e)
		{
			NodeUtils.SetTextValue(root, "BarsTeamsSpacing", "" + edSpacing.Value);
			picExample.Refresh();
		}

		private void picExample_Paint(object sender, PaintEventArgs e)
		{
			var darkgrassbrush = new SolidBrush(Color.DarkGreen);
			e.Graphics.FillRectangle(darkgrassbrush, 0, 0, picExample.Width, picExample.Height);

			if ((currentYamlTable != null) || (currentYamlBarstats != null))
			{
				var redpen = new Pen(Color.DarkRed, 1f);
				var deffont = new Font("Regular", 10);
				var blackbrush = new SolidBrush(Color.Black);
				var greenbrush = new SolidBrush(Color.Green);
				var yellowbrush = new SolidBrush(Color.Yellow);
				var whitebrush = new SolidBrush(Color.White);
				var darkredbrush = new SolidBrush(Color.FromArgb(200, Color.DarkRed.R, Color.DarkRed.G, Color.DarkRed.B));
				var darkbrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));

				var textfontbrush = yellowbrush;

				int colspacing = 0;
				int rowspacing = 0;

				MiniYamlNode headercolumns = null;
				MiniYamlNode rowcolumns = null;

				int x = 0;
				int y = 0;
				int w = 0;
				int h = 0;

				if (currentYamlTable != null)
				{
					var kids = GetKids(currentYamlTable);
					var headers = kids.Value.Nodes.Find(n => n.Key == "TableHeader");
					headercolumns = GetKids(headers);

					var rowtemplate = kids.Value.Nodes.Find(n => n.Key == "TableRow");
					rowcolumns = GetKids(rowtemplate);

					colspacing = NodeUtils.GetIntValue(currentYamlTable, "ColumnSpacing");
					rowspacing = NodeUtils.GetIntValue(currentYamlTable, "RowSpacing");

					h = NodeUtils.GetIntValue(currentYamlTable, "Height");
				}
				else
				{
					rowspacing = 1;

					textfontbrush = whitebrush;

					var kids = GetKids(currentYamlBarstats);
					var headtemplate = NodeUtils.GetValueNode(kids, "Spacing@OBSSTATSBOTTOM_TEMPLATE");
					kids = GetKids(headtemplate);

					var container = NodeUtils.GetValueNode(kids, "Background@OBSSTATSBOTTOM_TEMPLATE_CONTAINER");
					headercolumns = GetKids(container);
					rowcolumns = headercolumns;

					h = NodeUtils.GetIntValue(headtemplate, "Height");

					deffont = new Font("Courier", 12);
				}

				// precalc width
				for (int iCol = 0; iCol < headercolumns.Value.Nodes.Count; iCol++)
				{
					var col = headercolumns.Value.Nodes[iCol];
					var thisw = NodeUtils.GetIntValue(col, "Width");
					w += thisw;
				}

				if (currentYamlTable != null)
				{
					e.Graphics.FillRectangle(darkredbrush, new RectangleF(0, 0, w + 3 + (colspacing * headercolumns.Value.Nodes.Count), (h + rowspacing) * 2 + 3));
				}
				else if (currentYamlBarstats != null)
				{
					e.Graphics.FillRectangle(darkbrush, new RectangleF(0, 0, GuessCurrentBarstatsWidth(), h));
				}

				w = 0;
				for (int iCol = 0; iCol < headercolumns.Value.Nodes.Count; iCol++)
				{
					var col = headercolumns.Value.Nodes[iCol];
					var rowcol = rowcolumns.Value.Nodes[iCol];

					var thisw = NodeUtils.GetIntValue(col, "Width");

					if (currentYamlTable != null)
					{
						if (col.Key.StartsWith("Label"))
						{
							e.Graphics.DrawString(NodeUtils.GetTextValue(col, "Text"), deffont, whitebrush, new RectangleF(3 + w + (colspacing * iCol), 3, thisw, h - 3));
						}
					}

					if (rowcol.Key.StartsWith("Label"))
					{
						var bgcolor = NodeUtils.GetTextValue(rowcol, "BackgroundColor");
						if (bgcolor != "")
						{
							e.Graphics.FillRectangle(blackbrush, new Rectangle(2 + w + (colspacing *iCol), 2 + h + rowspacing, thisw, h));
						}

						if (currentYamlTable != null)
						{
							x = 3 + w + (colspacing * iCol);
							y = 3 + h + rowspacing;
						}
						else if (currentYamlBarstats != null)
						{
							x = NodeUtils.GetIntValue(col, "X");
							y = NodeUtils.GetIntValue(col, "Y") + 3;
						}

						var textrect = new RectangleF(x, y, thisw, h - 3);

						if (rowcol.Key == "Label@PLAYER")
						{
							e.Graphics.DrawString("Player 1", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@ENERGY")
						{
							e.Graphics.DrawString("100/100", deffont, greenbrush, textrect);
						}
						else if (rowcol.Key == "Label@CASH")
						{
							e.Graphics.DrawString("123", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@INCOME")
						{
							e.Graphics.DrawString("500", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@SPENT")
						{
							e.Graphics.DrawString("321", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@HARVESTERS")
						{
							e.Graphics.DrawString("1", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@EARNED_MIN")
						{
							e.Graphics.DrawString("12", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@ACTIONS_MIN")
						{
							e.Graphics.DrawString("10.5", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@MAP")
						{
							e.Graphics.DrawString("10%", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@CONTROL")
						{
							e.Graphics.DrawString("10%", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@KILLS_COST")
						{
							e.Graphics.DrawString("$1234", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@KILLS_COST_NOSIGN")
						{
							e.Graphics.DrawString("1234", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@DEATHS_COST")
						{
							e.Graphics.DrawString("$321", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@DEATHS_COST_NOSIGN")
						{
							e.Graphics.DrawString("321", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@UNITS_KILLED")
						{
							e.Graphics.DrawString("13", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@UNITS_DEAD")
						{
							e.Graphics.DrawString("10", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@BUILDINGS_KILLED")
						{
							e.Graphics.DrawString("3", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@BUILDINGS_DEAD")
						{
							e.Graphics.DrawString("1", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@ACTIONS_MIN_TXT")
						{
							e.Graphics.DrawString("10.5 APM", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@BUILDINGVALUE")
						{
							e.Graphics.DrawString("12", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@ARMYVALUE")
						{
							e.Graphics.DrawString("43", deffont, textfontbrush, textrect);
						}
						else if (rowcol.Key == "Label@STATICDEFVALUE")
						{
							e.Graphics.DrawString("2", deffont, textfontbrush, textrect);
						}
					}
					else if (rowcol.Key.StartsWith("Image"))
					{
						if (currentYamlTable != null)
						{
							x = 3 + w + (colspacing * iCol);
							y = 3 + h + rowspacing;
						}
						else if (currentYamlBarstats != null)
						{
							x = NodeUtils.GetIntValue(col, "X");
							y = NodeUtils.GetIntValue(col, "Y");
						}

						if (rowcol.Key == "Image@FLAG")
						{
							var collectionname = "flags";
							var imagename = "random";
							if (currentMod == "cnc") imagename = "gdi";
							if (currentMod == "ra") imagename = "allies";
							if (currentMod == "d2k") imagename = "atreides";

							try
							{
								var image = ChromeProvider.GetImage(collectionname, imagename);
								var imagerect = new Rectangle(x, y, image.bounds.Width, image.bounds.Height);
								var bmp = image.sheet.AsBitmap();
								e.Graphics.DrawImage(bmp, imagerect, new Rectangle(image.bounds.X, image.bounds.Y, image.bounds.Width, image.bounds.Height), GraphicsUnit.Pixel);
							}
							catch (Exception ex)
							{
								Log.Write("debug", "Could not find image '{0}.{1}': {2}", collectionname, imagename, ex.Message);
							}
						}
						else
						{
							var collectionname = NodeUtils.GetTextValue(rowcol, "ImageCollection");
							var imagename = NodeUtils.GetTextValue(rowcol, "ImageName");
							try
							{
								var image = ChromeProvider.GetImage(collectionname, imagename);
								var imagerect = new Rectangle(x, y, image.bounds.Width, image.bounds.Height);
								var bmp = image.sheet.AsBitmap();
								e.Graphics.DrawImage(bmp, imagerect, new Rectangle(image.bounds.X, image.bounds.Y, image.bounds.Width, image.bounds.Height), GraphicsUnit.Pixel);
							}
							catch(Exception ex)
							{
								Log.Write("debug", "Could not find image '{0}.{1}': {2}", collectionname, imagename, ex.Message);
							}
						}
					}

					w += thisw;
				}

				if (currentYamlTable != null)
				{
					e.Graphics.DrawRectangle(redpen, new Rectangle(0, 0, w + 3 + (colspacing * headercolumns.Value.Nodes.Count), (h + rowspacing) * 2 + 3));
				}
				else if (currentYamlBarstats != null)
				{
					e.Graphics.DrawRectangle(redpen, new Rectangle(0, 0, GuessCurrentBarstatsWidth(), h) );
				}
			}
		}

		private void miModCnc_Click(object sender, EventArgs e)
		{
			AskToSave();
			LoadMod("cnc");
		}

		private void miModRA_Click(object sender, EventArgs e)
		{
			AskToSave();
			LoadMod("ra");
		}

		private void miModD2k_Click(object sender, EventArgs e)
		{
			AskToSave();
			LoadMod("d2k");
		}

		private void miModTS_Click(object sender, EventArgs e)
		{
			AskToSave();
			LoadMod("ts");
		}

		public List<string> GetBackgroundOptions()
		{
			List<string> options = new List<string>();

			var chromeyaml = MiniYaml.FromFile(".\\mods\\" + currentMod + "\\chrome.yaml");
			foreach(var node in chromeyaml) {
				if (NodeUtils.GetValueNode(node, "border-r") != null)
				{
					options.Add(node.Key);
				}
			}

			return options;
		}

		public List<YamlSprite> GetYamlImageOptions()
		{
			List<YamlSprite> options = new List<YamlSprite>();

			var chromeyaml = MiniYaml.FromFile(".\\mods\\" + currentMod + "\\chrome.yaml");
			foreach (var collectionnode in chromeyaml)
			{
				foreach (var spritenode in collectionnode.Value.Nodes)
				{
					if (spritenode.Value.Value.EndsWith(",16,16") || (collectionnode.Key == "observericons"))
					{
						var option = new YamlSprite();
						option.Collection = collectionnode.Key;
						option.Bitmap = collectionnode.Value.Value;
						option.Name = spritenode.Key;
						option.RectStr = spritenode.Value.Value;
						options.Add(option);
					}
				}
			}

			return options;
		}

		private void cbBackground_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (currentYamlTable != null)
			{
				string s = "" + cbBackground.SelectedItem;
				var node = NodeUtils.GetValueNode(currentYamlTable, "Background");
				if ((node == null) && (s == "button"))
				{
					// do nothing, these are the default background settings
				}
				else if ((node != null) && (s == "button"))
				{
					NodeUtils.DeleteValueNode(currentYamlTable, "Background");
				}
				else
				{
					NodeUtils.SetTextValue(currentYamlTable, "Background", s);
				}
			}
			else if (currentYamlBarstats != null)
			{
				string s = "" + cbBackground.SelectedItem;

				var kids = GetKids(currentYamlBarstats);
				var headtemplate = NodeUtils.GetValueNode(kids, "Spacing@OBSSTATSBOTTOM_TEMPLATE");
				kids = GetKids(headtemplate);

				var container = NodeUtils.GetValueNode(kids, "Background@OBSSTATSBOTTOM_TEMPLATE_CONTAINER");

				var node = NodeUtils.GetValueNode(container, "Background");
				if (node == null)
				{
					node = new MiniYamlNode("Background", s);
					container.Value.Nodes.Add(node);
				}
				else
				{
					NodeUtils.SetTextValue(container, "Background", s);
				}
			}

			picExample.Refresh();
		}
	}
}

