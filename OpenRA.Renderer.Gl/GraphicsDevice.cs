#region Copyright & License Information
/*
* Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
* This file is part of OpenRA, which is free software. It is made 
* available to you under the terms of the GNU General Public License
* as published by the Free Software Foundation. For more information,
* see LICENSE.
*/
#endregion

using System;
using System.Drawing;
using System.IO;
using OpenRA.FileFormats.Graphics;
using Tao.Cg;
using Tao.OpenGl;
using Tao.Sdl;

[assembly: Renderer(typeof(OpenRA.Renderer.Glsl.GraphicsDevice))]

namespace OpenRA.Renderer.Glsl
{
	public class GraphicsDevice : IGraphicsDevice
	{
		Size windowSize;
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
		}

		internal static void CheckGlError()
		{
			var n = Gl.glGetError();
			if( n != Gl.GL_NO_ERROR )
				throw new InvalidOperationException( "GL Error: " + ( (GlError)n ).ToString() );
		}

		public GraphicsDevice( int width, int height, WindowMode window, bool vsync )
		{
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
				// pseudo-fullscreen only reliably works on windows; fall back to fullscreen for everyone else
				windowFlags |= ( Environment.OSVersion.Platform == PlatformID.Win32NT ) ? Sdl.SDL_NOFRAME : Sdl.SDL_FULLSCREEN;
				Environment.SetEnvironmentVariable( "SDL_VIDEO_WINDOW_POS", "0,0" );
				break;
			default:
				break;
			}

			surf = Sdl.SDL_SetVideoMode( width, height, 0, Sdl.SDL_OPENGL | windowFlags );

			Sdl.SDL_WM_SetCaption( "OpenRA", "OpenRA" );
			Sdl.SDL_ShowCursor( 0 );
			Sdl.SDL_EnableUNICODE( 1 );
			Sdl.SDL_EnableKeyRepeat( Sdl.SDL_DEFAULT_REPEAT_DELAY, Sdl.SDL_DEFAULT_REPEAT_INTERVAL );

			CheckGlError();

			windowSize = new Size( width, height );

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

		static MouseButton MakeButton( byte b )
		{
			return b == Sdl.SDL_BUTTON_LEFT ? MouseButton.Left
				: b == Sdl.SDL_BUTTON_RIGHT ? MouseButton.Right
				: b == Sdl.SDL_BUTTON_MIDDLE ? MouseButton.Middle
				: 0;
		}

		static Modifiers MakeModifiers( int raw )
		{
			return ( ( raw & Sdl.KMOD_ALT ) != 0 ? Modifiers.Alt : 0 )
				 | ( ( raw & Sdl.KMOD_CTRL ) != 0 ? Modifiers.Ctrl : 0 )
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

		public void Present( IInputHandler inputHandler )
		{
			Sdl.SDL_GL_SwapBuffers();
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
							KeyChar = (char)e.key.keysym.unicode,
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
							KeyChar = (char)e.key.keysym.unicode,
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

		public void DrawIndexedPrimitives( PrimitiveType pt, Range<int> vertices, Range<int> indices )
		{
			Gl.glDrawElements( ModeFromPrimitiveType( pt ), indices.End - indices.Start,
			Gl.GL_UNSIGNED_SHORT, new IntPtr( indices.Start * 2 ) );
			CheckGlError();
		}

		public void DrawIndexedPrimitives( PrimitiveType pt, int numVerts, int numPrimitives )
		{
			Gl.glDrawElements( ModeFromPrimitiveType( pt ), numPrimitives * IndicesPerPrimitive( pt ),
			Gl.GL_UNSIGNED_SHORT, IntPtr.Zero );
			CheckGlError();
		}

		static int ModeFromPrimitiveType( PrimitiveType pt )
		{
			switch( pt )
			{
			case PrimitiveType.PointList: return Gl.GL_POINTS;
			case PrimitiveType.LineList: return Gl.GL_LINES;
			case PrimitiveType.TriangleList: return Gl.GL_TRIANGLES;
			}
			throw new NotImplementedException();
		}

		static int IndicesPerPrimitive( PrimitiveType pt )
		{
			switch( pt )
			{
			case PrimitiveType.PointList: return 1;
			case PrimitiveType.LineList: return 2;
			case PrimitiveType.TriangleList: return 3;
			}
			throw new NotImplementedException();
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer( int size ) { return new VertexBuffer<Vertex>( this, size ); }
		public IIndexBuffer CreateIndexBuffer( int size ) { return new IndexBuffer( this, size ); }
		public ITexture CreateTexture() { return new Texture( this ); }
		public ITexture CreateTexture( Bitmap bitmap ) { return new Texture( this, bitmap ); }
		public IShader CreateShader( string name ) { return new Shader( this, name ); }
	}
}
