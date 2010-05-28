#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Net;
using System.IO.Compression;
using System.IO;

namespace OpenRA
{
	static class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			// brutal hack
			Application.CurrentCulture = CultureInfo.InvariantCulture;

			if( Debugger.IsAttached )
			{
				Run( args );
				return;
			}

			try
			{
				Run( args );
			}
			catch( Exception e )
			{
				Log.Write( "{0}", e.ToString() );
				UploadLog();
				throw;
			}
		}

		static void UploadLog()
		{
			Log.Close();
			var logfile = File.OpenRead(Log.Filename);
			byte[] fileContents = logfile.ReadAllBytes();
			var ms = new MemoryStream();
			
			using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
				gzip.Write(fileContents, 0, fileContents.Length);
	
			ms.Position = 0;
			byte[] buffer = ms.ReadAllBytes();

			WebRequest request = WebRequest.Create("http://open-ra.org/logs/upload.php");
			request.ContentType = "application/x-gzip";
			request.ContentLength = buffer.Length;
			request.Method = "POST";
			request.Headers.Add("Game-ID", Game.MasterGameID.ToString());
	
			using (var requestStream = request.GetRequestStream())
				requestStream.Write(buffer, 0, buffer.Length);

			var response = (HttpWebResponse)request.GetResponse();
			MessageBox.Show("{0} {1}:{2}".F(Game.MasterGameID, Game.CurrentHost, Game.CurrentPort));
		}

		static void Run( string[] args )
		{
			Game.Initialize( new Settings( args ) );
			Game.Run();
		}
	}
}