#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Threading;
using OpenRA.Graphics;

namespace OpenRA.Platforms.Default
{
	/// <summary>
	/// Creates a dedicated thread for the graphics device. An internal message queue is used to perform actions on the
	/// device. This allows calls to be enqueued to be processed asynchronously and thus free up the calling thread.
	/// </summary>
	sealed class ThreadedGraphicsDevice : IGraphicsDevice
	{
		// PERF: Maintain several object pools to reduce allocations.
		readonly Stack<Vertex[]> verticesPool = new Stack<Vertex[]>();
		readonly Stack<Message> messagePool = new Stack<Message>();
		readonly Queue<Message> messages = new Queue<Message>();

		readonly object syncObject = new object();
		readonly Thread renderThread;
		readonly int batchSize;
		volatile ExceptionDispatchInfo messageException;

		// Delegates that perform actions on the real device.
		Func<object> doClear;
		Action doClearDepthBuffer;
		Action doDisableDepthBuffer;
		Action doEnableDepthBuffer;
		Action doDisableScissor;
		Action doPresent;
		Action doGrabWindowMouseFocus;
		Action doReleaseWindowMouseFocus;
		Func<object> getWindowSize;
		Func<object> getWindowScale;
		Func<string> getGLVersion;
		Func<ITexture> getCreateTexture;
		Func<object, ITexture> getCreateTextureBitmap;
		Func<Bitmap> getTakeScreenshot;
		Func<string> getGetClipboardText;
		Action<object> doSetHardwareCursor;
		Func<object, IFrameBuffer> getCreateFrameBuffer;
		Func<object, IShader> getCreateShader;
		Func<object, IVertexBuffer<Vertex>> getCreateVertexBuffer;
		Action<object> doDrawPrimitives;
		Action<object> doEnableScissor;
		Action<object> doPumpInput;
		Action<object> doSetBlendMode;
		Func<object, IHardwareCursor> getCreateHardwareCursor;
		Func<object, object> getSetClipboardText;

		public ThreadedGraphicsDevice(IGraphicsDeviceInternal device, int batchSize)
		{
			this.batchSize = batchSize;
			renderThread = new Thread(RenderThread)
			{
				Name = "ThreadedGraphicsDevice RenderThread",
				IsBackground = true
			};
			renderThread.SetApartmentState(ApartmentState.STA);
			lock (syncObject)
			{
				// Start and wait for the rendering thread to have initialized before returning.
				// Otherwise, the delegates may not have been set yet.
				renderThread.Start(device);
				Monitor.Wait(syncObject);
			}
		}

		void RenderThread(object deviceObject)
		{
			using (var device = (IGraphicsDeviceInternal)deviceObject)
			{
				// This lock allows the constructor to block until initialization completes.
				lock (syncObject)
				{
					device.InitializeOpenGL();

					doClear = () => { device.Clear(); return null; };
					doClearDepthBuffer = () => device.ClearDepthBuffer();
					doDisableDepthBuffer = () => device.DisableDepthBuffer();
					doEnableDepthBuffer = () => device.EnableDepthBuffer();
					doDisableScissor = () => device.DisableScissor();
					doPresent = () => device.Present();
					doGrabWindowMouseFocus = () => device.GrabWindowMouseFocus();
					doReleaseWindowMouseFocus = () => device.ReleaseWindowMouseFocus();
					getWindowSize = () => device.WindowSize;
					getWindowScale = () => device.WindowScale;
					device.OnWindowScaleChanged += (before, after) =>
					{
						var onWindowScaleChanged = OnWindowScaleChanged;
						if (onWindowScaleChanged != null)
							System.Threading.Tasks.Task.Run(() => onWindowScaleChanged(before, after));
					};
					getGLVersion = () => device.GLVersion;
					getCreateTexture = () => new ThreadedTexture(this, (ITextureInternal)device.CreateTexture());
					getCreateTextureBitmap = bitmap => new ThreadedTexture(this, (ITextureInternal)device.CreateTexture((Bitmap)bitmap));
					getTakeScreenshot = () => device.TakeScreenshot();
					getGetClipboardText = () => device.GetClipboardText();
					doSetHardwareCursor = cursor => { device.SetHardwareCursor((IHardwareCursor)cursor); };
					getCreateFrameBuffer = s => new ThreadedFrameBuffer(this, device.CreateFrameBuffer((Size)s, (ITextureInternal)CreateTexture()));
					getCreateShader = name => new ThreadedShader(this, device.CreateShader((string)name));
					getCreateVertexBuffer = length => new ThreadedVertexBuffer(this, device.CreateVertexBuffer((int)length));
					doDrawPrimitives =
						 tuple =>
						 {
							 var t = (Tuple<PrimitiveType, int, int>)tuple;
							 device.DrawPrimitives(t.Item1, t.Item2, t.Item3);
						 };
					doEnableScissor =
						tuple =>
						{
							var t = (Tuple<int, int, int, int>)tuple;
							device.EnableScissor(t.Item1, t.Item2, t.Item3, t.Item4);
						};
					doPumpInput = inputHandler => { device.PumpInput((IInputHandler)inputHandler); };
					doSetBlendMode = mode => { device.SetBlendMode((BlendMode)mode); };
					getCreateHardwareCursor =
						tuple =>
						{
							var t = (Tuple<string, Size, byte[], int2>)tuple;
							return device.CreateHardwareCursor(t.Item1, t.Item2, t.Item3, t.Item4);
						};
					getSetClipboardText = text => device.SetClipboardText((string)text);

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
				if (size <= batchSize && verticesPool.Count > 0)
					return verticesPool.Pop();

			return new Vertex[size < batchSize ? batchSize : size];
		}

		internal void ReturnVertices(Vertex[] vertices)
		{
			if (vertices.Length == batchSize)
				lock (verticesPool)
					verticesPool.Push(vertices);
		}

		class Message
		{
			public Message(ThreadedGraphicsDevice device)
			{
				this.device = device;
			}

			readonly AutoResetEvent completed = new AutoResetEvent(false);
			readonly ThreadedGraphicsDevice device;
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

				if (localEdi != null)
					localEdi.Throw();
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
			if (exception != null)
				exception.Throw();

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

		public Size WindowSize
		{
			get
			{
				return (Size)Send(getWindowSize);
			}
		}

		public float WindowScale
		{
			get
			{
				return (float)Send(getWindowScale);
			}
		}

		public event Action<float, float> OnWindowScaleChanged;

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
			return Send(getCreateFrameBuffer, s);
		}

		public IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot)
		{
			return Send(getCreateHardwareCursor, Tuple.Create(name, size, data, hotspot));
		}

		public IShader CreateShader(string name)
		{
			return Send(getCreateShader, name);
		}

		public ITexture CreateTexture()
		{
			return Send(getCreateTexture);
		}

		public ITexture CreateTexture(Bitmap bitmap)
		{
			return Send(getCreateTextureBitmap, bitmap);
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
			Post(doDrawPrimitives, Tuple.Create(type, firstVertex, numVertices));
		}

		public void EnableDepthBuffer()
		{
			Post(doEnableDepthBuffer);
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			Post(doEnableScissor, Tuple.Create(left, top, width, height));
		}

		public string GetClipboardText()
		{
			return Send(getGetClipboardText);
		}

		public void GrabWindowMouseFocus()
		{
			Post(doGrabWindowMouseFocus);
		}

		public void Present()
		{
			Post(doPresent);
		}

		public void PumpInput(IInputHandler inputHandler)
		{
			Post(doPumpInput, inputHandler);
		}

		public void ReleaseWindowMouseFocus()
		{
			Post(doReleaseWindowMouseFocus);
		}

		public void SetBlendMode(BlendMode mode)
		{
			Post(doSetBlendMode, mode);
		}

		public bool SetClipboardText(string text)
		{
			return (bool)Send(getSetClipboardText, text);
		}

		public void SetHardwareCursor(IHardwareCursor cursor)
		{
			Post(doSetHardwareCursor, cursor);
		}

		public Bitmap TakeScreenshot()
		{
			return Send(getTakeScreenshot);
		}
	}

	class ThreadedFrameBuffer : IFrameBuffer
	{
		readonly ThreadedGraphicsDevice device;
		readonly Func<ITexture> getTexture;
		readonly Action bind;
		readonly Action unbind;
		readonly Action dispose;

		public ThreadedFrameBuffer(ThreadedGraphicsDevice device, IFrameBuffer frameBuffer)
		{
			this.device = device;
			getTexture = () => frameBuffer.Texture;
			bind = frameBuffer.Bind;
			unbind = frameBuffer.Unbind;
			dispose = frameBuffer.Dispose;
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

		public void Dispose()
		{
			device.Post(dispose);
		}
	}

	class ThreadedVertexBuffer : IVertexBuffer<Vertex>
	{
		readonly ThreadedGraphicsDevice device;
		readonly Action bind;
		readonly Action<object> setData1;
		readonly Func<object, object> setData2;
		readonly Action dispose;

		public ThreadedVertexBuffer(ThreadedGraphicsDevice device, IVertexBuffer<Vertex> vertexBuffer)
		{
			this.device = device;
			bind = vertexBuffer.Bind;
			setData1 = tuple => { var t = (Tuple<Vertex[], int>)tuple; vertexBuffer.SetData(t.Item1, t.Item2); device.ReturnVertices(t.Item1); };
			setData2 = tuple => { var t = (Tuple<IntPtr, int, int>)tuple; vertexBuffer.SetData(t.Item1, t.Item2, t.Item3); return null; };
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
			device.Post(setData1, Tuple.Create(buffer, length));
		}

		public void SetData(IntPtr data, int start, int length)
		{
			// We can't return until we are finished with the data, so we must Send here.
			device.Send(setData2, Tuple.Create(data, start, length));
		}

		public void SetData(Vertex[] vertices, int start, int length)
		{
			var buffer = device.GetVertices(length);
			Array.Copy(vertices, start, buffer, 0, length);
			device.Post(setData1, Tuple.Create(buffer, length));
		}

		public void Dispose()
		{
			device.Post(dispose);
		}
	}

	class ThreadedTexture : ITextureInternal
	{
		readonly ThreadedGraphicsDevice device;
		readonly uint id;
		readonly Func<object> getScaleFilter;
		readonly Action<object> setScaleFilter;
		readonly Func<object> getSize;
		readonly Action<object> setEmpty;
		readonly Func<byte[]> getData;
		readonly Func<object, object> setData1;
		readonly Func<object, object> setData2;
		readonly Action<object> setData3;
		readonly Action dispose;

		public ThreadedTexture(ThreadedGraphicsDevice device, ITextureInternal texture)
		{
			this.device = device;
			id = texture.ID;
			getScaleFilter = () => texture.ScaleFilter;
			setScaleFilter = value => texture.ScaleFilter = (TextureScaleFilter)value;
			getSize = () => texture.Size;
			setEmpty = tuple => { var t = (Tuple<int, int>)tuple; texture.SetEmpty(t.Item1, t.Item2); };
			getData = () => texture.GetData();
			setData1 = colors => { texture.SetData((uint[,])colors); return null; };
			setData2 = bitmap => { texture.SetData((Bitmap)bitmap); return null; };
			setData3 = tuple => { var t = (Tuple<byte[], int, int>)tuple; texture.SetData(t.Item1, t.Item2, t.Item3); };
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
			device.Post(setEmpty, Tuple.Create(width, height));
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

		public void SetData(Bitmap bitmap)
		{
			// We can't return until we are finished with the data, so we must Send here.
			device.Send(setData2, bitmap);
		}

		public void SetData(byte[] colors, int width, int height)
		{
			// This creates some garbage for the GC to clean up,
			// but allows us post a message instead of blocking the message queue by sending it.
			var temp = new byte[colors.Length];
			Array.Copy(colors, temp, temp.Length);
			device.Post(setData3, Tuple.Create(temp, width, height));
		}

		public void Dispose()
		{
			device.Post(dispose);
		}
	}

	class ThreadedShader : IShader
	{
		readonly ThreadedGraphicsDevice device;
		readonly Action prepareRender;
		readonly Action<object> setBool;
		readonly Action<object> setMatrix;
		readonly Action<object> setTexture;
		readonly Action<object> setVec1;
		readonly Action<object> setVec2;
		readonly Action<object> setVec3;
		readonly Action<object> setVec4;

		public ThreadedShader(ThreadedGraphicsDevice device, IShader shader)
		{
			this.device = device;
			prepareRender = shader.PrepareRender;
			setBool = tuple => { var t = (Tuple<string, bool>)tuple; shader.SetBool(t.Item1, t.Item2); };
			setMatrix = tuple => { var t = (Tuple<string, float[]>)tuple; shader.SetMatrix(t.Item1, t.Item2); };
			setTexture = tuple => { var t = (Tuple<string, ITexture>)tuple; shader.SetTexture(t.Item1, t.Item2); };
			setVec1 = tuple => { var t = (Tuple<string, float>)tuple; shader.SetVec(t.Item1, t.Item2); };
			setVec2 = tuple => { var t = (Tuple<string, float[], int>)tuple; shader.SetVec(t.Item1, t.Item2, t.Item3); };
			setVec3 = tuple => { var t = (Tuple<string, float, float>)tuple; shader.SetVec(t.Item1, t.Item2, t.Item3); };
			setVec4 = tuple => { var t = (Tuple<string, float, float, float>)tuple; shader.SetVec(t.Item1, t.Item2, t.Item3, t.Item4); };
		}

		public void PrepareRender()
		{
			device.Post(prepareRender);
		}

		public void SetBool(string name, bool value)
		{
			device.Post(setBool, Tuple.Create(name, value));
		}

		public void SetMatrix(string param, float[] mtx)
		{
			device.Post(setMatrix, Tuple.Create(param, mtx));
		}

		public void SetTexture(string param, ITexture texture)
		{
			device.Post(setTexture, Tuple.Create(param, texture));
		}

		public void SetVec(string name, float x)
		{
			device.Post(setVec1, Tuple.Create(name, x));
		}

		public void SetVec(string name, float[] vec, int length)
		{
			device.Post(setVec2, Tuple.Create(name, vec, length));
		}

		public void SetVec(string name, float x, float y)
		{
			device.Post(setVec3, Tuple.Create(name, x, y));
		}

		public void SetVec(string name, float x, float y, float z)
		{
			device.Post(setVec4, Tuple.Create(name, x, y, z));
		}
	}
}