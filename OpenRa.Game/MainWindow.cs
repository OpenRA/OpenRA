using System.Drawing;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using System.Runtime.InteropServices;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		readonly Renderer renderer;
		
		static Size GetResolution(Settings settings)
		{
			var desktopResolution = Screen.PrimaryScreen.Bounds.Size;

			return new Size(
				settings.GetValue("width", desktopResolution.Width),
				settings.GetValue("height", desktopResolution.Height));
		}

		[DllImport("user32")]
		static extern int ShowCursor([MarshalAs(UnmanagedType.Bool)] bool visible);

		public MainWindow(Settings settings)
		{
			FileSystem.Mount(new Folder("../../../../"));
			FileSystem.Mount(new Package("redalert.mix"));
			FileSystem.Mount(new Package("conquer.mix"));
			FileSystem.Mount(new Package("hires.mix"));
			FileSystem.Mount(new Package("general.mix"));
			FileSystem.Mount(new Package("local.mix"));
			FileSystem.Mount(new Package("sounds.mix"));
			FileSystem.Mount(new Package("speech.mix"));
			FileSystem.Mount(new Package("allies.mix"));
			FileSystem.Mount(new Package("russian.mix"));

			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.Black;
			StartPosition = FormStartPosition.Manual;
			Location = Point.Empty;
			Visible = true;

			bool windowed = !settings.GetValue("fullscreen", false);
			renderer = new Renderer(this, GetResolution(settings), windowed);
			SheetBuilder.Initialize(renderer);

			UiOverlay.ShowUnitDebug = settings.GetValue("udebug", false);
			UiOverlay.ShowBuildDebug = settings.GetValue("bdebug", false);
			WorldRenderer.ShowUnitPaths = settings.GetValue("pathdebug", false);
			Game.timestep = settings.GetValue("rate", 40);
			Game.Replay = settings.GetValue("replay", "");
			Game.NetworkHost = settings.GetValue( "host", "" );
			Game.NetworkPort = int.Parse( settings.GetValue( "port", "0" ) );

			Game.Initialize(settings.GetValue("map", "scm12ea.ini"), renderer, new int2(ClientSize),
				settings.GetValue("player", 1));

			SequenceProvider.ForcePrecache();

			Game.world.Add( new Actor( "mcv", Game.map.Offset + new int2( 5, 5 ), Game.players[ 1 ]) );
			Game.world.Add( new Actor( "mcv", Game.map.Offset + new int2( 7, 5 ), Game.players[ 2 ] ) );
			Game.world.Add( new Actor( "mcv", Game.map.Offset + new int2( 9, 5 ), Game.players[ 0 ] ) );
			Game.world.Add( new Actor( "jeep", Game.map.Offset + new int2( 9, 14 ), Game.players[ 1 ] ) );
			Game.world.Add( new Actor( "3tnk", Game.map.Offset + new int2( 12, 7 ), Game.players[ 1 ] ) );
			Game.world.Add(new Actor("apc", Game.map.Offset + new int2(13, 7), Game.players[1]));
			Game.world.Add(new Actor("ca", Game.map.Offset + new int2(40, 7), Game.players[1]));
			Game.world.Add(new Actor("e1", Game.map.Offset + new int2(9, 13), Game.players[1]));
			Game.world.Add(new Actor("arty", Game.map.Offset + new int2(10, 13), Game.players[1]));
			Game.world.Add(new Actor("heli", Game.map.Offset + new int2(11, 12), Game.players[1]));

			renderer.BuildPalette(Game.map);
			ShowCursor(false);
			Game.ResetTimer();
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

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			lastPos = new int2(e.Location);

			Game.viewport.DispatchMouseInput(new MouseInput
			{
				Button = e.Button,
				Event = MouseInputEvent.Down,
				Location = new int2(e.Location)
			});
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button == MouseButtons.Middle)
			{
				int2 p = new int2(e.Location);
				Game.viewport.Scroll(lastPos - p);
				lastPos = p;
			}

			Game.viewport.DispatchMouseInput(new MouseInput
			{
				Button = e.Button,
				Event = MouseInputEvent.Move,
				Location = new int2(e.Location)
			});
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			Game.viewport.DispatchMouseInput(new MouseInput
			{
				Button = e.Button,
				Event = MouseInputEvent.Up,
				Location = new int2(e.Location)
			});
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);

			if (e.KeyChar == '\r')
				Game.chat.Toggle();
			else if (Game.chat.isChatting)
				Game.chat.TypeChar(e.KeyChar);
		}
	}

	struct MouseInput
	{
		public MouseInputEvent Event;
		public int2 Location;
		public MouseButtons Button;
	}

	enum MouseInputEvent { Down, Move, Up };
}
