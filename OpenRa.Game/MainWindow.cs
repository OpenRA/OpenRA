#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.GameRules;
using OpenRa.Graphics;

namespace OpenRa
{
	//class MainWindow : Form
	//{
	//    readonly Renderer renderer;

	//    static Size GetResolution(Settings settings)
	//    {
	//        var desktopResolution = Screen.PrimaryScreen.Bounds.Size;
	//        if (Game.Settings.Width > 0 && Game.Settings.Height > 0)
	//        {
	//            desktopResolution.Width = Game.Settings.Width;
	//            desktopResolution.Height = Game.Settings.Height;
	//        }
	//        return new Size(
	//            desktopResolution.Width,
	//            desktopResolution.Height);
	//    }

	//    [DllImport("user32")]
	//    static extern int ShowCursor([MarshalAs(UnmanagedType.Bool)] bool visible);

	//    public MainWindow(Settings settings)
	//    {
	//        Icon = Resources1.OpenRA;
	//        FormBorderStyle = FormBorderStyle.None;
	//        BackColor = Color.Black;
	//        StartPosition = FormStartPosition.Manual;
	//        Location = Point.Empty;
	//        Visible = true;
			
	//        while (!File.Exists("redalert.mix"))
	//        {
	//            var current = Directory.GetCurrentDirectory();
	//            if (Directory.GetDirectoryRoot(current) == current)
	//                throw new InvalidOperationException("Unable to load MIX files.");
	//            Directory.SetCurrentDirectory("..");
	//        }

			
	//        LoadUserSettings(settings);
	//        Game.LobbyInfo.GlobalSettings.Mods = Game.Settings.InitialMods;
			
	//        // Load the default mod to access required files
	//        Game.LoadModPackages(new Manifest(Game.LobbyInfo.GlobalSettings.Mods));
			
	//        UiOverlay.ShowUnitDebug = Game.Settings.UnitDebug;
	//        WorldRenderer.ShowUnitPaths = Game.Settings.PathDebug;
	//        Renderer.SheetSize = Game.Settings.SheetSize;
			
	//        bool windowed = !Game.Settings.Fullscreen;
	//        renderer = new Renderer(this, GetResolution(settings), windowed);

	//        var controller = new Controller(() => (Modifiers)(int)ModifierKeys);	/* a bit of insane input routing */

	//        Game.Initialize(Game.Settings.Map, renderer, new int2(ClientSize), Game.Settings.Player, controller);

	//        ShowCursor(false);
	//        Game.ResetTimer();
	//    }

	//    static void LoadUserSettings(Settings settings)
	//    {
	//        Game.Settings = new UserSettings();
	//        var settingsFile = settings.GetValue("settings", "settings.ini");
	//        FileSystem.Mount("./");
	//        if (FileSystem.Exists(settingsFile))
	//            FieldLoader.Load(Game.Settings,
	//                new IniFile(FileSystem.Open(settingsFile)).GetSection("Settings"));
	//        FileSystem.UnmountAll();
	//    }

	//    internal void Run()
	//    {
	//        while (Created && Visible)
	//        {
	//            Game.Tick();
	//            Application.DoEvents();
	//        }
	//    }

	//    int2 lastPos;

	//    protected override void OnMouseDown(MouseEventArgs e)
	//    {
	//        base.OnMouseDown(e);
	//        lastPos = new int2(e.Location);
	//        Game.DispatchMouseInput(MouseInputEvent.Down, e, ModifierKeys);
	//    }

	//    protected override void OnMouseMove(MouseEventArgs e)
	//    {
	//        base.OnMouseMove(e);

	//        if (e.Button == MouseButtons.Middle || e.Button == (MouseButtons.Left | MouseButtons.Right))
	//        {
	//            int2 p = new int2(e.Location);
	//            Game.viewport.Scroll(lastPos - p);
	//            lastPos = p;
	//        }

	//        Game.DispatchMouseInput(MouseInputEvent.Move, e, ModifierKeys);
	//    }

	//    protected override void OnMouseUp(MouseEventArgs e)
	//    {
	//        base.OnMouseUp(e);
	//        Game.DispatchMouseInput(MouseInputEvent.Up, e, ModifierKeys);
	//    }

	//    protected override void OnKeyDown(KeyEventArgs e)
	//    {
	//        base.OnKeyDown(e);

	//        Game.HandleKeyDown( e );
	//    }

	//    protected override void OnKeyPress(KeyPressEventArgs e)
	//    {
	//        base.OnKeyPress(e);

	//        Game.HandleKeyPress( e );
	//    }
	//}

	[Flags]
	public enum MouseButton
	{
		None = (int)MouseButtons.None,
		Left = (int)MouseButtons.Left,
		Right = (int)MouseButtons.Right,
		Middle = (int)MouseButtons.Middle,
	}

	[Flags]
	public enum Modifiers
	{
		None = (int)Keys.None,
		Shift = (int)Keys.Shift,
		Alt = (int)Keys.Alt,
		Ctrl = (int)Keys.Control,
	}

	public struct MouseInput
	{
		public MouseInputEvent Event;
		public int2 Location;
		public MouseButton Button;
		public Modifiers Modifiers;
	}

	public enum MouseInputEvent { Down, Move, Up };
}
