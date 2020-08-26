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
using System.Runtime.ExceptionServices;
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	/// <summary>
	/// Creates a dedicated thread for the graphics device. An internal message queue is used to perform actions on the
	/// device. This allows calls to be enqueued to be processed asynchronously and thus free up the calling thread.
	/// </summary>
	sealed class ThreadedGraphicsContext : IGraphicsContext
	{
		// PERF: Maintain several object pools to reduce allocations.
		readonly Stack<Vertex[]> verticesPool = new Stack<Vertex[]>();
		readonly Stack<Message> messagePool = new Stack<Message>();
		readonly Queue<Message> messages = new Queue<Message>();

		public readonly int BatchSize;
		readonly object syncObject = new object();
		readonly Thread renderThread;
		volatile ExceptionDispatchInfo messageException;

		// Delegates that perform actions on the real device.
		Func<object> doClear;
		Action doClearDepthBuffer;
		Action doDisableDepthBuffer;
		Action doEnableDepthBuffer;
		Action doDisableScissor;
		Action doPresent;
		Func<string> getGLVersion;
		Func<ITexture> getCreateTexture;
		Func<object, IFrameBuffer> getCreateFrameBuffer;
		Func<object, IShader> getCreateShader;
		Func<object, IVertexBuffer<Vertex>> getCreateVertexBuffer;
		Action<object> doDrawPrimitives;
		Action<object> doEnableScissor;
		Action<object> doSetBlendMode;
		Action<object> doSetVSync;

		public ThreadedGraphicsContext(Sdl2GraphicsContext context, int batchSize)
		{
			BatchSize = batchSize;
			renderThread = new Thread(RenderThread)
			{
				Name = "ThreadedGraphicsContext RenderThread",
				IsBackground = true
			};
			lock (syncObject)
			{
				// Start and wait for the rendering thread to have initialized before returning.
				// Otherwise, the delegates may not have been set yet.
				renderThread.Start(context);
				Monitor.Wait(syncObject);
			}
		}

		void RenderThread(object contextObject)
		{
			using (var context = (Sdl2GraphicsContext)contextObject)
			{
				// This lock allows the constructor to block until initialization completes.
				lock (syncObject)
				{
					context.InitializeOpenGL();

					doClear = () => { context.Clear(); return null; };
					doClearDepthBuffer = () => context.ClearDepthBuffer();
					doDisableDepthBuffer = () => context.DisableDepthBuffer();
					doEnableDepthBuffer = () => context.EnableDepthBuffer();
					doDisableScissor = () => context.DisableScissor();
					doPresent = () => context.Present();
					getGLVersion = () => context.GLVersion;
					getCreateTexture = () => new ThreadedTexture(this, (ITextureInternal)context.CreateTexture());
					getCreateFrameBuffer =
						tuple =>
						{
							var t = (ValueTuple<Size, Color>)tuple;
							return new ThreadedFrameBuffer(this,
								context.CreateFrameBuffer(t.Item1, (ITextureInternal)CreateTexture(), t.Item2));
						};
					getCreateShader = name => new ThreadedShader(this, context.CreateShader((string)name));
					getCreateVertexBuffer = length => new ThreadedVertexBuffer(this, context.CreateVertexBuffer((int)length));
					doDrawPrimitives =
						 tuple =>
						 {
							 var t = (ValueTuple<PrimitiveType, int, int>)tuple;
							 context.DrawPrimitives(t.Item1, t.Item2, t.Item3);
						 };
					doEnableScissor =
						tuple =>
						{
							var t = (ValueTuple<int, int, int, int>)tuple;
							context.EnableScissor(t.Item1, t.Item2, t.Item3, t.Item4);
						};
					doSetBlendMode = mode => { context.SetBlendMode((BlendMode)mode); };
					doSetVSync = enabled => { context.SetVSyncEnabled((bool)enabled); };

					Monitor.Pulse(syncObject);
				}

				// Run a message loop.
				// Only this rendering thread can perform actions on the real device,
				// so other threads must send us a message which we process here.
				Message message;
				while (true)
				{
					lock (messages)
					{
						if (messages.Count == 0)
						{
							if (messageException != null)
								break;

							Monitor.Wait(messages);
						}

						message = messages.Dequeue();
					}

					if (message == null)
						break;

					message.Execute();
				}
			}
		}

		internal Vertex[] GetVertices(int size)
		{
			lock (verticesPool)
				if (size <= BatchSize && verticesPool.Count > 0)
					return verticesPool.Pop();

			return new Vertex[size < BatchSize ? BatchSize : size];
		}

		internal void ReturnVertices(Vertex[] vertices)
		{
			if (vertices.Length == BatchSize)
				lock (verticesPool)
					verticesPool.Push(vertices);
		}

		class Message
		{
			public Message(ThreadedGraphicsContext device)
			{
				this.device = device;
			}

			readonly AutoResetEvent completed = new AutoResetEvent(false);
			readonly ThreadedGraphicsContext device;
			volatile Action action;
			volatile Action<object> actionWithParam;
			volatile Func<object> func;
			volatile Func<object, object> funcWithParam;
			volatile object param;
			volatile object result;
			volatile ExceptionDispatchInfo edi;

			public void SetAction(Action method)
			{
				action = method;
			}

			public void SetAction(Action<object> method, object state)
			{
				actionWithParam = method;
				param = state;
			}

			public void SetAction(Func<object> method)
			{
				func = method;
			}

			public void SetAction(Func<object, object> method, object state)
			{
				funcWithParam = method;
				param = state;
			}

			public void Execute()
			{
				var wasSend = action != null || actionWithParam != null;
				try
				{
					if (action != null)
					{
						action();
						result = null;
						action = null;
					}
					else if (actionWithParam != null)
					{
						actionWithParam(param);
						result = null;
						actionWithParam = null;
						param = null;
					}
					else if (func != null)
					{
						result = func();
						func = null;
					}
					else
					{
						result = funcWithParam(param);
						funcWithParam = null;
						param = null;
					}
				}
				catch (Exception ex)
				{
					edi = ExceptionDispatchInfo.Capture(ex);

					if (wasSend)
						device.messageException = edi;

					result = null;
					param = null;
					action = null;
					actionWithParam = null;
					func = null;
					funcWithParam = null;
				}

				if (wasSend)
				{
					lock (device.messagePool)
						device.messagePool.Push(this);
				}
				else
				{
					completed.Set();
				}
			}

			public object Result()
			{
				completed.WaitOne();

				var localEdi = edi;
				edi = null;
				var localResult = result;
				result = null;

				localEdi?.Throw();
				return localResult;
			}
		}

		Message GetMessage()
		{
			lock (messagePool)
				if (messagePool.Count > 0)
					return messagePool.Pop();

			return new Message(this);
		}

		void QueueMessage(Message message)
		{
			var exception = messageException;
			exception?.Throw();

			lock (messages)
			{
				messages.Enqueue(message);
				if (messages.Count == 1)
					Monitor.Pulse(messages);
			}
		}

		object RunMessage(Message message)
		{
			QueueMessage(message);
			var result = message.Result();
			lock (messagePool)
				messagePool.Push(message);
			return result;
		}

		/// <summary>
		/// Sends a message to the rendering thread.
		/// This method blocks until the message is processed, and returns the result.
		/// </summary>
		public T Send<T>(Func<T> method) where T : class
		{
			if (renderThread == Thread.CurrentThread)
				return method();

			var message = GetMessage();
			message.SetAction(method);
			return (T)RunMessage(message);
		}

		/// <summary>
		/// Sends a message to the rendering thread.
		/// This method blocks until the message is processed, and returns the result.
		/// </summary>
		public T Send<T>(Func<object, T> method, object state) where T : class
		{
			if (renderThread == Thread.CurrentThread)
				return method(state);

			var message = GetMessage();
			message.SetAction(method, state);
			return (T)RunMessage(message);
		}

		/// <summary>
		/// Posts a message to the rendering thread.
		/// This method then returns immediately and does not wait for the message to be processed.
		/// </summary>
		public void Post(Action method)
		{
			if (renderThread == Thread.CurrentThread)
			{
				method();
				return;
			}

			var message = GetMessage();
			message.SetAction(method);
			QueueMessage(message);
		}

		/// <summary>
		/// Posts a message to the rendering thread.
		/// This method then returns immediately and does not wait for the message to be processed.
		/// </summary>
		public void Post(Action<object> method, object state)
		{
			if (renderThread == Thread.CurrentThread)
			{
				method(state);
				return;
			}

			var message = GetMessage();
			message.SetAction(method, state);
			QueueMessage(message);
		}

		public void Dispose()
		{
			// Use a null message to signal the rendering thread to clean up, then wait for it to complete.
			QueueMessage(null);
			renderThread.Join();
		}

		public string GLVersion
		{
			get
			{
				return Send(getGLVersion);
			}
		}

		public void Clear()
		{
			// We send the clear even though we could just post it.
			// This ensures all previous messages have been processed before we return.
			// This prevents us from queuing up work faster than it can be processed if rendering is behind.
			Send(doClear);
		}

		public void ClearDepthBuffer()
		{
			Post(doClearDepthBuffer);
		}

		public IFrameBuffer CreateFrameBuffer(Size s)
		{
			return Send(getCreateFrameBuffer, (s, Color.FromArgb(0)));
		}

		public IFrameBuffer CreateFrameBuffer(Size s, Color clearColor)
		{
			return Send(getCreateFrameBuffer, (s, clearColor));
		}

		public IShader CreateShader(string name)
		{
			return Send(getCreateShader, name);
		}

		public ITexture CreateTexture()
		{
			return Send(getCreateTexture);
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer(int length)
		{
			return Send(getCreateVertexBuffer, length);
		}

		public void DisableDepthBuffer()
		{
			Post(doDisableDepthBuffer);
		}

		public void DisableScissor()
		{
			Post(doDisableScissor);
		}

		public void DrawPrimitives(PrimitiveType type, int firstVertex, int numVertices)
		{
			Post(doDrawPrimitives, (type, firstVertex, numVertices));
		}

		public void EnableDepthBuffer()
		{
			Post(doEnableDepthBuffer);
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			Post(doEnableScissor, (left, top, width, height));
		}

		public void Present()
		{
			Post(doPresent);
		}

		public void SetBlendMode(BlendMode mode)
		{
			Post(doSetBlendMode, mode);
		}

		public void SetVSyncEnabled(bool enabled)
		{
			Post(doSetVSync, enabled);
		}
	}

	class ThreadedFrameBuffer : IFrameBuffer
	{
		readonly ThreadedGraphicsContext device;
		readonly Func<ITexture> getTexture;
		readonly Action bind;
		readonly Action unbind;
		readonly Action dispose;
		readonly Action<object> enableScissor;
		readonly Action disableScissor;

		public ThreadedFrameBuffer(ThreadedGraphicsContext device, IFrameBuffer frameBuffer)
		{
			this.device = device;
			getTexture = () => frameBuffer.Texture;
			bind = frameBuffer.Bind;
			unbind = frameBuffer.Unbind;
			dispose = frameBuffer.Dispose;

			enableScissor = rect => frameBuffer.EnableScissor((Rectangle)rect);
			disableScissor = frameBuffer.DisableScissor;
		}

		public ITexture Texture
		{
			get
			{
				return device.Send(getTexture);
			}
		}

		public void Bind()
		{
			device.Post(bind);
		}

		public void Unbind()
		{
			device.Post(unbind);
		}

		public void EnableScissor(Rectangle rect)
		{
			device.Post(enableScissor, rect);
		}

		public void DisableScissor()
		{
			device.Post(disableScissor);
		}

		public void Dispose()
		{
			device.Post(dispose);
		}
	}

	class ThreadedVertexBuffer : IVertexBuffer<Vertex>
	{
		readonly ThreadedGraphicsContext device;
		readonly Action bind;
		readonly Action<object> setData1;
		readonly Action<object> setData2;
		readonly Func<object, object> setData3;
		readonly Action dispose;

		public ThreadedVertexBuffer(ThreadedGraphicsContext device, IVertexBuffer<Vertex> vertexBuffer)
		{
			this.device = device;
			bind = vertexBuffer.Bind;
			setData1 = tuple => { var t = (ValueTuple<Vertex[], int>)tuple; vertexBuffer.SetData(t.Item1, t.Item2); device.ReturnVertices(t.Item1); };
			setData2 = tuple => { var t = (ValueTuple<Vertex[], int, int, int>)tuple; vertexBuffer.SetData(t.Item1, t.Item2, t.Item3, t.Item4); device.ReturnVertices(t.Item1); };
			setData3 = tuple => { setData2(tuple); return null; };
			dispose = vertexBuffer.Dispose;
		}

		public void Bind()
		{
			device.Post(bind);
		}

		public void SetData(Vertex[] vertices, int length)
		{
			var buffer = device.GetVertices(length);
			Array.Copy(vertices, buffer, length);
			device.Post(setData1, (buffer, length));
		}

		public void SetData(Vertex[] vertices, int offset, int start, int length)
		{
			if (length <= device.BatchSize)
			{
				// If we are able to use a buffer without allocation, post a message to avoid blocking.
				var buffer = device.GetVertices(length);
				Array.Copy(vertices, offset, buffer, 0, length);
				device.Post(setData2, (buffer, 0, start, length));
			}
			else
			{
				// If the length is too large for a buffer, send a message and block to avoid allocations.
				device.Send(setData3, (vertices, offset, start, length));
			}
		}

		public void Dispose()
		{
			device.Post(dispose);
		}
	}

	class ThreadedTexture : ITextureInternal
	{
		readonly ThreadedGraphicsContext device;
		readonly uint id;
		readonly Func<object> getScaleFilter;
		readonly Action<object> setScaleFilter;
		readonly Func<object> getSize;
		readonly Action<object> setEmpty;
		readonly Func<byte[]> getData;
		readonly Func<object, object> setData1;
		readonly Action<object> setData2;
		readonly Func<object, object> setData3;
		readonly Action dispose;

		public ThreadedTexture(ThreadedGraphicsContext device, ITextureInternal texture)
		{
			this.device = device;
			id = texture.ID;
			getScaleFilter = () => texture.ScaleFilter;
			setScaleFilter = value => texture.ScaleFilter = (TextureScaleFilter)value;
			getSize = () => texture.Size;
			setEmpty = tuple => { var t = (ValueTuple<int, int>)tuple; texture.SetEmpty(t.Item1, t.Item2); };
			getData = () => texture.GetData();
			setData1 = colors => { texture.SetData((uint[,])colors); return null; };
			setData2 = tuple => { var t = (ValueTuple<byte[], int, int>)tuple; texture.SetData(t.Item1, t.Item2, t.Item3); };
			setData3 = tuple => { setData2(tuple); return null; };
			dispose = texture.Dispose;
		}

		public uint ID
		{
			get
			{
				return id;
			}
		}

		public TextureScaleFilter ScaleFilter
		{
			get
			{
				return (TextureScaleFilter)device.Send(getScaleFilter);
			}

			set
			{
				device.Post(setScaleFilter, value);
			}
		}

		public Size Size
		{
			get
			{
				return (Size)device.Send(getSize);
			}
		}

		public void SetEmpty(int width, int height)
		{
			device.Post(setEmpty, (width, height));
		}

		public byte[] GetData()
		{
			return device.Send(getData);
		}

		public void SetData(uint[,] colors)
		{
			// We can't return until we are finished with the data, so we must Send here.
			device.Send(setData1, colors);
		}

		public void SetData(byte[] colors, int width, int height)
		{
			// Objects 85000 bytes or more will be directly allocated in the Large Object Heap (LOH).
			// https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap
			if (colors.Length < 85000)
			{
				// If we are able to create a small array the GC can collect easily, post a message to avoid blocking.
				var temp = new byte[colors.Length];
				Array.Copy(colors, temp, temp.Length);
				device.Post(setData2, (temp, width, height));
			}
			else
			{
				// If the length is large and would result in an array on the Large Object Heap (LOH),
				// send a message and block to avoid LOH allocation as this requires a Gen2 collection.
				device.Send(setData3, (colors, width, height));
			}
		}

		public void Dispose()
		{
			device.Post(dispose);
		}
	}

	class ThreadedShader : IShader
	{
		readonly ThreadedGraphicsContext device;
		readonly Action prepareRender;
		readonly Action<object> setBool;
		readonly Action<object> setMatrix;
		readonly Action<object> setTexture;
		readonly Action<object> setVec1;
		readonly Action<object> setVec2;
		readonly Action<object> setVec3;
		readonly Action<object> setVec4;

		public ThreadedShader(ThreadedGraphicsContext device, IShader shader)
		{
			this.device = device;
			prepareRender = shader.PrepareRender;
			setBool = tuple => { var t = (ValueTuple<string, bool>)tuple; shader.SetBool(t.Item1, t.Item2); };
			setMatrix = tuple => { var t = (ValueTuple<string, float[]>)tuple; shader.SetMatrix(t.Item1, t.Item2); };
			setTexture = tuple => { var t = (ValueTuple<string, ITexture>)tuple; shader.SetTexture(t.Item1, t.Item2); };
			setVec1 = tuple => { var t = (ValueTuple<string, float>)tuple; shader.SetVec(t.Item1, t.Item2); };
			setVec2 = tuple => { var t = (ValueTuple<string, float[], int>)tuple; shader.SetVec(t.Item1, t.Item2, t.Item3); };
			setVec3 = tuple => { var t = (ValueTuple<string, float, float>)tuple; shader.SetVec(t.Item1, t.Item2, t.Item3); };
			setVec4 = tuple => { var t = (ValueTuple<string, float, float, float>)tuple; shader.SetVec(t.Item1, t.Item2, t.Item3, t.Item4); };
		}

		public void PrepareRender()
		{
			device.Post(prepareRender);
		}

		public void SetBool(string name, bool value)
		{
			device.Post(setBool, (name, value));
		}

		public void SetMatrix(string param, float[] mtx)
		{
			device.Post(setMatrix, (param, mtx));
		}

		public void SetTexture(string param, ITexture texture)
		{
			device.Post(setTexture, (param, texture));
		}

		public void SetVec(string name, float x)
		{
			device.Post(setVec1, (name, x));
		}

		public void SetVec(string name, float[] vec, int length)
		{
			device.Post(setVec2, (name, vec, length));
		}

		public void SetVec(string name, float x, float y)
		{
			device.Post(setVec3, (name, x, y));
		}

		public void SetVec(string name, float x, float y, float z)
		{
			device.Post(setVec4, (name, x, y, z));
		}
	}
}
