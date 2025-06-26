#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace EasyEditor.Internal
{
	class QuickAccessWindow : EditorWindow
	{

		[MenuItem("Tools/Quick Access")]
		static void Init()
		{
			QuickAccessWindow window = GetWindow(typeof(QuickAccessWindow)) as QuickAccessWindow;
			window.titleContent = new GUIContent("Quick Access");
			window.Show();
		}

		QuickAccessSaveData saveData;
		Object grabbed = null;
		const string saveKey = "QuickAccess";

		readonly List<Object> objects = new();
		readonly List<Object> selection = new();

		GUIContent deselectContent;
		GUIContent selectContent;
		GUIContent grabContent;
		GUIContent addContent;
		GUIContent removeContent;


		void OnGUI()
		{
			if (saveData == null)
				TryToLoad();

			deselectContent = new("Deselect", "Deselect the object");
			selectContent = new("Select", "Select the object");
			grabContent = new(EditorHelper.GetIcon(IconType.Move), "Grab to reorder list");
			addContent = new("+", "Add to Quick Access");
			removeContent = new("-", "Remove from Quick Access");


			selection.Clear();
			objects.Clear();
			selection.AddRange(Selection.objects);
			objects.AddRange(saveData.quickAccess);
			objects.AddRange(selection.Where(o => !saveData.quickAccess.Contains(o)));

			foreach (Object obj in objects)
				DrawObject(EditorGUILayout.GetControlRect(), obj);

		}

		private void DrawObject(Rect controlRect, Object obj)
		{
			Rect addRemoveRect = controlRect.SliceOut(25, Side.Right);
			Rect grabRect = controlRect.SliceOut(18, Side.Right);
			Rect selectRect = controlRect.SliceOut(60, Side.Right);
			Rect selectToggleRect = controlRect.SliceOut(14, Side.Right);

			EditorGUI.ObjectField(controlRect, obj, obj.GetType(), true);

			bool isSelected = selection.Contains(obj);
			if (EditorGUI.Toggle(selectToggleRect, GUIContent.none, isSelected) != isSelected)
			{
				if (isSelected)
					selection.Remove(obj);
				else
					selection.Add(obj);
				Selection.objects = selection.ToArray();
			}

			GUI.color = isSelected ? Color.gray : Color.white;
			if (GUI.Button(selectRect, isSelected ? deselectContent : selectContent))
			{
				if (isSelected)
					selection.Remove(obj);
				else
				{
					selection.Clear();
					selection.Add(obj);
				}
				Selection.objects = selection.ToArray();
			}

			GUI.color = grabbed == obj ? Color.gray : Color.white;
			bool isQuickAccess = saveData.quickAccess.Contains(obj);
			GUI.enabled = isQuickAccess;
			if (GUI.Button(grabRect, GUIContent.none))
			{
				if (grabbed != null && grabbed != obj)
				{
					int i = saveData.quickAccess.IndexOf(obj);
					saveData.quickAccess.Remove(grabbed);
					saveData.quickAccess.Insert(i, grabbed);
					grabbed = null;
					TryToSave();
				}
				else
					grabbed = grabbed == obj ? null : obj;
			}
			GUI.Label(grabRect, grabContent);
			GUI.color = Color.white;

			GUI.enabled = true;
			GUI.color = isQuickAccess ? EditorHelper.ErrorRedColor : EditorHelper.successGreenColor;
			if (GUI.Button(addRemoveRect, isQuickAccess ? removeContent : addContent))
			{
				if (isQuickAccess)
					saveData.quickAccess.Remove(obj);
				else
					saveData.quickAccess.Add(obj);

				TryToSave();
			}
			GUI.color = Color.white;
		}

		void TryToLoad()
		{
			string json = EditorPrefs.GetString(saveKey, default);
			//Debug.Log("Load: " + json);
			saveData ??= JsonUtility.FromJson<QuickAccessSaveData>(json);
			saveData.SetupAfterDeserialization();
		}

		void TryToSave()
		{
			if (saveData == null) return;
			string json = JsonUtility.ToJson(saveData);
			//Debug.Log("Save: " + json);
			EditorPrefs.SetString(saveKey, json);
		}
	}

	[System.Serializable]
	class QuickAccessSaveData : ISerializationCallbackReceiver
	{
		[SerializeField] List<string> quickAccessGUIDS = new();
		public List<Object> quickAccess = new();
		public void OnAfterDeserialize() { }

		public void SetupAfterDeserialization()
		{
			for (int i = 0; i < quickAccessGUIDS.Count; i++)
			{
				string guid = quickAccessGUIDS[i];
				if (string.IsNullOrEmpty(guid)) continue;
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path))
				{
					Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
					if (obj != null)
					{
						quickAccess[i] = obj;
						continue;
					}
				}
				if (quickAccess[i] != null) continue;

				quickAccess.RemoveAt(i);
				quickAccessGUIDS.RemoveAt(i);
				i--;
			}
		}

		public void OnBeforeSerialize()
		{
			quickAccessGUIDS.Clear();
			for (int i = 0; i < quickAccess.Count; i++)
			{
				Object obj = quickAccess[i];

				if (obj == null)
				{
					quickAccess.RemoveAt(i);
					i--;
					continue;
				}

				string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
				quickAccessGUIDS.Add(guid);
			}
		}
	}
}
#endif