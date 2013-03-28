#region Copyright & License Information
/*
 * Copyright 2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Network;

namespace OpenRA.Exceptions
{
	public class OutOfSyncException : System.ApplicationException
	{
		public SyncReport.Report SyncReport;
		public OutOfSyncException(string message, SyncReport.Report syncreport) : base(message)
		{
			SyncReport = syncreport;
		}
	}
}

