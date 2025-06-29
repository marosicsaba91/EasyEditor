#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	[Serializable]
	class TableViewSetting : ScriptableObject
	{  
		// ----------------- Singleton -----------------

		static TableViewSetting instance;

		public static TableViewSetting Instance
		{
			get
			{
				if (instance == null)
				{
					string path = AssetDatabase.FindAssets("t:TableViewSetting")
						.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
						.ToList()
						.FirstOrDefault();
					if (!string.IsNullOrEmpty(path))
						instance = AssetDatabase.LoadAssetAtPath<TableViewSetting>(path);
				}
				return instance;
			}
		}

		// ------------------------------------------------

		[SerializeField] string selectedType;
		[SerializeField] List<TableViewSetting_Type> allTypeInfo = new();

		public IReadOnlyList<TableViewSetting_Type> AllTypeInfo => allTypeInfo;

		public void MovePinnedTab(TableViewSetting_Type type, bool right)
		{
			if (allTypeInfo.Count < 2) return;
			int index = allTypeInfo.FindIndex(x => x.fullTypeName == type.fullTypeName);
			if (index == -1) return;
			if (right && index  >= allTypeInfo.Count - 1) return;
			if (!right && index <= 0) return;

			int otherIndex = index + (right ? 1 : -1);
			(allTypeInfo[index], allTypeInfo[otherIndex]) = (allTypeInfo[otherIndex], allTypeInfo[index]);
		}

		public void RemovePinnedTab(TableViewSetting_Type typeDisplaySetting)
		{
			int index = allTypeInfo.FindIndex(x => x.fullTypeName == typeDisplaySetting.fullTypeName);
			if (index == -1) return;
			allTypeInfo.RemoveAt(index);
			SetSODirty();
		}

		public void CleanupSetting()
		{
			// Remove not existing types
			for (int i = allTypeInfo.Count - 1; i >= 0; i--)
			{
				if (allTypeInfo[i].ObjectType == null)
				{
					allTypeInfo.RemoveAt(i);
					SetSODirty();
				}
			}
		}

		public bool TryGetSelectedType(out TableViewSetting_Type typeSetting)
		{
			int index = allTypeInfo.FindIndex(x => x.fullTypeName == selectedType);
			if (index == -1)
			{
				typeSetting = default;
				return false;
			}
			typeSetting = allTypeInfo[index];
			return typeSetting != null;
		}

		public void SetSelectedType(Type type)
		{
			int index = allTypeInfo.FindIndex(x => x.ObjectType == type);
			if (index == -1)
			{
				TableViewSetting_Type setting = new(type);
				allTypeInfo.Add(setting);
				selectedType = type.FullName;
				SetSODirty();
			}
			else
			{
				string typeName = allTypeInfo[index].fullTypeName;
				if (typeName != selectedType)
				{
					TableViewSetting_Type setting = allTypeInfo[index];
					setting.isPinned = true;
					selectedType = typeName;
					SetSODirty();
				}
			}
		}

		public void ResetLayout(Type selected)
		{
			TableViewSetting_Type typeSetting = allTypeInfo.Find(x => x.ObjectType == selected);
			if (typeSetting == null) return;

			typeSetting.properties = new();
			typeSetting.openedObjects = new();
			SetSODirty();
		}

		public void SetSODirty()
		{
			EditorUtility.SetDirty(this);
		}
	}
}

#endif