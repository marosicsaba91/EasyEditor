#if UNITY_EDITOR

using System;

namespace EasyEditor
{
	[Serializable]
	public class TableViewSetting_Column
	{
		public string fullTypeName;
		public string propertyName;
		public float width;

		public TableViewSetting_Column(string fullTypeName, string propertyName, float width)
		{
			this.fullTypeName = fullTypeName;
			this.propertyName = propertyName;
			this.width = width;
		}
	}
}

#endif