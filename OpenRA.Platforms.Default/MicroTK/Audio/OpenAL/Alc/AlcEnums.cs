#region --- OpenTK.OpenAL License ---
/* AlcTokens.cs
 * C header: \OpenAL 1.1 SDK\include\Alc.h
 * Spec: http://www.openal.org/openal_webstf/specs/OpenAL11Specification.pdf
 * Copyright (c) 2008 Christoph Brandtner and Stefanos Apostolopoulos
 * See license.txt for license details
 * http://www.OpenTK.net */
#endregion

using System;

namespace OpenTK.Audio.OpenAL
{
	public enum AlcContextAttributes : int
	{
		Frequency = 0x1007,
		Refresh = 0x1008,
		Sync = 0x1009,
		MonoSources = 0x1010,
		StereoSources = 0x1011,
		EfxMaxAuxiliarySends = 0x20003,
	}

	public enum AlcError : int
	{
		NoError = 0,
		InvalidDevice = 0xA001,
		InvalidContext = 0xA002,
		InvalidEnum = 0xA003,
		InvalidValue = 0xA004,
		OutOfMemory = 0xA005,
	}

	public enum AlcGetString : int
	{
		DefaultDeviceSpecifier = 0x1004,
		Extensions = 0x1006,
		CaptureDefaultDeviceSpecifier = 0x311, // ALC_EXT_CAPTURE extension.
		DefaultAllDevicesSpecifier = 0x1012,
		// duplicates from AlcGetStringList:
		CaptureDeviceSpecifier = 0x310,
		DeviceSpecifier = 0x1005,
		AllDevicesSpecifier = 0x1013,
	}

	public enum AlcGetStringList : int
	{
		CaptureDeviceSpecifier = 0x310,
		DeviceSpecifier = 0x1005,
		AllDevicesSpecifier = 0x1013,
	}

	public enum AlcGetInteger : int
	{
		MajorVersion = 0x1000,
		MinorVersion = 0x1001,
		AttributesSize = 0x1002,
		AllAttributes = 0x1003,
		CaptureSamples = 0x312,
		EfxMajorVersion = 0x20001,
		EfxMinorVersion = 0x20002,
		EfxMaxAuxiliarySends = 0x20003,
	}
}
