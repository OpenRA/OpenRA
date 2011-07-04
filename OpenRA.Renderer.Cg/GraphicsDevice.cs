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
using System.Drawing;
using System.IO;
using OpenRA.FileFormats.Graphics;
using Tao.Cg;
using Tao.OpenGl;
using Tao.Sdl;

[assembly: Renderer(typeof(OpenRA.Renderer.Cg.DeviceFactory))]

namespace OpenRA.Renderer.Cg
{
	public class DeviceFactory : IDeviceFactory
	{
		public IGraphicsDevice Create(Size size, WindowMode windowMode, bool vsync)
		{
			return new GraphicsDevice( size, windowMode, vsync );
		}
	}

	public class GraphicsDevice : IGraphicsDevice
	{
		Size windowSize;
		internal IntPtr cgContext;
		internal int vertexProfile, fragmentProfile;

		IntPtr surf;

		public Size WindowSize { get { return windowSize; } }

		public enum GlError
		{
			GL_NO_ERROR = Gl.GL_NO_ERROR,
			GL_INVALID_ENUM = Gl.GL_INVALID_ENUM,
			GL_INVALID_VALUE = Gl.GL_INVALID_VALUE,
			GL_STACK_OVERFLOW = Gl.GL_STACK_OVERFLOW,
			GL_STACK_UNDERFLOW = Gl.GL_STACK_UNDERFLOW,
			GL_OUT_OF_MEMORY = Gl.GL_OUT_OF_MEMORY,
			GL_TABLE_TOO_LARGE = Gl.GL_TABLE_TOO_LARGE,
			GL_INVALID_OPERATION = Gl.GL_INVALID_OPERATION,
		}

		internal static void CheckGlError()
		{
			var n = Gl.glGetError();
			if( n != Gl.GL_NO_ERROR )
			{
				var error = "GL Error: {0}\n{1}".F((GlError)n, new System.Diagnostics.StackTrace());
				WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}
		}

		static void WriteGraphicsLog(string message)
		{
			Log.AddChannel("graphics", "graphics.log");
			Log.Write("graphics", message);
			Log.Write("graphics", "");
			Log.Write("graphics", "OpenGL Information:");
			Log.Write("graphics",  "Vendor: {0}", Gl.glGetString(Gl.GL_VENDOR));
			Log.Write("graphics",  "Renderer: {0}", Gl.glGetString(Gl.GL_RENDERER));
			Log.Write("graphics",  "GL Version: {0}", Gl.glGetString(Gl.GL_VERSION));
			Log.Write("graphics",  "Shader Version: {0}", Gl.glGetString(Gl.GL_SHADING_LANGUAGE_VERSION));
			Log.Write("graphics", "Available extensions:");
			Log.Write("graphics", Gl.glGetString(Gl.GL_EXTENSIONS));
		}

		static Tao.Cg.Cg.CGerrorCallbackFuncDelegate CgErrorCallback = () =>
		{
			var err = Tao.Cg.Cg.cgGetError();
			var msg = "CG Error: {0}: {1}".F(err, Tao.Cg.Cg.cgGetErrorString(err));
			WriteGraphicsLog(msg);
			throw new InvalidOperationException("CG Error. See graphics.log for details");
		};

		public GraphicsDevice( Size size, WindowMode window, bool vsync )
		{
			Console.WriteLine("Using Cg renderer");
			Sdl.SDL_Init( Sdl.SDL_INIT_NOPARACHUTE | Sdl.SDL_INIT_VIDEO );
			Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_DOUBLEBUFFER, 1 );
			Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_RED_SIZE, 8 );
			Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_GREEN_SIZE, 8 );
			Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_BLUE_SIZE, 8 );
			Sdl.SDL_GL_SetAttribute( Sdl.SDL_GL_ALPHA_SIZE, 0 );

			int windowFlags = 0;
			switch( window )
			{
			case WindowMode.Fullscreen:
				windowFlags |= Sdl.SDL_FULLSCREEN;
				break;
			case WindowMode.PseudoFullscreen:
				windowFlags |= Sdl.SDL_NOFRAME;
				Environment.SetEnvironmentVariable( "SDL_VIDEO_WINDOW_POS", "0,0" );
				break;
			case WindowMode.Windowed:
				Environment.SetEnvironmentVariable( "SDL_VIDEO_CENTERED", "1" );
				break;
			default:
				break;
			}

			surf = Sdl.SDL_SetVideoMode( size.Width, size.Height, 0, Sdl.SDL_OPENGL | windowFlags );

			Sdl.SDL_WM_SetCaption( "OpenRA", "OpenRA" );
			Sdl.SDL_ShowCursor( 0 );
			Sdl.SDL_EnableUNICODE( 1 );
			Sdl.SDL_EnableKeyRepeat( Sdl.SDL_DEFAULT_REPEAT_DELAY, Sdl.SDL_DEFAULT_REPEAT_INTERVAL );

			CheckGlError();

			windowSize = size;

			cgContext = Tao.Cg.Cg.cgCreateContext();

			Tao.Cg.Cg.cgSetErrorCallback( CgErrorCallback );

			Tao.Cg.CgGl.cgGLRegisterStates( cgContext );
			Tao.Cg.CgGl.cgGLSetManageTextureParameters( cgContext, true );
			vertexProfile = CgGl.cgGLGetLatestProfile( CgGl.CG_GL_VERTEX );
			fragmentProfile = CgGl.cgGLGetLatestProfile( CgGl.CG_GL_FRAGMENT );

			Gl.glEnableClientState( Gl.GL_VERTEX_ARRAY );
			CheckGlError();
			Gl.glEnableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
			CheckGlError();

			Sdl.SDL_SetModState( 0 );	// i have had enough.
		}

		public void EnableScissor( int left, int top, int width, int height )
		{
			if( width < 0 ) width = 0;
			if( height < 0 ) height = 0;
			Gl.glScissor( left, windowSize.Height - ( top + height ), width, height );
			CheckGlError();
			Gl.glEnable( Gl.GL_SCISSOR_TEST );
			CheckGlError();
		}

		public void DisableScissor()
		{
			Gl.glDisable( Gl.GL_SCISSOR_TEST );
			CheckGlError();
		}

		public void Clear( Color c )
		{
			Gl.glClearColor( 0, 0, 0, 0 );
			CheckGlError();
			Gl.glClear( Gl.GL_COLOR_BUFFER_BIT );
			CheckGlError();
		}

		MouseButton lastButtonBits = (MouseButton)0;

		MouseButton MakeButton( byte b )
		{
			return b == Sdl.SDL_BUTTON_LEFT ? MouseButton.Left
				: b == Sdl.SDL_BUTTON_RIGHT ? MouseButton.Right
				: b == Sdl.SDL_BUTTON_MIDDLE ? MouseButton.Middle
				: b == Sdl.SDL_BUTTON_WHEELDOWN ? MouseButton.WheelDown
				: b == Sdl.SDL_BUTTON_WHEELUP ? MouseButton.WheelUp
				: 0;
		}

		Modifiers MakeModifiers( int raw )
		{
			return ( ( raw & Sdl.KMOD_ALT ) != 0 ? Modifiers.Alt : 0 )
				 | ( ( raw & Sdl.KMOD_CTRL ) != 0 ? Modifiers.Ctrl : 0 )
				 | ( ( raw & Sdl.KMOD_META ) != 0 ? Modifiers.Meta : 0 )
				 | ( ( raw & Sdl.KMOD_SHIFT ) != 0 ? Modifiers.Shift : 0 );
		}

		bool HandleSpecialKey( KeyInput k )
		{
			switch( k.VirtKey )
			{
			case Sdl.SDLK_F13:
				var path = Environment.GetFolderPath( Environment.SpecialFolder.Personal )
					+ Path.DirectorySeparatorChar + DateTime.UtcNow.ToString( "OpenRA-yyyy-MM-ddThhmmssZ" ) + ".bmp";
				Sdl.SDL_SaveBMP( surf, path );
				return true;

			case Sdl.SDLK_F4:
				if( k.Modifiers.HasModifier( Modifiers.Alt ) )
				{
					OpenRA.Game.Exit();
					return true;
				}
				return false;

			default:
				return false;
			}
		}

		public void Present()
		{
			Sdl.SDL_GL_SwapBuffers();
		}

		public void PumpInput( IInputHandler inputHandler )
		{
			Game.HasInputFocus = 0 != ( Sdl.SDL_GetAppState() & Sdl.SDL_APPINPUTFOCUS );

			var mods = MakeModifiers( Sdl.SDL_GetModState() );
			inputHandler.ModifierKeys( mods );
			MouseInput? pendingMotion = null;

			Sdl.SDL_Event e;
			while( Sdl.SDL_PollEvent( out e ) != 0 )
			{
				switch( e.type )
				{
				case Sdl.SDL_QUIT:
					OpenRA.Game.Exit();
					break;

				case Sdl.SDL_MOUSEBUTTONDOWN:
					{
						if( pendingMotion != null )
						{
							inputHandler.OnMouseInput( pendingMotion.Value );
							pendingMotion = null;
						}

						var button = MakeButton( e.button.button );
						lastButtonBits |= button;

						inputHandler.OnMouseInput( new MouseInput(
							MouseInputEvent.Down, button, new int2( e.button.x, e.button.y ), mods ) );
					} break;

				case Sdl.SDL_MOUSEBUTTONUP:
					{
						if( pendingMotion != null )
						{
							inputHandler.OnMouseInput( pendingMotion.Value );
							pendingMotion = null;
						}

						var button = MakeButton( e.button.button );
						lastButtonBits &= ~button;

						inputHandler.OnMouseInput( new MouseInput(
							MouseInputEvent.Up, button, new int2( e.button.x, e.button.y ), mods ) );
					} break;

				case Sdl.SDL_MOUSEMOTION:
					{
						pendingMotion = new MouseInput(
							MouseInputEvent.Move,
							lastButtonBits,
							new int2( e.motion.x, e.motion.y ),
							mods );
					} break;

				case Sdl.SDL_KEYDOWN:
					{
						var keyEvent = new KeyInput
						{
							Event = KeyInputEvent.Down,
							Modifiers = mods,
							UnicodeChar = (char)e.key.keysym.unicode,
							KeyName = Sdl.SDL_GetKeyName( e.key.keysym.sym ),
							VirtKey = e.key.keysym.sym
						};

						if( !HandleSpecialKey( keyEvent ) )
							inputHandler.OnKeyInput( keyEvent );
					} break;

				case Sdl.SDL_KEYUP:
					{
						var keyEvent = new KeyInput
						{
							Event = KeyInputEvent.Up,
							Modifiers = mods,
							UnicodeChar = (char)e.key.keysym.unicode,
							KeyName = Sdl.SDL_GetKeyName( e.key.keysym.sym ),
							VirtKey = e.key.keysym.sym
						};

						inputHandler.OnKeyInput( keyEvent );
					} break;
				}
			}

			if( pendingMotion != null )
			{
				inputHandler.OnMouseInput( pendingMotion.Value );
				pendingMotion = null;
			}

			CheckGlError();
		}

		public void DrawPrimitives( PrimitiveType pt, int firstVertex, int numVertices )
		{
			Gl.glDrawArrays( ModeFromPrimitiveType( pt ), firstVertex, numVertices );
			CheckGlError();
		}

		static int ModeFromPrimitiveType( PrimitiveType pt )
		{
			switch( pt )
			{
			case PrimitiveType.PointList: return Gl.GL_POINTS;
			case PrimitiveType.LineList: return Gl.GL_LINES;
			case PrimitiveType.TriangleList: return Gl.GL_TRIANGLES;
			case PrimitiveType.QuadList: return Gl.GL_QUADS;
			}
			throw new NotImplementedException();
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer( int size ) { return new VertexBuffer<Vertex>( this, size ); }
		public ITexture CreateTexture() { return new Texture( this ); }
		public ITexture CreateTexture( Bitmap bitmap ) { return new Texture( this, bitmap ); }
		public IShader CreateShader( string name ) { return new Shader( this, name ); }

		public int GpuMemoryUsed { get { return 0; } }
	}
}
