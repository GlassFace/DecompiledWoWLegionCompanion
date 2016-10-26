using JamLib;
using System;
using System.Runtime.Serialization;

namespace WowJamMessages.MobileClientJSON
{
	[FlexJamMessage(Id = 4860, Name = "MobileClientCanResearchGarrisonTalentResult", Version = 33577221u), DataContract]
	public class MobileClientCanResearchGarrisonTalentResult
	{
		[FlexJamMember(Name = "conditionText", Type = FlexJamType.String), DataMember(Name = "conditionText")]
		public string ConditionText
		{
			get;
			set;
		}

		[FlexJamMember(Name = "result", Type = FlexJamType.Int32), DataMember(Name = "result")]
		public int Result
		{
			get;
			set;
		}

		[FlexJamMember(Name = "garrTalentID", Type = FlexJamType.Int32), DataMember(Name = "garrTalentID")]
		public int GarrTalentID
		{
			get;
			set;
		}
	}
}
