#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	sealed class ReplayRecorder
	{
		public ReplayMetadata Metadata;
		BinaryWriter writer;
		Func<string> chooseFilename;
		MemoryStream preStartBuffer = new MemoryStream();

		static bool IsGameStart(byte[] data)
		{
			if (data.Length > 4 && (data[4] == (byte)OrderType.Disconnect || data[4] == (byte)OrderType.SyncHash))
				return false;

			var frame = BitConverter.ToInt32(data, 0);
			return frame == 0 && data.ToOrderList(null).Any(o => o.OrderString == "StartGame");
		}

		public ReplayRecorder(Func<string> chooseFilename)
		{
			this.chooseFilename = chooseFilename;

			writer = new BinaryWriter(preStartBuffer);
		}

		void StartSavingReplay(byte[] initialContent)
		{
			var filename = chooseFilename();
			var mod = Game.ModData.Manifest;
			var dir = Path.Combine(Platform.SupportDir, "Replays", mod.Id, mod.Metadata.Version);

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			FileStream file = null;
			var id = -1;
			while (file == null)
			{
				var fullFilename = Path.Combine(dir, id < 0 ? "{0}.orarep".F(filename) : "{0}-{1}.orarep".F(filename, id));
				id++;
				try
				{
					file = File.Create(fullFilename);
				}
				catch (IOException) { }
			}

			file.WriteArray(initialContent);
			writer = new BinaryWriter(file);
		}

		public void Receive(int clientID, byte[] data)
		{
			if (disposed) // TODO: This can be removed once NetworkConnection is fixed to dispose properly.
				return;

			if (preStartBuffer != null && IsGameStart(data))
			{
				writer.Flush();
				var preStartData = preStartBuffer.ToArray();
				preStartBuffer = null;
				StartSavingReplay(preStartData);
			}

			writer.Write(clientID);
			writer.Write(data.Length);
			writer.Write(data);
		}

		public void ReceiveFrame(int clientID, int frame, byte[] data)
		{
			var ms = new MemoryStream(4 + data.Length);
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteArray(data);
			Receive(clientID, ms.GetBuffer());
		}

		bool disposed;

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			if (Metadata != null)
			{
				if (Metadata.GameInfo != null)
					Metadata.GameInfo.EndTimeUtc = DateTime.UtcNow;
				Metadata.Write(writer);
			}

			preStartBuffer?.Dispose();
			writer.Close();
		}
	}
}
