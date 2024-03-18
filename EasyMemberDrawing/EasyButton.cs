using System; 

namespace EasyEditor
{
	[Serializable]
	public class EasyButton
	{
		[NonSerialized] public string methodName;
		[NonSerialized] public bool useMethodNameAsLabel; 

		public EasyButton(string methodName, bool useMethodNameAsLabel = false)
		{
			this.methodName = methodName;
			this.useMethodNameAsLabel = useMethodNameAsLabel;
		}
	}
}