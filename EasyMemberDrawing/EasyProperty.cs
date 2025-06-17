using System; 

namespace EasyEditor
{
	[Serializable]
	public class EasyProperty
	{
		[NonSerialized] public string propertyName;
		[NonSerialized] public bool usePropertyNameAsLabel;

		public EasyProperty(string propertyName, bool usePropertyNameAsLabel = false)
		{
			this.propertyName = propertyName;
			this.usePropertyNameAsLabel = usePropertyNameAsLabel;
		}
	}
}