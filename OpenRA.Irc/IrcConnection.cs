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
using System.IO;
using System.Net.Sockets;

namespace OpenRA.Irc
{
	public class IrcConnection : IDisposable
	{
		TcpClient socket;
		Stream stream;
		StreamWriter writer;
		StreamReader reader;
		bool disposed;

		public void Connect(string hostname, int port, int connectionTimeout)
		{
			CheckDisposed();
			if (socket != null && socket.Connected)
				throw new InvalidOperationException("Socket already connected");

			socket = new TcpClient(hostname, port);
			socket.ReceiveTimeout = socket.SendTimeout = connectionTimeout;
			stream = socket.GetStream();
			writer = new StreamWriter(stream) { AutoFlush = true };
			reader = new StreamReader(stream);
		}

		public void WriteLine(string format, params object[] args)
		{
			CheckDisposed();
			writer.WriteLine(format, args);
		}

		public string ReadLine()
		{
			CheckDisposed();
			return reader.ReadLine();
		}

		public void Close()
		{
			CloseImpl();
			GC.SuppressFinalize(this);
		}

		void CloseImpl()
		{
			if (disposed) return;

			disposed = true;
			if (socket != null) socket.Close();
			if (stream != null) stream.Close();
			if (writer != null) writer.Close();
			if (reader != null) reader.Close();
		}

		void IDisposable.Dispose()
		{
			Close();
		}

		~IrcConnection()
		{
			CloseImpl();
		}

		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(GetType().FullName);
		}
	}
}
