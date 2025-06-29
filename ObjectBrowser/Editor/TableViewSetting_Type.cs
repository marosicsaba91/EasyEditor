#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace EasyEditor
{
	[Serializable]
	public class TableViewSetting_Type
	{
		public string fullTypeName;
		public string name;
		public bool isPinned;
		public string orderingPropertyName;
		public bool isAscending;
		public bool showFiles;
		public bool showPrefabs = true;
		public float nameWidth = 200;
		public List<string> openedObjects;
		public List<TableViewSetting_Column> properties;

		Type cachedObjectType;

		public Type ObjectType
		{
			get
			{
				if (cachedObjectType != null) return cachedObjectType;
				if (fullTypeName == null) return null;
				string n = fullTypeName;
				cachedObjectType = TableViewCache.GetAllScriptableTypes().Find(x => x.FullName == n);
				if (cachedObjectType == null)
					cachedObjectType = TableViewCache.GetAllMonoBehaviourTypes().Find(x => x.FullName == n);
				return cachedObjectType;
			}
		}

		public TableViewSetting_Type(Type type)
		{
			fullTypeName = type.FullName;
			cachedObjectType = type;
			name = type.Name;
			isPinned = true;
			showFiles = true;
			orderingPropertyName = null;
			isAscending = true;
			openedObjects = new();
			properties = new();
		}

		public bool TryGetColumnSettings(string fullTypeName, string propertyName, out TableViewSetting_Column pds)
		{
			if (properties == null)
			{
				pds = default;
				return false;
			}
			pds = properties.Find(x => x.propertyName == propertyName && x.fullTypeName == fullTypeName);
			return pds != null;
		}

		public void SetColumnWidth(string fullTypeName, string propertyName, float width)
		{
			int index = properties.FindIndex(x => x.propertyName == propertyName && x.fullTypeName == fullTypeName);
			if (index < 0)
			{
				TableViewSetting_Column pds = new(fullTypeName, propertyName, width);
				properties.Add(pds);
			}
			else
			{
				TableViewSetting_Column pds = properties[index];
				pds.width = width;
			}
		}
	}
}

#endif