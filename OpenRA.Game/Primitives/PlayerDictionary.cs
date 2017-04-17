using System;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	/// <summary>
	/// Provides a mapping of players to values, as well as fast lookup by player index.
	/// </summary>
	public struct PlayerDictionary<T> : IReadOnlyList<T>, IReadOnlyDictionary<Player, T> where T : class
	{
		readonly T[] valueByPlayerIndex;
		readonly Dictionary<Player, T> valueByPlayer;

		public PlayerDictionary(World world, Func<Player, int, T> valueFactory)
		{
			var players = world.Players;
			if (players.Length == 0)
				throw new InvalidOperationException("The players in the world have not yet been set.");

			// The world players never change, so we can safely maintain an array of values.
			// We need to enforce T is a class, so if it changes that change is available in both collections.
			valueByPlayerIndex = new T[players.Length];
			valueByPlayer = new Dictionary<Player, T>(players.Length);
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];
				var state = valueFactory(player, i);
				valueByPlayerIndex[i] = state;
				valueByPlayer[player] = state;
			}
		}

		public PlayerDictionary(World world, Func<Player, T> valueFactory)
			: this(world, (player, _) => valueFactory(player))
		{ }

		/// <summary>Gets the value for the specified player. This is slower than specifying a player index.</summary>
		public T this[Player player] { get { return valueByPlayer[player]; } }

		/// <summary>Gets the value for the specified player index.</summary>
		public T this[int playerIndex] { get { return valueByPlayerIndex[playerIndex]; } }

		public int Count { get { return valueByPlayerIndex.Length; } }
		public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)valueByPlayerIndex).GetEnumerator(); }

		ICollection<Player> IReadOnlyDictionary<Player, T>.Keys { get { return valueByPlayer.Keys; } }
		ICollection<T> IReadOnlyDictionary<Player, T>.Values { get { return valueByPlayer.Values; } }
		bool IReadOnlyDictionary<Player, T>.ContainsKey(Player key) { return valueByPlayer.ContainsKey(key); }
		bool IReadOnlyDictionary<Player, T>.TryGetValue(Player key, out T value) { return valueByPlayer.TryGetValue(key, out value); }
		IEnumerator<KeyValuePair<Player, T>> IEnumerable<KeyValuePair<Player, T>>.GetEnumerator() { return valueByPlayer.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
