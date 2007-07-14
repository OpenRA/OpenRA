#pragma once

namespace BluntDirectX { namespace Direct3D
{	
	generic< typename T >
	public value class Range
	{
		T start, end;
	public:
		Range( T start, T end )
			: start( start ), end( end )
		{
		}

		property T Start { T get() { return start; } }
		property T End { T get() { return end; } }
	};

	public ref class GraphicsDevice
	{
	private:
		GraphicsDevice( IDirect3DDevice9* device, bool hasHardwareVp ) : device( device ), hardwareVp( hasHardwareVp )
		{
			IDirect3DSurface9* s;
			device->GetRenderTarget(0, &s);
			defaultSurface = s;
		}

		bool hardwareVp;

	internal:
		IDirect3DDevice9* device;
		IDirect3DSurface9* defaultSurface;

		void SetRenderTarget( IDirect3DSurface9* surface )
		{
			if (surface == nullptr)
				surface = defaultSurface;

			device->SetRenderTarget( 0, surface );
		}

	public:
		void BindDefaultRenderTarget()
		{
			SetRenderTarget( nullptr );
		}

		void BindSurface( IntPtr p )
		{
			SetRenderTarget( (IDirect3DSurface9*) p.ToPointer() );
		}

		IntPtr GetDefaultSurface()
		{
			return IntPtr(defaultSurface);
		}

		void Begin()
		{
			device->BeginScene();
		}

		void End()
		{
			device->EndScene();
		}

		~GraphicsDevice()
		{
			safe_release( device );
		}

		void Clear( int color, Surfaces surfaces )
		{
			device->Clear( 0, NULL, (DWORD)surfaces, (D3DCOLOR)color, 1.0f, 0 );
		}

		void Present()
		{
			device->Present(NULL,NULL,NULL,NULL);
		}

		property bool HasHardwareVP
		{
			bool get() { return hardwareVp; } 
		}

		static GraphicsDevice^ CreateRenderless()
		{
			IDirect3D9* d3d = Direct3DCreate9( D3D_SDK_VERSION );
			D3DPRESENT_PARAMETERS pp;
			memset( &pp, 0, sizeof(pp) );

			pp.BackBufferCount = 1;
			pp.BackBufferFormat = D3DFMT_UNKNOWN;
			pp.BackBufferWidth = pp.BackBufferHeight = 1;
			pp.SwapEffect = D3DSWAPEFFECT_COPY;
			pp.Windowed = true;

			IDirect3DDevice9* dev;
			HRESULT hr;
			
			if (FAILED(hr = d3d->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_NULLREF, GetConsoleWindow(),
				D3DCREATE_HARDWARE_VERTEXPROCESSING, &pp, &dev)))
				ThrowHelper::Hr(hr);

			return gcnew GraphicsDevice( dev, true );
		}

		static GraphicsDevice^ Create(Control^ host, int width, int height, bool windowed, bool vsync)
		{
			IDirect3D9* d3d = Direct3DCreate9( D3D_SDK_VERSION );
			D3DPRESENT_PARAMETERS pp;
			memset( &pp, 0, sizeof(pp) );

			pp.BackBufferCount = vsync ? 1 : 2;
			pp.BackBufferWidth = width;
			pp.BackBufferHeight = height;
			pp.BackBufferFormat = D3DFMT_X8R8G8B8;

			pp.EnableAutoDepthStencil = false;
			pp.SwapEffect = D3DSWAPEFFECT_DISCARD;
			
			pp.Windowed = windowed;
			pp.PresentationInterval = (vsync && !windowed) ? D3DPRESENT_INTERVAL_ONE : D3DPRESENT_INTERVAL_IMMEDIATE;

			DWORD flags[] = {
				D3DCREATE_PUREDEVICE | D3DCREATE_HARDWARE_VERTEXPROCESSING,
				D3DCREATE_HARDWARE_VERTEXPROCESSING,
				D3DCREATE_MIXED_VERTEXPROCESSING,
				D3DCREATE_SOFTWARE_VERTEXPROCESSING,
				0
			};

			DWORD* pf = &flags[0];
			while(*pf)
			{
				IDirect3DDevice9* pd;
				HRESULT hr;

				bool configHasHardwareVp = 0 != (*pf & D3DCREATE_HARDWARE_VERTEXPROCESSING);

				if (SUCCEEDED( hr = d3d->CreateDevice( D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, (HWND)host->Handle.ToInt32(),
					*pf, &pp, &pd)))
					return gcnew GraphicsDevice( pd, configHasHardwareVp  );

				++pf;
			}

			throw gcnew ApplicationException("D3D not available.");
		}

		void EnableScissor( int left, int top, int width, int height )
		{
			RECT r = { left, top, left + width, top + height };
			device->SetScissorRect( &r );
			device->SetRenderState( D3DRS_SCISSORTESTENABLE, true );
		}

		void DisableScissor()
		{
			device->SetRenderState( D3DRS_SCISSORTESTENABLE, false );
		}

		void DrawPrimitives(PrimitiveType primtype, int count)
		{
			device->DrawPrimitive( (D3DPRIMITIVETYPE)primtype, 0, count );
		}

		void DrawIndexedPrimitives(PrimitiveType primtype, int vertexPoolSize, int numPrimitives)
		{
			device->DrawIndexedPrimitive( (D3DPRIMITIVETYPE)primtype, 0, 0, vertexPoolSize, 0, numPrimitives );
		}

		void DrawIndexedPrimitives(PrimitiveType primType, Range<int> vertices, Range<int> indices)
		{
			device->DrawIndexedPrimitive( (D3DPRIMITIVETYPE)primType,
				0, vertices.Start, vertices.End - vertices.Start, 
				indices.Start, (indices.End - indices.Start) / 3 );
		}
	};
}}
