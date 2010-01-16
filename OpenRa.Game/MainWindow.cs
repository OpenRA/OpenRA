using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Orders;


namespace OpenRa.Game
{
	class MainWindow : Form
	{
		readonly Renderer renderer;

		static Size GetResolution(Settings settings)
		{
			var desktopResolution = Screen.PrimaryScreen.Bounds.Size;
			if (Game.Settings.Width > 0 && Game.Settings.Height > 0)
			{
				desktopResolution.Width = Game.Settings.Width;
				desktopResolution.Height = Game.Settings.Height;
			}
			return new Size(
				desktopResolution.Width,
				desktopResolution.Height);
		}

		[DllImport("user32")]
		static extern int ShowCursor([MarshalAs(UnmanagedType.Bool)] bool visible);

		public MainWindow(Settings settings)
		{
			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.Black;
			StartPosition = FormStartPosition.Manual;
			Location = Point.Empty;
			Visible = true;
			
			while (!File.Exists("redalert.mix"))
			{
				var current = Directory.GetCurrentDirectory();
				if (Directory.GetDirectoryRoot(current) == current)
					throw new InvalidOperationException("Unable to load MIX files.");
				Directory.SetCurrentDirectory("..");
			}

			LoadUserSettings(settings);

			UiOverlay.ShowUnitDebug = Game.Settings.UnitDebug;
			UiOverlay.ShowBuildDebug = Game.Settings.BuildingDebug;
			WorldRenderer.ShowUnitPaths = Game.Settings.PathDebug;
			Renderer.SheetSize = Game.Settings.SheetSize;

			FileSystem.MountDefaultPackages();
			
			if (Game.Settings.UseAftermath)
				FileSystem.MountAftermathPackages();

			bool windowed = !Game.Settings.Fullscreen;
			renderer = new Renderer(this, GetResolution(settings), windowed);

			var controller = new Controller(() => (Modifiers)(int)ModifierKeys);	/* a bit of insane input routing */

			Game.Initialize(Game.Settings.Map, renderer, new int2(ClientSize),
				Game.Settings.Player, Game.Settings.UseAftermath, controller);

			ShowCursor(false);
			Game.ResetTimer();
		}

		static void LoadUserSettings(Settings settings)
		{
			Game.Settings = new UserSettings();
			var settingsFile = settings.GetValue("settings", "settings.ini");
			FileSystem.MountTemporary(new Folder("./"));
			if (FileSystem.Exists(settingsFile))
				FieldLoader.Load(Game.Settings,
					new IniFile(FileSystem.Open(settingsFile)).GetSection("Settings"));
			FileSystem.UnmountTemporaryPackages();
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Game.Tick();
				Application.DoEvents();
			}
		}

		int2 lastPos;

		void DispatchMouseInput(MouseInputEvent ev, MouseEventArgs e)
		{
			int sync = Game.world.SyncHash();

			Game.viewport.DispatchMouseInput(
				new MouseInput
				{
					Button = (MouseButton)(int)e.Button,
					Event = ev,
					Location = new int2(e.Location),
					Modifiers = (Modifiers)(int)ModifierKeys,
				});

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in DispatchMouseInput" );
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			lastPos = new int2(e.Location);
			DispatchMouseInput(MouseInputEvent.Down, e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button == MouseButtons.Middle || e.Button == (MouseButtons.Left | MouseButtons.Right))
			{
				int2 p = new int2(e.Location);
				Game.viewport.Scroll(lastPos - p);
				lastPos = p;
			}

			DispatchMouseInput(MouseInputEvent.Move, e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			DispatchMouseInput(MouseInputEvent.Up, e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			int sync = Game.world.SyncHash();

			/* hack hack hack */
			if (e.KeyCode == Keys.F8 && !Game.orderManager.GameStarted)
			{
				Game.controller.AddOrder(
					new Order( "ToggleReady", Game.LocalPlayer.PlayerActor, null, int2.Zero, "") { IsImmediate = true });
			}

			/* temporary hack: DO NOT LEAVE IN */
			if (e.KeyCode == Keys.F2)
				Game.LocalPlayer = Game.players[(Game.LocalPlayer.Index + 1) % 4];
			if (e.KeyCode == Keys.F3)
				Game.controller.orderGenerator = new SellOrderGenerator();
			if (e.KeyCode == Keys.F4)
				Game.controller.orderGenerator = new RepairOrderGenerator();				

			if (!Game.chat.isChatting)
				if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
					Game.controller.DoControlGroup( (int)e.KeyCode - (int)Keys.D0, (Modifiers)(int)e.Modifiers );

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in OnKeyDown" );
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);

			int sync = Game.world.SyncHash();

			if (e.KeyChar == '\r')
				Game.chat.Toggle();
			else if (Game.chat.isChatting)
				Game.chat.TypeChar(e.KeyChar);

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in OnKeyPress" );
		}
	}

	[Flags]
	enum MouseButton
	{
		None = (int)MouseButtons.None,
		Left = (int)MouseButtons.Left,
		Right = (int)MouseButtons.Right,
		Middle = (int)MouseButtons.Middle,
	}

	[Flags]
	enum Modifiers
	{
		None = (int)Keys.None,
		Shift = (int)Keys.Shift,
		Alt = (int)Keys.Alt,
		Ctrl = (int)Keys.Control,
	}

	struct MouseInput
	{
		public MouseInputEvent Event;
		public int2 Location;
		public MouseButton Button;
		public Modifiers Modifiers;
	}

	enum MouseInputEvent { Down, Move, Up };
}
