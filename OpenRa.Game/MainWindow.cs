using System.Drawing;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using OpenRa.TechTree;
using System.Runtime.InteropServices;

namespace OpenRa.Game
{
	using GRegion = OpenRa.Game.Graphics.Region;


	class MainWindow : Form
	{
		readonly Renderer renderer;
		
		public readonly Sidebar sidebar;

		static Size GetResolution(Settings settings)
		{
			Size desktopResolution = Screen.PrimaryScreen.Bounds.Size;

			return new Size(settings.GetValue("width", desktopResolution.Width),
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
			WorldRenderer.ShowUnitPaths = settings.GetValue("pathdebug", false);
			Game.Replay = settings.GetValue("replay", "");

			Game.Initialize(settings.GetValue("map", "scg11eb.ini"), renderer, new int2(ClientSize),
				settings.GetValue("player", 1));

			SequenceProvider.ForcePrecache();

			Game.world.Add( new Actor( "mcv", Game.map.Offset + new int2( 5, 5 ), Game.players[ 1 ]) );
			Game.world.Add( new Actor( "mcv", Game.map.Offset + new int2( 7, 5 ), Game.players[ 2 ] ) );
			Game.world.Add( new Actor( "mcv", Game.map.Offset + new int2( 9, 5 ), Game.players[ 0 ] ) );
			Game.world.Add( new Actor( "jeep", Game.map.Offset + new int2( 9, 15 ), Game.players[ 1 ] ) );
			Game.world.Add( new Actor( "3tnk", Game.map.Offset + new int2( 12, 7 ), Game.players[ 1 ] ) );

			sidebar = new Sidebar(renderer, Game.LocalPlayer);

			renderer.BuildPalette(Game.map);

			ShowCursor(false);

			Game.world.ResetTimer();
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Game.Tick();
				Game.viewport.cursor = Game.controller.ChooseCursor();
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

			if (Game.controller.orderGenerator != null)
				Game.controller.orderGenerator.PrepareOverlay(
					((1 / 24f) * (new float2(e.Location) + Game.viewport.Location)).ToInt2());
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
	}

    struct MouseInput
    {
        public MouseInputEvent Event;
        public int2 Location;
        public MouseButtons Button;
    }

    enum MouseInputEvent { Down, Move, Up };
}
