using System;
using System.IO;

namespace bnet.protocol.connection
{
	public class EchoRequest : IProtoBuf
	{
		public bool HasTime;

		private ulong _Time;

		public bool HasNetworkOnly;

		private bool _NetworkOnly;

		public bool HasPayload;

		private byte[] _Payload;

		public ulong Time
		{
			get
			{
				return this._Time;
			}
			set
			{
				this._Time = value;
				this.HasTime = true;
			}
		}

		public bool NetworkOnly
		{
			get
			{
				return this._NetworkOnly;
			}
			set
			{
				this._NetworkOnly = value;
				this.HasNetworkOnly = true;
			}
		}

		public byte[] Payload
		{
			get
			{
				return this._Payload;
			}
			set
			{
				this._Payload = value;
				this.HasPayload = (value != null);
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
			EchoRequest.Deserialize(stream, this);
		}

		public static EchoRequest Deserialize(Stream stream, EchoRequest instance)
		{
			return EchoRequest.Deserialize(stream, instance, -1L);
		}

		public static EchoRequest DeserializeLengthDelimited(Stream stream)
		{
			EchoRequest echoRequest = new EchoRequest();
			EchoRequest.DeserializeLengthDelimited(stream, echoRequest);
			return echoRequest;
		}

		public static EchoRequest DeserializeLengthDelimited(Stream stream, EchoRequest instance)
		{
			long num = (long)((ulong)ProtocolParser.ReadUInt32(stream));
			num += stream.get_Position();
			return EchoRequest.Deserialize(stream, instance, num);
		}

		public static EchoRequest Deserialize(Stream stream, EchoRequest instance, long limit)
		{
			BinaryReader binaryReader = new BinaryReader(stream);
			instance.NetworkOnly = false;
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
					if (num2 != 9)
					{
						if (num2 != 16)
						{
							if (num2 != 26)
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
								instance.Payload = ProtocolParser.ReadBytes(stream);
							}
						}
						else
						{
							instance.NetworkOnly = ProtocolParser.ReadBool(stream);
						}
					}
					else
					{
						instance.Time = binaryReader.ReadUInt64();
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
			EchoRequest.Serialize(stream, this);
		}

		public static void Serialize(Stream stream, EchoRequest instance)
		{
			BinaryWriter binaryWriter = new BinaryWriter(stream);
			if (instance.HasTime)
			{
				stream.WriteByte(9);
				binaryWriter.Write(instance.Time);
			}
			if (instance.HasNetworkOnly)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteBool(stream, instance.NetworkOnly);
			}
			if (instance.HasPayload)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteBytes(stream, instance.Payload);
			}
		}

		public uint GetSerializedSize()
		{
			uint num = 0u;
			if (this.HasTime)
			{
				num += 1u;
				num += 8u;
			}
			if (this.HasNetworkOnly)
			{
				num += 1u;
				num += 1u;
			}
			if (this.HasPayload)
			{
				num += 1u;
				num += ProtocolParser.SizeOfUInt32(this.Payload.Length) + (uint)this.Payload.Length;
			}
			return num;
		}

		public void SetTime(ulong val)
		{
			this.Time = val;
		}

		public void SetNetworkOnly(bool val)
		{
			this.NetworkOnly = val;
		}

		public void SetPayload(byte[] val)
		{
			this.Payload = val;
		}

		public override int GetHashCode()
		{
			int num = base.GetType().GetHashCode();
			if (this.HasTime)
			{
				num ^= this.Time.GetHashCode();
			}
			if (this.HasNetworkOnly)
			{
				num ^= this.NetworkOnly.GetHashCode();
			}
			if (this.HasPayload)
			{
				num ^= this.Payload.GetHashCode();
			}
			return num;
		}

		public override bool Equals(object obj)
		{
			EchoRequest echoRequest = obj as EchoRequest;
			return echoRequest != null && this.HasTime == echoRequest.HasTime && (!this.HasTime || this.Time.Equals(echoRequest.Time)) && this.HasNetworkOnly == echoRequest.HasNetworkOnly && (!this.HasNetworkOnly || this.NetworkOnly.Equals(echoRequest.NetworkOnly)) && this.HasPayload == echoRequest.HasPayload && (!this.HasPayload || this.Payload.Equals(echoRequest.Payload));
		}

		public static EchoRequest ParseFrom(byte[] bs)
		{
			return ProtobufUtil.ParseFrom<EchoRequest>(bs, 0, -1);
		}
	}
}
