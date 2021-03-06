using System;
using System.Collections.Generic;
using System.IO;

namespace bnet.protocol.connection
{
	public class ConnectionMeteringContentHandles : IProtoBuf
	{
		private List<ContentHandle> _ContentHandle = new List<ContentHandle>();

		public List<ContentHandle> ContentHandle
		{
			get
			{
				return this._ContentHandle;
			}
			set
			{
				this._ContentHandle = value;
			}
		}

		public List<ContentHandle> ContentHandleList
		{
			get
			{
				return this._ContentHandle;
			}
		}

		public int ContentHandleCount
		{
			get
			{
				return this._ContentHandle.get_Count();
			}
		}

		public bool IsInitialized
		{
			get
			{
				return true;
			}
		}

		public void Deserialize(Stream stream)
		{
			ConnectionMeteringContentHandles.Deserialize(stream, this);
		}

		public static ConnectionMeteringContentHandles Deserialize(Stream stream, ConnectionMeteringContentHandles instance)
		{
			return ConnectionMeteringContentHandles.Deserialize(stream, instance, -1L);
		}

		public static ConnectionMeteringContentHandles DeserializeLengthDelimited(Stream stream)
		{
			ConnectionMeteringContentHandles connectionMeteringContentHandles = new ConnectionMeteringContentHandles();
			ConnectionMeteringContentHandles.DeserializeLengthDelimited(stream, connectionMeteringContentHandles);
			return connectionMeteringContentHandles;
		}

		public static ConnectionMeteringContentHandles DeserializeLengthDelimited(Stream stream, ConnectionMeteringContentHandles instance)
		{
			long num = (long)((ulong)ProtocolParser.ReadUInt32(stream));
			num += stream.get_Position();
			return ConnectionMeteringContentHandles.Deserialize(stream, instance, num);
		}

		public static ConnectionMeteringContentHandles Deserialize(Stream stream, ConnectionMeteringContentHandles instance, long limit)
		{
			if (instance.ContentHandle == null)
			{
				instance.ContentHandle = new List<ContentHandle>();
			}
			while (limit < 0L || stream.get_Position() < limit)
			{
				int num = stream.ReadByte();
				if (num == -1)
				{
					if (limit >= 0L)
					{
						throw new EndOfStreamException();
					}
					return instance;
				}
				else
				{
					int num2 = num;
					if (num2 != 10)
					{
						Key key = ProtocolParser.ReadKey((byte)num, stream);
						uint field = key.Field;
						if (field == 0u)
						{
							throw new ProtocolBufferException("Invalid field id: 0, something went wrong in the stream");
						}
						ProtocolParser.SkipKey(stream, key);
					}
					else
					{
						instance.ContentHandle.Add(bnet.protocol.ContentHandle.DeserializeLengthDelimited(stream));
					}
				}
			}
			if (stream.get_Position() == limit)
			{
				return instance;
			}
			throw new ProtocolBufferException("Read past max limit");
		}

		public void Serialize(Stream stream)
		{
			ConnectionMeteringContentHandles.Serialize(stream, this);
		}

		public static void Serialize(Stream stream, ConnectionMeteringContentHandles instance)
		{
			if (instance.ContentHandle.get_Count() > 0)
			{
				using (List<ContentHandle>.Enumerator enumerator = instance.ContentHandle.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ContentHandle current = enumerator.get_Current();
						stream.WriteByte(10);
						ProtocolParser.WriteUInt32(stream, current.GetSerializedSize());
						bnet.protocol.ContentHandle.Serialize(stream, current);
					}
				}
			}
		}

		public uint GetSerializedSize()
		{
			uint num = 0u;
			if (this.ContentHandle.get_Count() > 0)
			{
				using (List<ContentHandle>.Enumerator enumerator = this.ContentHandle.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ContentHandle current = enumerator.get_Current();
						num += 1u;
						uint serializedSize = current.GetSerializedSize();
						num += serializedSize + ProtocolParser.SizeOfUInt32(serializedSize);
					}
				}
			}
			return num;
		}

		public void AddContentHandle(ContentHandle val)
		{
			this._ContentHandle.Add(val);
		}

		public void ClearContentHandle()
		{
			this._ContentHandle.Clear();
		}

		public void SetContentHandle(List<ContentHandle> val)
		{
			this.ContentHandle = val;
		}

		public override int GetHashCode()
		{
			int num = base.GetType().GetHashCode();
			using (List<ContentHandle>.Enumerator enumerator = this.ContentHandle.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ContentHandle current = enumerator.get_Current();
					num ^= current.GetHashCode();
				}
			}
			return num;
		}

		public override bool Equals(object obj)
		{
			ConnectionMeteringContentHandles connectionMeteringContentHandles = obj as ConnectionMeteringContentHandles;
			if (connectionMeteringContentHandles == null)
			{
				return false;
			}
			if (this.ContentHandle.get_Count() != connectionMeteringContentHandles.ContentHandle.get_Count())
			{
				return false;
			}
			for (int i = 0; i < this.ContentHandle.get_Count(); i++)
			{
				if (!this.ContentHandle.get_Item(i).Equals(connectionMeteringContentHandles.ContentHandle.get_Item(i)))
				{
					return false;
				}
			}
			return true;
		}

		public static ConnectionMeteringContentHandles ParseFrom(byte[] bs)
		{
			return ProtobufUtil.ParseFrom<ConnectionMeteringContentHandles>(bs, 0, -1);
		}
	}
}
