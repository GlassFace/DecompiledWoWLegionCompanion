using System;
using System.Collections;
using System.Collections.Generic;

namespace bgs
{
	public class Map<TKey, TValue> : IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>>
	{
		public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<TKey, TValue>>
		{
			private Map<TKey, TValue> dictionary;

			private int next;

			private int stamp;

			internal KeyValuePair<TKey, TValue> current;

			object IEnumerator.Current
			{
				get
				{
					this.VerifyCurrent();
					return this.current;
				}
			}

			public KeyValuePair<TKey, TValue> Current
			{
				get
				{
					return this.current;
				}
			}

			internal TKey CurrentKey
			{
				get
				{
					this.VerifyCurrent();
					return this.current.get_Key();
				}
			}

			internal TValue CurrentValue
			{
				get
				{
					this.VerifyCurrent();
					return this.current.get_Value();
				}
			}

			internal Enumerator(Map<TKey, TValue> dictionary)
			{
				this.dictionary = dictionary;
				this.stamp = dictionary.generation;
			}

			void IEnumerator.Reset()
			{
				this.Reset();
			}

			public bool MoveNext()
			{
				this.VerifyState();
				if (this.next < 0)
				{
					return false;
				}
				while (this.next < this.dictionary.touchedSlots)
				{
					int num = this.next++;
					if ((this.dictionary.linkSlots[num].HashCode & -2147483648) != 0)
					{
						this.current = new KeyValuePair<TKey, TValue>(this.dictionary.keySlots[num], this.dictionary.valueSlots[num]);
						return true;
					}
				}
				this.next = -1;
				return false;
			}

			internal void Reset()
			{
				this.VerifyState();
				this.next = 0;
			}

			private void VerifyState()
			{
				if (this.dictionary == null)
				{
					throw new ObjectDisposedException(null);
				}
				if (this.dictionary.generation != this.stamp)
				{
					throw new InvalidOperationException("out of sync");
				}
			}

			private void VerifyCurrent()
			{
				this.VerifyState();
				if (this.next <= 0)
				{
					throw new InvalidOperationException("Current is not valid");
				}
			}

			public void Dispose()
			{
				this.dictionary = null;
			}
		}

		public sealed class KeyCollection : IEnumerable, ICollection, ICollection<TKey>, IEnumerable<TKey>
		{
			public struct Enumerator : IDisposable, IEnumerator, IEnumerator<TKey>
			{
				private Map<TKey, TValue>.Enumerator host_enumerator;

				object IEnumerator.Current
				{
					get
					{
						return this.host_enumerator.CurrentKey;
					}
				}

				public TKey Current
				{
					get
					{
						return this.host_enumerator.current.get_Key();
					}
				}

				internal Enumerator(Map<TKey, TValue> host)
				{
					this.host_enumerator = host.GetEnumerator();
				}

				void IEnumerator.Reset()
				{
					this.host_enumerator.Reset();
				}

				public void Dispose()
				{
					this.host_enumerator.Dispose();
				}

				public bool MoveNext()
				{
					return this.host_enumerator.MoveNext();
				}
			}

			private Map<TKey, TValue> dictionary;

			bool ICollection<TKey>.IsReadOnly
			{
				get
				{
					return true;
				}
			}

			bool ICollection.IsSynchronized
			{
				get
				{
					return false;
				}
			}

			object ICollection.SyncRoot
			{
				get
				{
					return ((ICollection)this.dictionary).get_SyncRoot();
				}
			}

			public int Count
			{
				get
				{
					return this.dictionary.Count;
				}
			}

			public KeyCollection(Map<TKey, TValue> dictionary)
			{
				if (dictionary == null)
				{
					throw new ArgumentNullException("dictionary");
				}
				this.dictionary = dictionary;
			}

			void ICollection<TKey>.Add(TKey item)
			{
				throw new NotSupportedException("this is a read-only collection");
			}

			void ICollection<TKey>.Clear()
			{
				throw new NotSupportedException("this is a read-only collection");
			}

			bool ICollection<TKey>.Contains(TKey item)
			{
				return this.dictionary.ContainsKey(item);
			}

			bool ICollection<TKey>.Remove(TKey item)
			{
				throw new NotSupportedException("this is a read-only collection");
			}

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			void ICollection.CopyTo(Array array, int index)
			{
				TKey[] array2 = array as TKey[];
				if (array2 != null)
				{
					this.CopyTo(array2, index);
					return;
				}
				this.dictionary.CopyToCheck(array, index);
				this.dictionary.Do_ICollectionCopyTo<TKey>(array, index, new Map<TKey, TValue>.Transform<TKey>(Map<TKey, TValue>.pick_key));
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			public void CopyTo(TKey[] array, int index)
			{
				this.dictionary.CopyToCheck(array, index);
				this.dictionary.CopyKeys(array, index);
			}

			public Map<TKey, TValue>.KeyCollection.Enumerator GetEnumerator()
			{
				return new Map<TKey, TValue>.KeyCollection.Enumerator(this.dictionary);
			}
		}

		public sealed class ValueCollection : IEnumerable, ICollection, ICollection<TValue>, IEnumerable<TValue>
		{
			public struct Enumerator : IDisposable, IEnumerator, IEnumerator<TValue>
			{
				private Map<TKey, TValue>.Enumerator host_enumerator;

				object IEnumerator.Current
				{
					get
					{
						return this.host_enumerator.CurrentValue;
					}
				}

				public TValue Current
				{
					get
					{
						return this.host_enumerator.current.get_Value();
					}
				}

				internal Enumerator(Map<TKey, TValue> host)
				{
					this.host_enumerator = host.GetEnumerator();
				}

				void IEnumerator.Reset()
				{
					this.host_enumerator.Reset();
				}

				public void Dispose()
				{
					this.host_enumerator.Dispose();
				}

				public bool MoveNext()
				{
					return this.host_enumerator.MoveNext();
				}
			}

			private Map<TKey, TValue> dictionary;

			bool ICollection<TValue>.IsReadOnly
			{
				get
				{
					return true;
				}
			}

			bool ICollection.IsSynchronized
			{
				get
				{
					return false;
				}
			}

			object ICollection.SyncRoot
			{
				get
				{
					return ((ICollection)this.dictionary).get_SyncRoot();
				}
			}

			public int Count
			{
				get
				{
					return this.dictionary.Count;
				}
			}

			public ValueCollection(Map<TKey, TValue> dictionary)
			{
				if (dictionary == null)
				{
					throw new ArgumentNullException("dictionary");
				}
				this.dictionary = dictionary;
			}

			void ICollection<TValue>.Add(TValue item)
			{
				throw new NotSupportedException("this is a read-only collection");
			}

			void ICollection<TValue>.Clear()
			{
				throw new NotSupportedException("this is a read-only collection");
			}

			bool ICollection<TValue>.Contains(TValue item)
			{
				return this.dictionary.ContainsValue(item);
			}

			bool ICollection<TValue>.Remove(TValue item)
			{
				throw new NotSupportedException("this is a read-only collection");
			}

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			void ICollection.CopyTo(Array array, int index)
			{
				TValue[] array2 = array as TValue[];
				if (array2 != null)
				{
					this.CopyTo(array2, index);
					return;
				}
				this.dictionary.CopyToCheck(array, index);
				this.dictionary.Do_ICollectionCopyTo<TValue>(array, index, new Map<TKey, TValue>.Transform<TValue>(Map<TKey, TValue>.pick_value));
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			public void CopyTo(TValue[] array, int index)
			{
				this.dictionary.CopyToCheck(array, index);
				this.dictionary.CopyValues(array, index);
			}

			public Map<TKey, TValue>.ValueCollection.Enumerator GetEnumerator()
			{
				return new Map<TKey, TValue>.ValueCollection.Enumerator(this.dictionary);
			}
		}

		private delegate TRet Transform<TRet>(TKey key, TValue value);

		private const int INITIAL_SIZE = 4;

		private const float DEFAULT_LOAD_FACTOR = 0.9f;

		private const int NO_SLOT = -1;

		private const int HASH_FLAG = -2147483648;

		private int[] table;

		private Link[] linkSlots;

		private TKey[] keySlots;

		private TValue[] valueSlots;

		private IEqualityComparer<TKey> hcp;

		private int touchedSlots;

		private int emptySlot;

		private int count;

		private int threshold;

		private int generation;

		public int Count
		{
			get
			{
				return this.count;
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException("key");
				}
				int num = this.hcp.GetHashCode(key) | -2147483648;
				for (int num2 = this.table[(num & 2147483647) % this.table.Length] - 1; num2 != -1; num2 = this.linkSlots[num2].Next)
				{
					if (this.linkSlots[num2].HashCode == num && this.hcp.Equals(this.keySlots[num2], key))
					{
						return this.valueSlots[num2];
					}
				}
				throw new KeyNotFoundException();
			}
			set
			{
				if (key == null)
				{
					throw new ArgumentNullException("key");
				}
				int num = this.hcp.GetHashCode(key) | -2147483648;
				int num2 = (num & 2147483647) % this.table.Length;
				int num3 = this.table[num2] - 1;
				int num4 = -1;
				if (num3 != -1)
				{
					while (this.linkSlots[num3].HashCode != num || !this.hcp.Equals(this.keySlots[num3], key))
					{
						num4 = num3;
						num3 = this.linkSlots[num3].Next;
						if (num3 == -1)
						{
							break;
						}
					}
				}
				if (num3 == -1)
				{
					if (++this.count > this.threshold)
					{
						this.Resize();
						num2 = (num & 2147483647) % this.table.Length;
					}
					num3 = this.emptySlot;
					if (num3 == -1)
					{
						num3 = this.touchedSlots++;
					}
					else
					{
						this.emptySlot = this.linkSlots[num3].Next;
					}
					this.linkSlots[num3].Next = this.table[num2] - 1;
					this.table[num2] = num3 + 1;
					this.linkSlots[num3].HashCode = num;
					this.keySlots[num3] = key;
				}
				else if (num4 != -1)
				{
					this.linkSlots[num4].Next = this.linkSlots[num3].Next;
					this.linkSlots[num3].Next = this.table[num2] - 1;
					this.table[num2] = num3 + 1;
				}
				this.valueSlots[num3] = value;
				this.generation++;
			}
		}

		public Map<TKey, TValue>.KeyCollection Keys
		{
			get
			{
				return new Map<TKey, TValue>.KeyCollection(this);
			}
		}

		public Map<TKey, TValue>.ValueCollection Values
		{
			get
			{
				return new Map<TKey, TValue>.ValueCollection(this);
			}
		}

		public Map()
		{
			this.Init(4, null);
		}

		public Map(int count)
		{
			this.Init(count, null);
		}

		public Map(IEqualityComparer<TKey> comparer)
		{
			this.Init(4, comparer);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Map<TKey, TValue>.Enumerator(this);
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return new Map<TKey, TValue>.Enumerator(this);
		}

		private void Init(int capacity, IEqualityComparer<TKey> hcp)
		{
			this.hcp = (hcp ?? EqualityComparer<TKey>.get_Default());
			capacity = Math.Max(1, (int)((float)capacity / 0.9f));
			this.InitArrays(capacity);
		}

		private void InitArrays(int size)
		{
			this.table = new int[size];
			this.linkSlots = new Link[size];
			this.emptySlot = -1;
			this.keySlots = new TKey[size];
			this.valueSlots = new TValue[size];
			this.touchedSlots = 0;
			this.threshold = (int)((float)this.table.Length * 0.9f);
			if (this.threshold == 0 && this.table.Length > 0)
			{
				this.threshold = 1;
			}
		}

		private void CopyToCheck(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (index > array.get_Length())
			{
				throw new ArgumentException("index larger than largest valid index of array");
			}
			if (array.get_Length() - index < this.Count)
			{
				throw new ArgumentException("Destination array cannot hold the requested elements!");
			}
		}

		private void CopyKeys(TKey[] array, int index)
		{
			for (int i = 0; i < this.touchedSlots; i++)
			{
				if ((this.linkSlots[i].HashCode & -2147483648) != 0)
				{
					array[index++] = this.keySlots[i];
				}
			}
		}

		private void CopyValues(TValue[] array, int index)
		{
			for (int i = 0; i < this.touchedSlots; i++)
			{
				if ((this.linkSlots[i].HashCode & -2147483648) != 0)
				{
					array[index++] = this.valueSlots[i];
				}
			}
		}

		private static KeyValuePair<TKey, TValue> make_pair(TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue>(key, value);
		}

		private static TKey pick_key(TKey key, TValue value)
		{
			return key;
		}

		private static TValue pick_value(TKey key, TValue value)
		{
			return value;
		}

		private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			this.CopyToCheck(array, index);
			for (int i = 0; i < this.touchedSlots; i++)
			{
				if ((this.linkSlots[i].HashCode & -2147483648) != 0)
				{
					array[index++] = new KeyValuePair<TKey, TValue>(this.keySlots[i], this.valueSlots[i]);
				}
			}
		}

		private void Do_ICollectionCopyTo<TRet>(Array array, int index, Map<TKey, TValue>.Transform<TRet> transform)
		{
			Type typeFromHandle = typeof(TRet);
			Type elementType = array.GetType().GetElementType();
			try
			{
				if ((typeFromHandle.get_IsPrimitive() || elementType.get_IsPrimitive()) && !elementType.IsAssignableFrom(typeFromHandle))
				{
					throw new Exception();
				}
				object[] array2 = (object[])array;
				for (int i = 0; i < this.touchedSlots; i++)
				{
					if ((this.linkSlots[i].HashCode & -2147483648) != 0)
					{
						array2[index++] = transform(this.keySlots[i], this.valueSlots[i]);
					}
				}
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Cannot copy source collection elements to destination array", "array", ex);
			}
		}

		private void Resize()
		{
			int num = HashPrimeNumbers.ToPrime(this.table.Length << 1 | 1);
			int[] array = new int[num];
			Link[] array2 = new Link[num];
			for (int i = 0; i < this.table.Length; i++)
			{
				for (int num2 = this.table[i] - 1; num2 != -1; num2 = this.linkSlots[num2].Next)
				{
					int num3 = array2[num2].HashCode = (this.hcp.GetHashCode(this.keySlots[num2]) | -2147483648);
					int num4 = (num3 & 2147483647) % num;
					array2[num2].Next = array[num4] - 1;
					array[num4] = num2 + 1;
				}
			}
			this.table = array;
			this.linkSlots = array2;
			TKey[] array3 = new TKey[num];
			TValue[] array4 = new TValue[num];
			Array.Copy(this.keySlots, 0, array3, 0, this.touchedSlots);
			Array.Copy(this.valueSlots, 0, array4, 0, this.touchedSlots);
			this.keySlots = array3;
			this.valueSlots = array4;
			this.threshold = (int)((float)num * 0.9f);
		}

		public void Add(TKey key, TValue value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = this.hcp.GetHashCode(key) | -2147483648;
			int num2 = (num & 2147483647) % this.table.Length;
			int num3;
			for (num3 = this.table[num2] - 1; num3 != -1; num3 = this.linkSlots[num3].Next)
			{
				if (this.linkSlots[num3].HashCode == num && this.hcp.Equals(this.keySlots[num3], key))
				{
					throw new ArgumentException("An element with the same key already exists in the dictionary.");
				}
			}
			if (++this.count > this.threshold)
			{
				this.Resize();
				num2 = (num & 2147483647) % this.table.Length;
			}
			num3 = this.emptySlot;
			if (num3 == -1)
			{
				num3 = this.touchedSlots++;
			}
			else
			{
				this.emptySlot = this.linkSlots[num3].Next;
			}
			this.linkSlots[num3].HashCode = num;
			this.linkSlots[num3].Next = this.table[num2] - 1;
			this.table[num2] = num3 + 1;
			this.keySlots[num3] = key;
			this.valueSlots[num3] = value;
			this.generation++;
		}

		public void Clear()
		{
			if (this.count == 0)
			{
				return;
			}
			this.count = 0;
			Array.Clear(this.table, 0, this.table.Length);
			Array.Clear(this.keySlots, 0, this.keySlots.Length);
			Array.Clear(this.valueSlots, 0, this.valueSlots.Length);
			Array.Clear(this.linkSlots, 0, this.linkSlots.Length);
			this.emptySlot = -1;
			this.touchedSlots = 0;
			this.generation++;
		}

		public bool ContainsKey(TKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = this.hcp.GetHashCode(key) | -2147483648;
			for (int num2 = this.table[(num & 2147483647) % this.table.Length] - 1; num2 != -1; num2 = this.linkSlots[num2].Next)
			{
				if (this.linkSlots[num2].HashCode == num && this.hcp.Equals(this.keySlots[num2], key))
				{
					return true;
				}
			}
			return false;
		}

		public bool ContainsValue(TValue value)
		{
			IEqualityComparer<TValue> @default = EqualityComparer<TValue>.get_Default();
			for (int i = 0; i < this.table.Length; i++)
			{
				for (int num = this.table[i] - 1; num != -1; num = this.linkSlots[num].Next)
				{
					if (@default.Equals(this.valueSlots[num], value))
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool Remove(TKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = this.hcp.GetHashCode(key) | -2147483648;
			int num2 = (num & 2147483647) % this.table.Length;
			int num3 = this.table[num2] - 1;
			if (num3 == -1)
			{
				return false;
			}
			int num4 = -1;
			while (this.linkSlots[num3].HashCode != num || !this.hcp.Equals(this.keySlots[num3], key))
			{
				num4 = num3;
				num3 = this.linkSlots[num3].Next;
				if (num3 == -1)
				{
					IL_A4:
					if (num3 == -1)
					{
						return false;
					}
					this.count--;
					if (num4 == -1)
					{
						this.table[num2] = this.linkSlots[num3].Next + 1;
					}
					else
					{
						this.linkSlots[num4].Next = this.linkSlots[num3].Next;
					}
					this.linkSlots[num3].Next = this.emptySlot;
					this.emptySlot = num3;
					this.linkSlots[num3].HashCode = 0;
					this.keySlots[num3] = default(TKey);
					this.valueSlots[num3] = default(TValue);
					this.generation++;
					return true;
				}
			}
			goto IL_A4;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = this.hcp.GetHashCode(key) | -2147483648;
			for (int num2 = this.table[(num & 2147483647) % this.table.Length] - 1; num2 != -1; num2 = this.linkSlots[num2].Next)
			{
				if (this.linkSlots[num2].HashCode == num && this.hcp.Equals(this.keySlots[num2], key))
				{
					value = this.valueSlots[num2];
					return true;
				}
			}
			value = default(TValue);
			return false;
		}

		public Map<TKey, TValue>.Enumerator GetEnumerator()
		{
			return new Map<TKey, TValue>.Enumerator(this);
		}
	}
}
