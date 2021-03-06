using System;
using System.Collections.Generic;
using System.IO;

namespace bnet.protocol
{
	public class Header : IProtoBuf
	{
		public bool HasMethodId;

		private uint _MethodId;

		public bool HasObjectId;

		private ulong _ObjectId;

		public bool HasSize;

		private uint _Size;

		public bool HasStatus;

		private uint _Status;

		private List<ErrorInfo> _Error = new List<ErrorInfo>();

		public bool HasTimeout;

		private ulong _Timeout;

		public uint ServiceId
		{
			get;
			set;
		}

		public uint MethodId
		{
			get
			{
				return this._MethodId;
			}
			set
			{
				this._MethodId = value;
				this.HasMethodId = true;
			}
		}

		public uint Token
		{
			get;
			set;
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

		public uint Size
		{
			get
			{
				return this._Size;
			}
			set
			{
				this._Size = value;
				this.HasSize = true;
			}
		}

		public uint Status
		{
			get
			{
				return this._Status;
			}
			set
			{
				this._Status = value;
				this.HasStatus = true;
			}
		}

		public List<ErrorInfo> Error
		{
			get
			{
				return this._Error;
			}
			set
			{
				this._Error = value;
			}
		}

		public List<ErrorInfo> ErrorList
		{
			get
			{
				return this._Error;
			}
		}

		public int ErrorCount
		{
			get
			{
				return this._Error.get_Count();
			}
		}

		public ulong Timeout
		{
			get
			{
				return this._Timeout;
			}
			set
			{
				this._Timeout = value;
				this.HasTimeout = true;
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
			Header.Deserialize(stream, this);
		}

		public static Header Deserialize(Stream stream, Header instance)
		{
			return Header.Deserialize(stream, instance, -1L);
		}

		public static Header DeserializeLengthDelimited(Stream stream)
		{
			Header header = new Header();
			Header.DeserializeLengthDelimited(stream, header);
			return header;
		}

		public static Header DeserializeLengthDelimited(Stream stream, Header instance)
		{
			long num = (long)((ulong)ProtocolParser.ReadUInt32(stream));
			num += stream.get_Position();
			return Header.Deserialize(stream, instance, num);
		}

		public static Header Deserialize(Stream stream, Header instance, long limit)
		{
			instance.ObjectId = 0uL;
			instance.Size = 0u;
			instance.Status = 0u;
			if (instance.Error == null)
			{
				instance.Error = new List<ErrorInfo>();
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
					if (num2 != 8)
					{
						if (num2 != 16)
						{
							if (num2 != 24)
							{
								if (num2 != 32)
								{
									if (num2 != 40)
									{
										if (num2 != 48)
										{
											if (num2 != 58)
											{
												if (num2 != 64)
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
													instance.Timeout = ProtocolParser.ReadUInt64(stream);
												}
											}
											else
											{
												instance.Error.Add(ErrorInfo.DeserializeLengthDelimited(stream));
											}
										}
										else
										{
											instance.Status = ProtocolParser.ReadUInt32(stream);
										}
									}
									else
									{
										instance.Size = ProtocolParser.ReadUInt32(stream);
									}
								}
								else
								{
									instance.ObjectId = ProtocolParser.ReadUInt64(stream);
								}
							}
							else
							{
								instance.Token = ProtocolParser.ReadUInt32(stream);
							}
						}
						else
						{
							instance.MethodId = ProtocolParser.ReadUInt32(stream);
						}
					}
					else
					{
						instance.ServiceId = ProtocolParser.ReadUInt32(stream);
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
			Header.Serialize(stream, this);
		}

		public static void Serialize(Stream stream, Header instance)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ServiceId);
			if (instance.HasMethodId)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, instance.MethodId);
			}
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Token);
			if (instance.HasObjectId)
			{
				stream.WriteByte(32);
				ProtocolParser.WriteUInt64(stream, instance.ObjectId);
			}
			if (instance.HasSize)
			{
				stream.WriteByte(40);
				ProtocolParser.WriteUInt32(stream, instance.Size);
			}
			if (instance.HasStatus)
			{
				stream.WriteByte(48);
				ProtocolParser.WriteUInt32(stream, instance.Status);
			}
			if (instance.Error.get_Count() > 0)
			{
				using (List<ErrorInfo>.Enumerator enumerator = instance.Error.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ErrorInfo current = enumerator.get_Current();
						stream.WriteByte(58);
						ProtocolParser.WriteUInt32(stream, current.GetSerializedSize());
						ErrorInfo.Serialize(stream, current);
					}
				}
			}
			if (instance.HasTimeout)
			{
				stream.WriteByte(64);
				ProtocolParser.WriteUInt64(stream, instance.Timeout);
			}
		}

		public uint GetSerializedSize()
		{
			uint num = 0u;
			num += ProtocolParser.SizeOfUInt32(this.ServiceId);
			if (this.HasMethodId)
			{
				num += 1u;
				num += ProtocolParser.SizeOfUInt32(this.MethodId);
			}
			num += ProtocolParser.SizeOfUInt32(this.Token);
			if (this.HasObjectId)
			{
				num += 1u;
				num += ProtocolParser.SizeOfUInt64(this.ObjectId);
			}
			if (this.HasSize)
			{
				num += 1u;
				num += ProtocolParser.SizeOfUInt32(this.Size);
			}
			if (this.HasStatus)
			{
				num += 1u;
				num += ProtocolParser.SizeOfUInt32(this.Status);
			}
			if (this.Error.get_Count() > 0)
			{
				using (List<ErrorInfo>.Enumerator enumerator = this.Error.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ErrorInfo current = enumerator.get_Current();
						num += 1u;
						uint serializedSize = current.GetSerializedSize();
						num += serializedSize + ProtocolParser.SizeOfUInt32(serializedSize);
					}
				}
			}
			if (this.HasTimeout)
			{
				num += 1u;
				num += ProtocolParser.SizeOfUInt64(this.Timeout);
			}
			num += 2u;
			return num;
		}

		public void SetServiceId(uint val)
		{
			this.ServiceId = val;
		}

		public void SetMethodId(uint val)
		{
			this.MethodId = val;
		}

		public void SetToken(uint val)
		{
			this.Token = val;
		}

		public void SetObjectId(ulong val)
		{
			this.ObjectId = val;
		}

		public void SetSize(uint val)
		{
			this.Size = val;
		}

		public void SetStatus(uint val)
		{
			this.Status = val;
		}

		public void AddError(ErrorInfo val)
		{
			this._Error.Add(val);
		}

		public void ClearError()
		{
			this._Error.Clear();
		}

		public void SetError(List<ErrorInfo> val)
		{
			this.Error = val;
		}

		public void SetTimeout(ulong val)
		{
			this.Timeout = val;
		}

		public override int GetHashCode()
		{
			int num = base.GetType().GetHashCode();
			num ^= this.ServiceId.GetHashCode();
			if (this.HasMethodId)
			{
				num ^= this.MethodId.GetHashCode();
			}
			num ^= this.Token.GetHashCode();
			if (this.HasObjectId)
			{
				num ^= this.ObjectId.GetHashCode();
			}
			if (this.HasSize)
			{
				num ^= this.Size.GetHashCode();
			}
			if (this.HasStatus)
			{
				num ^= this.Status.GetHashCode();
			}
			using (List<ErrorInfo>.Enumerator enumerator = this.Error.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ErrorInfo current = enumerator.get_Current();
					num ^= current.GetHashCode();
				}
			}
			if (this.HasTimeout)
			{
				num ^= this.Timeout.GetHashCode();
			}
			return num;
		}

		public override bool Equals(object obj)
		{
			Header header = obj as Header;
			if (header == null)
			{
				return false;
			}
			if (!this.ServiceId.Equals(header.ServiceId))
			{
				return false;
			}
			if (this.HasMethodId != header.HasMethodId || (this.HasMethodId && !this.MethodId.Equals(header.MethodId)))
			{
				return false;
			}
			if (!this.Token.Equals(header.Token))
			{
				return false;
			}
			if (this.HasObjectId != header.HasObjectId || (this.HasObjectId && !this.ObjectId.Equals(header.ObjectId)))
			{
				return false;
			}
			if (this.HasSize != header.HasSize || (this.HasSize && !this.Size.Equals(header.Size)))
			{
				return false;
			}
			if (this.HasStatus != header.HasStatus || (this.HasStatus && !this.Status.Equals(header.Status)))
			{
				return false;
			}
			if (this.Error.get_Count() != header.Error.get_Count())
			{
				return false;
			}
			for (int i = 0; i < this.Error.get_Count(); i++)
			{
				if (!this.Error.get_Item(i).Equals(header.Error.get_Item(i)))
				{
					return false;
				}
			}
			return this.HasTimeout == header.HasTimeout && (!this.HasTimeout || this.Timeout.Equals(header.Timeout));
		}

		public static Header ParseFrom(byte[] bs)
		{
			return ProtobufUtil.ParseFrom<Header>(bs, 0, -1);
		}
	}
}
