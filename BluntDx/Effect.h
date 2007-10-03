#pragma once

using namespace System::Collections::Generic;
using namespace IjwFramework::Collections;
using namespace IjwFramework::Delegates;

namespace BluntDirectX { namespace Direct3D
{
	public enum class ShaderQuality
	{
		Low,
		Medium,
		High,
	};

	public ref class Shader
	{
	private:
		static ID3DXEffectPool* effectPool;

		static Shader()
		{
			ID3DXEffectPool* e;
			D3DXCreateEffectPool( &e );
			effectPool = e;
		}

		ShaderQuality shaderQuality;

		IntPtr GetHandle( String^ symbol )
		{
			array<unsigned char>^ chars = System::Text::Encoding::ASCII->GetBytes(symbol);
			pin_ptr<const unsigned char> p = &chars[0];
			IntPtr result = IntPtr((void*)effect->GetParameterByName(NULL, (char*)p));
			return result;
		}

	internal:
		ID3DXEffect* effect;
		Cache<String^,IntPtr>^ parameters;

	public:
		Shader(GraphicsDevice^ device, Stream^ data)
		{
			parameters = gcnew Cache<String^,IntPtr>( gcnew Provider<IntPtr,String^>(this, &Shader::GetHandle));

			ID3DXEffect* e;
			ID3DXBuffer* compilationErrors;
			HRESULT hr;

			data->Position = 0;
			array<unsigned char>^ bytes = gcnew array<unsigned char>((int)data->Length);
			data->Read( bytes, 0, (int)data->Length );

			pin_ptr<unsigned char> pdata = &bytes[0];

			if (FAILED( hr = D3DXCreateEffect( device->device, pdata, (UINT)data->Length, NULL, NULL,
				D3DXSHADER_PACKMATRIX_COLUMNMAJOR | D3DXSHADER_USE_LEGACY_D3DX9_31_DLL, effectPool, &e, &compilationErrors ) ))
			{
				String^ errors = gcnew String((char*)compilationErrors->GetBufferPointer());
				compilationErrors->Release();

				throw gcnew InvalidOperationException( String::Format(
					"Failed compiling shader; HRESULT={0}, Errors:\n{1}", 
					hr, errors ));
			}

			effect = e;
			Quality = ShaderQuality::High;
		}

		~Shader()
		{
			safe_release( effect );
		}

		property ShaderQuality Quality
		{
			ShaderQuality get() { return shaderQuality; }
			void set( ShaderQuality s )
			{
				if (shaderQuality == s)
					return;

				shaderQuality = s;
				switch(s)
				{
				case ShaderQuality::High:
					if (SUCCEEDED(effect->SetTechnique( "high_quality" )))
						break;
				case ShaderQuality::Medium:
					if (SUCCEEDED(effect->SetTechnique( "med_quality" )))
						break;
				case ShaderQuality::Low:
					if (SUCCEEDED(effect->SetTechnique( "low_quality" )))
						break;
				default:
					throw gcnew Exception( "Invalid shader quality" );
				}
			}
		};

		void Commit()
		{
			effect->CommitChanges();
		}

		void Render( IjwFramework::Delegates::Action^ action )
		{
			unsigned int passes;
			effect->Begin( &passes, D3DXFX_DONOTSAVESTATE | D3DXFX_DONOTSAVESHADERSTATE );

			for( unsigned int i = 0; i < passes; i++ )
			{
				effect->BeginPass( i );
				action();
				effect->EndPass();
			}

			effect->End();
		}

		generic< typename T> where T : value class
		void SetValue( String^ name, T value )
		{
			IntPtr handle = parameters[name];
			pin_ptr<T> pvalue = &value;
			effect->SetValue( (D3DXHANDLE)handle.ToPointer(), pvalue, sizeof(T) );
		}

		void SetValue( String^ name, Texture^ texture )
		{
			IntPtr handle = parameters[name];
			effect->SetTexture( (D3DXHANDLE)handle.ToPointer(), texture->texture );
		}

		property array<KeyValuePair<IntPtr, String^>>^ ShaderResources
		{
			array<KeyValuePair<IntPtr, String^>>^ get() 
			{
				List<KeyValuePair<IntPtr, String^>>^ resources = gcnew List<KeyValuePair<IntPtr, String^>>();
				int id = 0;
				D3DXHANDLE parameter;

				while( parameter = effect->GetParameter( NULL, id++ ) )
				{
					D3DXHANDLE annotation;

					if (!(annotation = effect->GetAnnotationByName( parameter, "src" )))
						continue;

					char* value;
					HRESULT hr;
					
					if (FAILED(hr = effect->GetString( annotation, (LPCSTR*)&value )))
						ThrowHelper::Hr(hr);

					resources->Add( KeyValuePair<IntPtr, String^>( IntPtr( (void*)parameter ), gcnew String(value) ) );
				}

				return resources->ToArray();
			}
		}
	};
}}