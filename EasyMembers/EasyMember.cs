using System; 

namespace EasyInspector
{
	[Serializable]
	public class EasyMember
	{
		[NonSerialized] public string memberName;
		[NonSerialized] public bool useMemberNameAsLabel; 

		public EasyMember(string memberName, bool useMemberNameAsLabel = false)
		{
			this.memberName = memberName;
			this.useMemberNameAsLabel = useMemberNameAsLabel;
		}
	}
}