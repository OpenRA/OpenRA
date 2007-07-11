#pragma once

namespace BluntDirectX { namespace Direct3D
{
	public ref class IndexBuffer
	{
	internal:
		IDirect3DIndexBuffer9* buffer;
		IDirect3DDevice9* device;
		int size;

	public:
		IndexBuffer( GraphicsDevice^ device, int size )
			: size( size ), device( device->device )
		{
			HRESULT hr;
			IDirect3DIndexBuffer9* b;

			if (FAILED( hr = this->device->CreateIndexBuffer( sizeof( unsigned short ) * size,
				device->HasHardwareVP ? 0 : D3DUSAGE_SOFTWAREPROCESSING, D3DFMT_INDEX16, D3DPOOL_SYSTEMMEM, &b, NULL )))
				ThrowHelper::Hr(hr);

			buffer = b;
		}

		void SetData( array<unsigned short>^ data )
		{
			pin_ptr<unsigned short> pdata = &data[0];
			void * ib;

			buffer->Lock( 0, sizeof(unsigned short) * size, &ib, D3DLOCK_DISCARD );
			memcpy( ib, pdata, sizeof(unsigned short) * data.Length );
			buffer->Unlock();
		}

		void Bind()
		{
			device->SetIndices( buffer );
		}

		~IndexBuffer()
		{
			safe_release( buffer );
		}

		property int Size { int get() { return size; } }
	};

	generic<typename T> where T : value class
	public ref class FvfVertexBuffer
	{
	internal:
		IDirect3DVertexBuffer9* buffer;
		IDirect3DDevice9* device;
		int numVertices;
		unsigned int fvf;

	public:
		FvfVertexBuffer( GraphicsDevice^ device, int numVertices, VertexFormat fvf ) 
			: numVertices( numVertices ), device(device->device), fvf((unsigned int)fvf)
		{
			HRESULT hr;
			IDirect3DVertexBuffer9* b;

			if (FAILED( hr = device->device->CreateVertexBuffer( sizeof(T) * numVertices, 
				device->HasHardwareVP ? 0 : D3DUSAGE_SOFTWAREPROCESSING, this->fvf, D3DPOOL_SYSTEMMEM, &b, NULL )))
				ThrowHelper::Hr(hr);

			buffer = b;
		}

		void SetData( array<T>^ data )
		{
			pin_ptr<T> pdata = &data[0];
			void* vb;

			buffer->Lock( 0, sizeof(T) * numVertices, &vb, D3DLOCK_DISCARD );
			memcpy( vb, pdata, sizeof(T) * data->Length );
			buffer->Unlock();
		}

		void Bind( int stream )
		{
			device->SetStreamSource( stream, buffer, 0, sizeof(T) );
			device->SetFVF( fvf );
		}

		~FvfVertexBuffer()
		{
			safe_release( buffer );
		}

		property int Size { int get() { return numVertices; } }
		property VertexFormat FVF { VertexFormat get() { return (VertexFormat)fvf; }}
	};

	generic<typename T> where T : value class
	public ref class VertexBuffer
	{
	internal:
		IDirect3DVertexBuffer9* buffer;
		IDirect3DVertexDeclaration9* decl;
		IDirect3DDevice9* device;
		int numVertices;

	public:
		VertexBuffer( GraphicsDevice^ device, int numVertices ) 
			: numVertices( numVertices ), device(device->device)
		{
			HRESULT hr;
			IDirect3DVertexBuffer9* b;
			IDirect3DVertexDeclaration9* d;
			
			D3DVERTEXELEMENT9 elements[] = 
			{
				{0,0, D3DDECLTYPE_FLOAT3, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_POSITION, 0},
				{0,12,D3DDECLTYPE_FLOAT4, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_COLOR,0},
				{0,28,D3DDECLTYPE_FLOAT4, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_COLOR,1},
				{0,44,D3DDECLTYPE_FLOAT1, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_PSIZE,0},

				D3DDECL_END()
			};

			if (FAILED( hr = device->device->CreateVertexDeclaration( elements, &d ) ))
				ThrowHelper::Hr(hr);

			decl = d;

			if (FAILED( hr = device->device->CreateVertexBuffer( sizeof(T) * numVertices, 
				D3DUSAGE_DYNAMIC | D3DUSAGE_POINTS | 
				(device->HasHardwareVP ? 0 : D3DUSAGE_SOFTWAREPROCESSING), 0, D3DPOOL_DEFAULT, &b, NULL )))
				ThrowHelper::Hr(hr);

			buffer = b;
		}

		void SetData( array<T>^ data )
		{
			pin_ptr<T> pdata = &data[0];
			void* vb;

			buffer->Lock( 0, sizeof(T) * numVertices, &vb, D3DLOCK_DISCARD );
			memcpy( vb, pdata, sizeof(T) * data->Length );
			buffer->Unlock();
		}

		void Bind( int stream )
		{
			device->SetStreamSource( stream, buffer, 0, sizeof(T) );
			device->SetVertexDeclaration( decl );
		}

		~VertexBuffer()
		{
			safe_release( decl );
			safe_release( buffer );
		}

		property int Size { int get() { return numVertices; } }
	};
}}