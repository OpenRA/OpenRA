#region License
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2009 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Text;

using OpenTK.Audio.OpenAL;

namespace OpenTK.Audio
{
	struct AudioDeviceErrorChecker : IDisposable
	{
		#region Fields

		readonly IntPtr Device;
		static readonly string ErrorString = "Device {0} reported {1}.";

		#endregion

		#region Constructors

		public AudioDeviceErrorChecker(IntPtr device)
		{
			if (device == IntPtr.Zero)
				throw new AudioDeviceException();

			Device = device;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			AlcError err = Alc.GetError(Device);
			switch (err)
			{
			case AlcError.OutOfMemory:
				throw new OutOfMemoryException(String.Format(ErrorString, Device, err));

			case AlcError.InvalidValue:
				throw new AudioValueException(String.Format(ErrorString, Device, err));

			case AlcError.InvalidDevice:
				throw new AudioDeviceException(String.Format(ErrorString, Device, err));

			case AlcError.InvalidContext:
				throw new AudioContextException(String.Format(ErrorString, Device, err));

			case AlcError.NoError:
			default:
				// everything went fine, do nothing
				break;
			}
		}

		#endregion
	}
}
