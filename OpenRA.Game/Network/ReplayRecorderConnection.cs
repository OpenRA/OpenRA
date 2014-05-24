#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Network
{
	class ReplayRecorderConnection : IConnection, IDisposable
	{
		public ReplayMetadata Metadata;

		IConnection inner;
		BinaryWriter writer;
		Func<string> chooseFilename;
		MemoryStream preStartBuffer = new MemoryStream();

		public ReplayRecorderConnection(IConnection inner, Func<string> chooseFilename)
		{
			this.chooseFilename = chooseFilename;
			this.inner = inner;

			writer = new BinaryWriter(preStartBuffer);
		}

		void StartSavingReplay(byte[] initialContent)
		{
			var filename = chooseFilename();
			var mod = Game.modData.Manifest.Mod;
			var dir = new[] { Platform.SupportDir, "Replays", mod.Id, mod.Version }.Aggregate(Path.Combine);

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			FileStream file = null;
			var id = -1;
			while (file == null)
			{
				var fullFilename = Path.Combine(dir, id < 0 ? "{0}.rep".F(filename) : "{0}-{1}.rep".F(filename, id));
				id++;
				try
				{
					file = File.Create(fullFilename);
				}
				catch (IOException) { }
			}

			file.Write(initialContent);
			this.writer = new BinaryWriter(file);
		}

		public int LocalClientId { get { return inner.LocalClientId; } }
		public ConnectionState ConnectionState { get { return inner.ConnectionState; } }

		public void Send(int frame, List<byte[]> orders) { inner.Send(frame, orders); }
		public void SendImmediate(List<byte[]> orders) { inner.SendImmediate(orders); }
		public void SendSync(int frame, byte[] syncData) { inner.SendSync(frame, syncData); }

		public void Receive(Action<int, byte[]> packetFn)
		{
			inner.Receive((client, data) =>
				{
					if (preStartBuffer != null && IsGameStart(data))
					{
						writer.Flush();
						var preStartData = preStartBuffer.ToArray();
						preStartBuffer = null;
						StartSavingReplay(preStartData);
					}

					writer.Write(client);
					writer.Write(data.Length);
					writer.Write(data);
					packetFn(client, data);
				});
		}

		bool IsGameStart(byte[] data)
		{
			if (data.Length == 5 && data[4] == 0xbf)
				return false;
			if (data.Length >= 5 && data[4] == 0x65)
				return false;

			var frame = BitConverter.ToInt32(data, 0);
			return frame == 0 && data.ToOrderList(null).Any(o => o.OrderString == "StartGame");
		}

		bool disposed;
		protected void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				if (Metadata != null)
				{
					if (Metadata.GameInfo != null)
						Metadata.GameInfo.EndTimeUtc = DateTime.UtcNow;
					Metadata.Write(writer);
				}
				writer.Close();
				inner.Dispose();
			}

			disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
