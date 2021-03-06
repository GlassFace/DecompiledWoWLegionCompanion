using System;
using System.Collections.Generic;
using System.IO;

namespace bnet.protocol.game_master
{
	public class FindGameRequest : IProtoBuf
	{
		private List<Player> _Player = new List<Player>();

		public bool HasFactoryId;

		private ulong _FactoryId;

		public bool HasProperties;

		private GameProperties _Properties;

		public bool HasObjectId;

		private ulong _ObjectId;

		public bool HasRequestId;

		private ulong _RequestId;

		public bool HasAdvancedNotification;

		private bool _AdvancedNotification;

		public List<Player> Player
		{
			get
			{
				return this._Player;
			}
			set
			{
				this._Player = value;
			}
		}

		public List<Player> PlayerList
		{
			get
			{
				return this._Player;
			}
		}

		public int PlayerCount
		{
			get
			{
				return this._Player.get_Count();
			}
		}

		public ulong FactoryId
		{
			get
			{
				return this._FactoryId;
			}
			set
			{
				this._FactoryId = value;
				this.HasFactoryId = true;
			}
		}

		public GameProperties Properties
		{
			get
			{
				return this._Properties;
			}
			set
			{
				this._Properties = value;
				this.HasProperties = (value != null);
			}
		}

		public ulong ObjectId
		{
			get
			{
				return this._ObjectId;
			}
			set
			{
				this._ObjectId = value;
				this.HasObjectId = true;
			}
		}

		public ulong RequestId
		{
			get
			{
				return this._RequestId;
			}
			set
			{
				this._RequestId = value;
				this.HasRequestId = true;
			}
		}

		public bool AdvancedNotification
		{
			get
			{
				return this._AdvancedNotification;
			}
			set
			{
				this._AdvancedNotification = value;
				this.HasAdvancedNotification = true;
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
			FindGameRequest.Deserialize(stream, this);
		}

		public static FindGameRequest Deserialize(Stream stream, FindGameRequest instance)
		{
			return FindGameRequest.Deserialize(stream, instance, -1L);
		}

		public static FindGameRequest DeserializeLengthDelimited(Stream stream)
		{
			FindGameRequest findGameRequest = new FindGameRequest();
			FindGameRequest.DeserializeLengthDelimited(stream, findGameRequest);
			return findGameRequest;
		}

		public static FindGameRequest DeserializeLengthDelimited(Stream stream, FindGameRequest instance)
		{
			long num = (long)((ulong)ProtocolParser.ReadUInt32(stream));
			num += stream.get_Position();
			return FindGameRequest.Deserialize(stream, instance, num);
		}

		public static FindGameRequest Deserialize(Stream stream, FindGameRequest instance, long limit)
		{
			BinaryReader binaryReader = new BinaryReader(stream);
			if (instance.Player == null)
			{
				instance.Player = new List<Player>();
			}
			instance.AdvancedNotification = false;
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
						if (num2 != 17)
						{
							if (num2 != 26)
							{
								if (num2 != 32)
								{
									if (num2 != 41)
									{
										if (num2 != 48)
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
											instance.AdvancedNotification = ProtocolParser.ReadBool(stream);
										}
									}
									else
									{
										instance.RequestId = binaryReader.ReadUInt64();
									}
								}
								else
								{
									instance.ObjectId = ProtocolParser.ReadUInt64(stream);
								}
							}
							else if (instance.Properties == null)
							{
								instance.Properties = GameProperties.DeserializeLengthDelimited(stream);
							}
							else
							{
								GameProperties.DeserializeLengthDelimited(stream, instance.Properties);
							}
						}
						else
						{
							instance.FactoryId = binaryReader.ReadUInt64();
						}
					}
					else
					{
						instance.Player.Add(bnet.protocol.game_master.Player.DeserializeLengthDelimited(stream));
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
			FindGameRequest.Serialize(stream, this);
		}

		public static void Serialize(Stream stream, FindGameRequest instance)
		{
			BinaryWriter binaryWriter = new BinaryWriter(stream);
			if (instance.Player.get_Count() > 0)
			{
				using (List<Player>.Enumerator enumerator = instance.Player.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Player current = enumerator.get_Current();
						stream.WriteByte(10);
						ProtocolParser.WriteUInt32(stream, current.GetSerializedSize());
						bnet.protocol.game_master.Player.Serialize(stream, current);
					}
				}
			}
			if (instance.HasFactoryId)
			{
				stream.WriteByte(17);
				binaryWriter.Write(instance.FactoryId);
			}
			if (instance.HasProperties)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteUInt32(stream, instance.Properties.GetSerializedSize());
				GameProperties.Serialize(stream, instance.Properties);
			}
			if (instance.HasObjectId)
			{
				stream.WriteByte(32);
				ProtocolParser.WriteUInt64(stream, instance.ObjectId);
			}
			if (instance.HasRequestId)
			{
				stream.WriteByte(41);
				binaryWriter.Write(instance.RequestId);
			}
			if (instance.HasAdvancedNotification)
			{
				stream.WriteByte(48);
				ProtocolParser.WriteBool(stream, instance.AdvancedNotification);
			}
		}

		public uint GetSerializedSize()
		{
			uint num = 0u;
			if (this.Player.get_Count() > 0)
			{
				using (List<Player>.Enumerator enumerator = this.Player.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Player current = enumerator.get_Current();
						num += 1u;
						uint serializedSize = current.GetSerializedSize();
						num += serializedSize + ProtocolParser.SizeOfUInt32(serializedSize);
					}
				}
			}
			if (this.HasFactoryId)
			{
				num += 1u;
				num += 8u;
			}
			if (this.HasProperties)
			{
				num += 1u;
				uint serializedSize2 = this.Properties.GetSerializedSize();
				num += serializedSize2 + ProtocolParser.SizeOfUInt32(serializedSize2);
			}
			if (this.HasObjectId)
			{
				num += 1u;
				num += ProtocolParser.SizeOfUInt64(this.ObjectId);
			}
			if (this.HasRequestId)
			{
				num += 1u;
				num += 8u;
			}
			if (this.HasAdvancedNotification)
			{
				num += 1u;
				num += 1u;
			}
			return num;
		}

		public void AddPlayer(Player val)
		{
			this._Player.Add(val);
		}

		public void ClearPlayer()
		{
			this._Player.Clear();
		}

		public void SetPlayer(List<Player> val)
		{
			this.Player = val;
		}

		public void SetFactoryId(ulong val)
		{
			this.FactoryId = val;
		}

		public void SetProperties(GameProperties val)
		{
			this.Properties = val;
		}

		public void SetObjectId(ulong val)
		{
			this.ObjectId = val;
		}

		public void SetRequestId(ulong val)
		{
			this.RequestId = val;
		}

		public void SetAdvancedNotification(bool val)
		{
			this.AdvancedNotification = val;
		}

		public override int GetHashCode()
		{
			int num = base.GetType().GetHashCode();
			using (List<Player>.Enumerator enumerator = this.Player.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Player current = enumerator.get_Current();
					num ^= current.GetHashCode();
				}
			}
			if (this.HasFactoryId)
			{
				num ^= this.FactoryId.GetHashCode();
			}
			if (this.HasProperties)
			{
				num ^= this.Properties.GetHashCode();
			}
			if (this.HasObjectId)
			{
				num ^= this.ObjectId.GetHashCode();
			}
			if (this.HasRequestId)
			{
				num ^= this.RequestId.GetHashCode();
			}
			if (this.HasAdvancedNotification)
			{
				num ^= this.AdvancedNotification.GetHashCode();
			}
			return num;
		}

		public override bool Equals(object obj)
		{
			FindGameRequest findGameRequest = obj as FindGameRequest;
			if (findGameRequest == null)
			{
				return false;
			}
			if (this.Player.get_Count() != findGameRequest.Player.get_Count())
			{
				return false;
			}
			for (int i = 0; i < this.Player.get_Count(); i++)
			{
				if (!this.Player.get_Item(i).Equals(findGameRequest.Player.get_Item(i)))
				{
					return false;
				}
			}
			return this.HasFactoryId == findGameRequest.HasFactoryId && (!this.HasFactoryId || this.FactoryId.Equals(findGameRequest.FactoryId)) && this.HasProperties == findGameRequest.HasProperties && (!this.HasProperties || this.Properties.Equals(findGameRequest.Properties)) && this.HasObjectId == findGameRequest.HasObjectId && (!this.HasObjectId || this.ObjectId.Equals(findGameRequest.ObjectId)) && this.HasRequestId == findGameRequest.HasRequestId && (!this.HasRequestId || this.RequestId.Equals(findGameRequest.RequestId)) && this.HasAdvancedNotification == findGameRequest.HasAdvancedNotification && (!this.HasAdvancedNotification || this.AdvancedNotification.Equals(findGameRequest.AdvancedNotification));
		}

		public static FindGameRequest ParseFrom(byte[] bs)
		{
			return ProtobufUtil.ParseFrom<FindGameRequest>(bs, 0, -1);
		}
	}
}
