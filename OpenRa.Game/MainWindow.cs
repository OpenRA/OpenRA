using System.Drawing;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using OpenRa.TechTree;

namespace OpenRa.Game
{
	using GRegion = OpenRa.Game.Graphics.Region;
using System.Runtime.InteropServices;

	class MainWindow : Form
	{
		readonly Renderer renderer;
		
		Game game;
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

			game = new Game(settings.GetValue("map", "scg11eb.ini"), renderer, new int2(ClientSize));

			SequenceProvider.ForcePrecache();
			Traits.RenderBuilding.Prefetch();

			game.world.Add( new Actor( "mcv", new int2( 5, 5 ), game.players[ 3 ]) );
			game.world.Add( new Actor( "mcv", new int2( 7, 5 ), game.players[ 2 ] ) );
			game.world.Add( new Actor( "mcv", new int2( 9, 5 ), game.players[ 0 ] ) );
			game.world.Add( new Actor( "jeep", new int2( 9, 7 ), game.players[ 1 ] ) );

			game.world.Add(new Bullet("105mm", game.players[1], null,
				new int2(200, 200), new int2(400, 200), game));

			sidebar = new Sidebar(renderer, game);

			renderer.BuildPalette(game.map);

			ShowCursor(false);
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				game.Tick();

				// rude hack
				game.viewport.cursor = (game.controller.orderGenerator is UnitOrderGenerator)
					&& (game.controller.orderGenerator as UnitOrderGenerator).selection.Count > 0
					? OpenRa.Game.Cursor.Move : OpenRa.Game.Cursor.Default;

				Application.DoEvents();
			}
		}

		int2 lastPos;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            lastPos = new int2(e.Location);

            game.viewport.DispatchMouseInput(new MouseInput
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
				game.viewport.Scroll(lastPos - p);
				lastPos = p;
			}

            game.viewport.DispatchMouseInput(new MouseInput
            {
                Button = e.Button,
                Event = MouseInputEvent.Move,
                Location = new int2(e.Location)
            });

			if (game.controller.orderGenerator != null)
				game.controller.orderGenerator.PrepareOverlay(game,
					((1 / 24f) * (new float2(e.Location) + game.viewport.Location)).ToInt2());
		}

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            game.viewport.DispatchMouseInput(new MouseInput
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
