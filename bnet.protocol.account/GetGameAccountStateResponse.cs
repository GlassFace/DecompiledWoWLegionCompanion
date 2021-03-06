using System;
using System.IO;

namespace bnet.protocol.account
{
	public class GetGameAccountStateResponse : IProtoBuf
	{
		public bool HasState;

		private GameAccountState _State;

		public bool HasTags;

		private GameAccountFieldTags _Tags;

		public GameAccountState State
		{
			get
			{
				return this._State;
			}
			set
			{
				this._State = value;
				this.HasState = (value != null);
			}
		}

		public GameAccountFieldTags Tags
		{
			get
			{
				return this._Tags;
			}
			set
			{
				this._Tags = value;
				this.HasTags = (value != null);
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
			GetGameAccountStateResponse.Deserialize(stream, this);
		}

		public static GetGameAccountStateResponse Deserialize(Stream stream, GetGameAccountStateResponse instance)
		{
			return GetGameAccountStateResponse.Deserialize(stream, instance, -1L);
		}

		public static GetGameAccountStateResponse DeserializeLengthDelimited(Stream stream)
		{
			GetGameAccountStateResponse getGameAccountStateResponse = new GetGameAccountStateResponse();
			GetGameAccountStateResponse.DeserializeLengthDelimited(stream, getGameAccountStateResponse);
			return getGameAccountStateResponse;
		}

		public static GetGameAccountStateResponse DeserializeLengthDelimited(Stream stream, GetGameAccountStateResponse instance)
		{
			long num = (long)((ulong)ProtocolParser.ReadUInt32(stream));
			num += stream.get_Position();
			return GetGameAccountStateResponse.Deserialize(stream, instance, num);
		}

		public static GetGameAccountStateResponse Deserialize(Stream stream, GetGameAccountStateResponse instance, long limit)
		{
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
						if (num2 != 18)
						{
							Key key = ProtocolParser.ReadKey((byte)num, stream);
							uint field = key.Field;
							if (field == 0u)
							{
								throw new ProtocolBufferException("Invalid field id: 0, something went wrong in the stream");
							}
							ProtocolParser.SkipKey(stream, key);
						}
						else if (instance.Tags == null)
						{
							instance.Tags = GameAccountFieldTags.DeserializeLengthDelimited(stream);
						}
						else
						{
							GameAccountFieldTags.DeserializeLengthDelimited(stream, instance.Tags);
						}
					}
					else if (instance.State == null)
					{
						instance.State = GameAccountState.DeserializeLengthDelimited(stream);
					}
					else
					{
						GameAccountState.DeserializeLengthDelimited(stream, instance.State);
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
			GetGameAccountStateResponse.Serialize(stream, this);
		}

		public static void Serialize(Stream stream, GetGameAccountStateResponse instance)
		{
			if (instance.HasState)
			{
				stream.WriteByte(10);
				ProtocolParser.WriteUInt32(stream, instance.State.GetSerializedSize());
				GameAccountState.Serialize(stream, instance.State);
			}
			if (instance.HasTags)
			{
				stream.WriteByte(18);
				ProtocolParser.WriteUInt32(stream, instance.Tags.GetSerializedSize());
				GameAccountFieldTags.Serialize(stream, instance.Tags);
			}
		}

		public uint GetSerializedSize()
		{
			uint num = 0u;
			if (this.HasState)
			{
				num += 1u;
				uint serializedSize = this.State.GetSerializedSize();
				num += serializedSize + ProtocolParser.SizeOfUInt32(serializedSize);
			}
			if (this.HasTags)
			{
				num += 1u;
				uint serializedSize2 = this.Tags.GetSerializedSize();
				num += serializedSize2 + ProtocolParser.SizeOfUInt32(serializedSize2);
			}
			return num;
		}

		public void SetState(GameAccountState val)
		{
			this.State = val;
		}

		public void SetTags(GameAccountFieldTags val)
		{
			this.Tags = val;
		}

		public override int GetHashCode()
		{
			int num = base.GetType().GetHashCode();
			if (this.HasState)
			{
				num ^= this.State.GetHashCode();
			}
			if (this.HasTags)
			{
				num ^= this.Tags.GetHashCode();
			}
			return num;
		}

		public override bool Equals(object obj)
		{
			GetGameAccountStateResponse getGameAccountStateResponse = obj as GetGameAccountStateResponse;
			return getGameAccountStateResponse != null && this.HasState == getGameAccountStateResponse.HasState && (!this.HasState || this.State.Equals(getGameAccountStateResponse.State)) && this.HasTags == getGameAccountStateResponse.HasTags && (!this.HasTags || this.Tags.Equals(getGameAccountStateResponse.Tags));
		}

		public static GetGameAccountStateResponse ParseFrom(byte[] bs)
		{
			return ProtobufUtil.ParseFrom<GetGameAccountStateResponse>(bs, 0, -1);
		}
	}
}
