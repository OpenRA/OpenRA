#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OpenRA.Mods.Common
{
	// N3: Config & persistence types
	public enum CotEndpointMode
	{
		Localhost,
		Unicast,
		Multicast,
	}

	public sealed class CotOutputConfig
	{
		public CotEndpointMode Mode { get; set; } = CotEndpointMode.Localhost;
		public string Host { get; set; } = "127.0.0.1";
		public int Port { get; set; } = 4242;
		public int? MulticastTtl { get; set; } = 1;
		public string BindInterfaceName { get; set; }
		public bool Remember { get; set; } = true;
	}

	// N2: Transport layer + background sender. Config/persistence will come in N3.
	public static class CotOutputService
	{
		const int DefaultQueueCapacity = 256;

		static readonly object Sync = new();
		static Channel<byte[]> channel;
		static CancellationTokenSource cts;
		static Task pumpTask;
		static ICotTransport transport;
		static bool started;

		public static void EnsureInitializedFrom(string host, int port)
		{
			if (started)
				return;

			lock (Sync)
			{
				if (started)
					return;

				// Prefer remembered config if present and marked Remember=true
				var cfg = TryLoadConfig();
				if (cfg != null && cfg.Remember)
				{
					ConfigureAndStart(cfg, persist: false);
					return;
				}

				// Fallback to provided host/port with inferred mode
				var ip = ResolveIPv4(host);
				var inferredMode = IsLoopback(ip) ? CotEndpointMode.Localhost : (IsMulticast(ip) ? CotEndpointMode.Multicast : CotEndpointMode.Unicast);
				var fallbackCfg = new CotOutputConfig
				{
					Mode = inferredMode,
					Host = host,
					Port = port,
					MulticastTtl = 1,
					Remember = false,
				};
				ConfigureAndStart(fallbackCfg, persist: false);
			}
		}

		public static bool Enqueue(byte[] payload)
		{
			if (payload == null || payload.Length == 0)
				return false;

			if (!started)
				EnsureInitializedFrom("127.0.0.1", 4242);

			try
			{
				var ok = channel.Writer.TryWrite(payload);
				if (!ok)
				{
					// Drop-oldest policy: TryWrite may fail if writer is completed; log once per occurrence.
					Log.Write("cot", "backpressure drop-oldest (queue full)");
				}
				return ok;
			}
			catch (Exception e)
			{
				Log.Write("cot", e);
				return false;
			}
		}

		public static void Dispose()
		{
			lock (Sync)
			{
				if (!started)
					return;

				try { channel.Writer.TryComplete(); } catch { }
				try { cts.Cancel(); } catch { }
				try { pumpTask?.Wait(1000); } catch { }
				try { transport?.Dispose(); } catch { }
				try { Game.OnQuit -= Dispose; } catch { }

				channel = null;
				cts = null;
				pumpTask = null;
				transport = null;
				started = false;
			}
		}

		static void Start(ICotTransport t, string mode, string host, int port)
		{
			transport = t;
			var opts = new BoundedChannelOptions(DefaultQueueCapacity)
			{
				SingleReader = true,
				SingleWriter = false,
				FullMode = BoundedChannelFullMode.DropOldest,
			};
			channel = Channel.CreateBounded<byte[]>(opts);
			cts = new CancellationTokenSource();
			pumpTask = Task.Run(() => PumpAsync(cts.Token));
			started = true;

			Log.Write("cot", string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"init mode={0} host={1} port={2}", mode, host, port));

			// Ensure cleanup on exit
			Game.OnQuit += Dispose;
		}

		public static void ConfigureAndStart(CotOutputConfig cfg, bool persist)
		{
			if (cfg == null)
				throw new ArgumentNullException(nameof(cfg));

			lock (Sync)
			{
				// Allow reconfigure/restart even if a fallback already started the service
				if (started)
				{
					try { channel?.Writer.TryComplete(); } catch { }
					try { cts?.Cancel(); } catch { }
					try { pumpTask?.Wait(1000); } catch { }
					try { transport?.Dispose(); } catch { }
					try { Game.OnQuit -= Dispose; } catch { }

					channel = null;
					cts = null;
					pumpTask = null;
					transport = null;
					started = false;
				}

				var tuple = CreateTransportFromConfig(cfg);
				Start(tuple.transport, tuple.mode, tuple.host, tuple.port);
				if (persist && cfg.Remember)
					TrySaveConfig(cfg);
			}
		}

		static (ICotTransport transport, string mode, string host, int port) CreateTransportFromConfig(CotOutputConfig cfg)
		{
			var host = string.IsNullOrWhiteSpace(cfg.Host) ? "127.0.0.1" : cfg.Host;
			var port = cfg.Port <= 0 ? 4242 : cfg.Port;

			switch (cfg.Mode)
			{
				case CotEndpointMode.Localhost:
				{
					var ip = IPAddress.Loopback;
					var t = new UdpUnicastTransport(new IPEndPoint(ip, port));
					return (t, "Localhost", "127.0.0.1", port);
				}
				case CotEndpointMode.Multicast:
				{
					var ip = ResolveIPv4(host);
					var ttl = cfg.MulticastTtl.GetValueOrDefault(1);
					var t = new UdpMulticastTransport(new IPEndPoint(ip, port), ttl: ttl, loopback: false);
					return (t, "Multicast", host, port);
				}
				case CotEndpointMode.Unicast:
				default:
				{
					var ip = ResolveIPv4(host);
					var t = new UdpUnicastTransport(new IPEndPoint(ip, port));
					var mode = IsLoopback(ip) ? "Localhost" : "Unicast";
					return (t, mode, host, port);
				}
			}
		}

		static string GetConfigPath()
		{
			try
			{
				string baseDir;
				if (OperatingSystem.IsWindows())
					baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenRA");
				else
					baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".openra");
				Directory.CreateDirectory(baseDir);
				return Path.Combine(baseDir, "cot-output.json");
			}
			catch
			{
				return null;
			}
		}

		static CotOutputConfig TryLoadConfig()
		{
			try
			{
				var path = GetConfigPath();
				if (string.IsNullOrEmpty(path) || !File.Exists(path))
					return null;
				var json = File.ReadAllText(path);
				var options = new JsonSerializerOptions
				{
					ReadCommentHandling = JsonCommentHandling.Skip,
					AllowTrailingCommas = true,
				};
				options.Converters.Add(new JsonStringEnumConverter());
				return JsonSerializer.Deserialize<CotOutputConfig>(json, options);
			}
			catch (Exception e)
			{
				Log.Write("cot", e);
				return null;
			}
		}

		static void TrySaveConfig(CotOutputConfig cfg)
		{
			try
			{
				var path = GetConfigPath();
				if (string.IsNullOrEmpty(path))
					return;
				var options = new JsonSerializerOptions { WriteIndented = true };
				options.Converters.Add(new JsonStringEnumConverter());
				var json = JsonSerializer.Serialize(cfg, options);
				File.WriteAllText(path, json);
			}
			catch (Exception e)
			{
				Log.Write("cot", e);
			}
		}

		static async Task PumpAsync(CancellationToken token)
		{
			try
			{
				var reader = channel.Reader;
				while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
				{
					while (reader.TryRead(out var payload))
					{
						try
						{
							await transport.SendAsync(payload, token).ConfigureAwait(false);
						}
						catch (Exception e)
						{
							// Non-fatal: drop this packet, continue
							Log.Write("cot", e);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Normal shutdown
			}
			catch (Exception e)
			{
				Log.Write("cot", e);
			}
		}

		static IPAddress ResolveIPv4(string host)
		{
			if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
				return IPAddress.Loopback;
			if (IPAddress.TryParse(host, out var ip))
				return ip.AddressFamily == AddressFamily.InterNetwork ? ip : IPAddress.Loopback;
			var addrs = Dns.GetHostAddresses(host);
			var ipv4 = Array.Find(addrs, a => a.AddressFamily == AddressFamily.InterNetwork);
			return ipv4 ?? IPAddress.Loopback;
		}

		static bool IsMulticast(IPAddress ip)
		{
			var b = ip.GetAddressBytes();
			return ip.AddressFamily == AddressFamily.InterNetwork && b[0] >= 224 && b[0] <= 239;
		}

		static bool IsLoopback(IPAddress ip) => IPAddress.IsLoopback(ip);
	}

	interface ICotTransport : IDisposable
	{
		ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default);
	}

	sealed class UdpUnicastTransport : ICotTransport
	{
		readonly UdpClient udp;

		public UdpUnicastTransport(IPEndPoint endpoint)
		{
			udp = new UdpClient();
			udp.Connect(endpoint);
		}

		public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
		{
			// UdpClient requires byte[]; copy is acceptable for small messages
			return new ValueTask(udp.SendAsync(payload.ToArray(), payload.Length));
		}

		public void Dispose()
		{
			udp.Dispose();
		}
	}

	sealed class UdpMulticastTransport : ICotTransport
	{
		readonly UdpClient udp;
		readonly IPEndPoint group;

		public UdpMulticastTransport(IPEndPoint groupEndpoint, int ttl = 1, bool loopback = false)
		{
			if (!IsIPv4Multicast(groupEndpoint.Address))
				throw new ArgumentException("Group must be IPv4 multicast (224.0.0.0/4)", nameof(groupEndpoint));

			group = groupEndpoint;
			udp = new UdpClient(AddressFamily.InterNetwork);

			// TTL and loopback control
			udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);
			udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, loopback);
		}

		public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
		{
			return new ValueTask(udp.SendAsync(payload.ToArray(), payload.Length, group));
		}

		public void Dispose()
		{
			udp.Dispose();
		}

		static bool IsIPv4Multicast(IPAddress ip)
		{
			var b = ip.GetAddressBytes();
			return ip.AddressFamily == AddressFamily.InterNetwork && b[0] >= 224 && b[0] <= 239;
		}
	}
}
