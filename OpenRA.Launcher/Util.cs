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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OpenRA.Launcher
{
	static class Util
	{
		[DllImport("user32")]
		public static extern UInt32 SendMessage
			(IntPtr hWnd, UInt32 msg, UInt32 wParam, UInt32 lParam);

		internal const int BCM_FIRST = 0x1600; //Normal button

		internal const int BCM_SETSHIELD = (BCM_FIRST + 0x000C); //Elevated button

		static public void UacShield(Button b)
		{
			b.FlatStyle = FlatStyle.System;
			SendMessage(b.Handle, BCM_SETSHIELD, 0, 0xFFFFFFFF);
		}

		static public bool IsError(ref string utilityResponseLine)
		{
			utilityResponseLine = utilityResponseLine.Trim('\r', '\n');
			if (utilityResponseLine.StartsWith("Error:"))
			{
				utilityResponseLine = utilityResponseLine.Remove(0, 7);
				return true;
			}

			return false;
		}
	}
}
