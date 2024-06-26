﻿#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace EasyEditor
{

	[Serializable]
	class ObjectBrowserSetting
	{
		[SerializeField] string selectedType;
		[SerializeField] List<TypeDisplaySetting> allTypeInfo = new();
		[SerializeField] bool isDirty = false;
		[SerializeField] bool showPrefabs = true;
		[SerializeField] ObjectsBrowserView selectedView = ObjectsBrowserView.TableView;

		public bool ShowPrefabs
		{
			get => showPrefabs;
			set
			{
				showPrefabs = value;
				isDirty = true;
			}
		}

		public ObjectsBrowserView SelectedView
		{
			get => selectedView;
			set
			{
				selectedView = value;
				isDirty = true;
			}
		}

		internal void MovePinnedTab(TypeDisplaySetting type, bool right)
		{
			if (allTypeInfo.Count < 2) return;
			int index = allTypeInfo.FindIndex(x => x.fullTypeName == type.fullTypeName);
			if (index == -1) return;
			int otherIndex;
			if (right)
				otherIndex = index == allTypeInfo.Count - 1 ? 0 : index + 1;
			else
				otherIndex = index == 0 ? allTypeInfo.Count - 1 : index - 1;
			(allTypeInfo[index], allTypeInfo[otherIndex]) = (allTypeInfo[otherIndex], allTypeInfo[index]);
			isDirty = true;
		}

		internal void RemovePinnedTab(TypeDisplaySetting typeDisplaySetting)
		{
			int index = allTypeInfo.FindIndex(x => x.fullTypeName == typeDisplaySetting.fullTypeName);
			if (index == -1) return;
			allTypeInfo.RemoveAt(index);
			isDirty = true;
		}

		public void CleanupSetting()
		{
			// Remove not existing types
			for (int i = allTypeInfo.Count - 1; i >= 0; i--)
			{
				if (allTypeInfo[i].ObjectType == null)
				{
					allTypeInfo.RemoveAt(i);
					isDirty = true;
				}
			}

			// Check if all types are in order
			bool inOrder = true;
			for (int i = 0; i < allTypeInfo.Count; i++)
			{
				if (allTypeInfo[i].typeOrderPriority != i)
				{
					inOrder = false;
					break;
				}
			}
			if (!inOrder)
				return;

			// Reorder types if needed
			allTypeInfo.Sort((x, y) => x.typeOrderPriority.CompareTo(y.typeOrderPriority));
			for (int i = 0; i < allTypeInfo.Count; i++)
			{
				TypeDisplaySetting typeSetting = allTypeInfo[i];
				typeSetting.typeOrderPriority = i;
				allTypeInfo[i] = typeSetting;
			}
			isDirty = true;
		}

		public IReadOnlyList<TypeDisplaySetting> GetPinnedTypesInOrder() =>
			allTypeInfo.Where(x => x.isPinned).OrderBy(x => x.typeOrderPriority).ToList();

		public bool TryGetSelectedType(out TypeDisplaySetting typeSetting)
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
				TypeDisplaySetting setting = new(type);
				allTypeInfo.Add(setting);
				selectedType = type.FullName;
				setting.typeOrderPriority = allTypeInfo.Count;
				isDirty = true;
			}
			else
			{
				string typeName = allTypeInfo[index].fullTypeName;
				if (typeName != selectedType)
				{
					TypeDisplaySetting setting = allTypeInfo[index];
					setting.isPinned = true;
					selectedType = typeName;
					isDirty = true;
				}
			}
		}

		public void ResetLayout(Type selected)
		{
			TypeDisplaySetting typeSetting = allTypeInfo.Find(x => x.ObjectType == selected);
			if (typeSetting == null) return;

			typeSetting.properties = new();
			typeSetting.openedObjects = new();
			isDirty = true;
		}

		// ----------------- Singleton -----------------

		const string settingKey = "ObjectBrowserSetting";
		static ObjectBrowserSetting instance;

		public static ObjectBrowserSetting Instance
		{
			get
			{
				if (instance == null)
				{
					string objectBrowserSettingString = EditorPrefs.GetString(settingKey, null);
					if (objectBrowserSettingString != null)
					{
						instance = JsonUtility.FromJson<ObjectBrowserSetting>(objectBrowserSettingString);
						if (instance != null)
							return instance;
						else
							Debug.LogError("Failed to load ObjectBrowserSetting from EditorPrefs:\n" + objectBrowserSettingString);
					}
					instance = new() { isDirty = true };
				}
				return instance;
			}
		}

		public static void TrySave()
		{
			if (instance.isDirty)
			{
				instance.isDirty = false;
				string objectBrowserSettingString = JsonUtility.ToJson(instance);
				EditorPrefs.SetString(settingKey, objectBrowserSettingString);
			}
		}

		public void SetDirty() => isDirty = true;
	}

	[Serializable]
	public class TypeDisplaySetting
	{
		public string fullTypeName;
		public string name;
		public bool isPinned;
		public int typeOrderPriority;
		public string orderingPropertyName;
		public bool isAscending;
		public bool showFiles;
		public List<string> openedObjects;
		public List<PropertyDisplaySetting> properties;

		Type cachedObjectType;
		public Type ObjectType
		{
			get
			{
				if (cachedObjectType != null) return cachedObjectType;
				if (fullTypeName == null) return null;
				string n = fullTypeName;
				cachedObjectType = ObjectBrowserCache.GetAllScriptableTypes().Find(x => x.FullName == n);
				if (cachedObjectType == null)
					cachedObjectType = ObjectBrowserCache.GetAllMonoBehaviourTypes().Find(x => x.FullName == n);
				return cachedObjectType;
			}
		}

		public TypeDisplaySetting(Type type)
		{
			fullTypeName = type.FullName;
			cachedObjectType = type;
			name = type.Name;
			isPinned = true;
			showFiles = true;
			typeOrderPriority = 0;
			orderingPropertyName = null;
			isAscending = true;
			openedObjects = new();
			properties = new();
		}

		public bool TryGetPropertySettings(string fullTypeName, string propertyName, out PropertyDisplaySetting pds)
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
				PropertyDisplaySetting pds = new(fullTypeName, propertyName, width);
				properties.Add(pds);
			}
			else
			{
				PropertyDisplaySetting pds = properties[index];
				pds.width = width;
			}
		}
	}

	[Serializable]
	public class PropertyDisplaySetting
	{
		public string fullTypeName;
		public string propertyName;
		public float width;

		public PropertyDisplaySetting(string fullTypeName, string propertyName, float width)
		{
			this.fullTypeName = fullTypeName;
			this.propertyName = propertyName;
			this.width = width;
		}
	}
}

#endif