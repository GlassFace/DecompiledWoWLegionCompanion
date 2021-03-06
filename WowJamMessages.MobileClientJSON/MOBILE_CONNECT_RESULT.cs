using System;
using System.Runtime.Serialization;

namespace WowJamMessages.MobileClientJSON
{
	[DataContract]
	public enum MOBILE_CONNECT_RESULT
	{
		[EnumMember]
		MOBILE_CONNECT_RESULT_SUCCESS = 0,
		[EnumMember]
		MOBILE_CONNECT_RESULT_GENERIC_FAILURE = 1,
		[EnumMember]
		MOBILE_CONNECT_RESULT_CHARACTER_STILL_IN_WORLD = 2,
		[EnumMember]
		MOBILE_CONNECT_RESULT_UNABLE_TO_ENTER_WORLD = 3,
		[EnumMember]
		MOBILE_CONNECT_RESULT_MOBILE_LOGIN_DISABLED = 4,
		[EnumMember]
		MOBILE_CONNECT_RESULT_MOBILE_TRIAL_NOT_ALLOWED = 5,
		[EnumMember]
		MOBILE_CONNECT_RESULT_MOBILE_CONSUMPTION_TIME = 6
	}
}
