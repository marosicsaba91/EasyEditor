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
		const string saveKey = "QuickAccess";

		readonly List<Object> pinnedObjects = new();
		readonly List<Object> selection = new();

		// Reorder
		Object grabbed = null;
		readonly List<Rect> grabRects = new();
		int insertIndex = 0;
		bool insertBelow = false;

		GUIContent deselectContent;
		GUIContent selectContent;
		GUIContent grabContent;
		GUIContent addContent;
		GUIContent removeContent;

		Vector2 scrollPosition;

		void OnGUI()
		{
			if (saveData == null)
				TryToLoad();

			if (deselectContent == null)
			{
				deselectContent = new("Deselect", "Deselect the object");
				selectContent = new("Select", "Select the object");
				grabContent = new("=", "Grab to reorder list");  // ⇵
				addContent = new("+", "Pin to Quick Access");
				removeContent = new("-", "Remove from Quick Access");
			}

			selection.Clear();
			pinnedObjects.Clear();
			selection.AddRange(Selection.objects);
			pinnedObjects.AddRange(saveData.quickAccess);
			pinnedObjects.AddRange(selection.Where(o => !saveData.quickAccess.Contains(o)));

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			grabRects.Clear();
			for (int i = 0; i < pinnedObjects.Count; i++)
				DrawObject(EditorGUILayout.GetControlRect(), i, pinnedObjects[i], grabRects);

			EditorGUILayout.EndScrollView();

			HandleDragAndDrop(GUILayoutUtility.GetLastRect().width, grabRects);
		}

		void DrawObject(Rect controlRect, int index, Object obj, List<Rect> grabRects)
		{
			Rect addRemoveRect = SliceOut(ref controlRect, right: true, 20);
			Rect grabRect = SliceOut(ref controlRect, right: false, 14);
			Rect selectRect = SliceOut(ref controlRect, right: true, 60);
			Rect selectToggleRect = SliceOut(ref controlRect, right: false, 14);

			float a;
			a = obj != grabbed ? 1 : 0.5f;
			Color white = new(1, 1, 1, a);
			Color gray = new(0.5f, 0.5f, 0.5f, a);
			Color green = new(0.75f, 1f, 0.45f, a);

			GUI.color = white;

			Object newObj = EditorGUI.ObjectField(controlRect, obj, typeof(Object), true);
			if (newObj != obj && !pinnedObjects.Contains(newObj))
			{
				saveData.quickAccess.RemoveAt(index);
				saveData.quickAccess.Insert(index, newObj);
				TryToSave();
			}

			bool isSelected = selection.Contains(obj);
			if (EditorGUI.Toggle(selectToggleRect, GUIContent.none, isSelected) != isSelected)
			{
				if (isSelected)
					selection.Remove(obj);
				else
					selection.Add(obj);
				Selection.objects = selection.ToArray();
			}

			GUI.color = isSelected ? gray : white;
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
			GUI.color = white;

			bool isQuickAccess = saveData.quickAccess.Contains(obj);
			if (isQuickAccess)
			{
				GUI.Label(grabRect, grabContent);
				grabRects.Add(grabRect);
			}

			GUI.color = isQuickAccess ? white : green;
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

		void HandleDragAndDrop(float width, List<Rect> grabRects)
		{
			if (grabbed != null)
			{
				Rect indicatorRect = grabRects[insertIndex];
				indicatorRect.width = width;
				indicatorRect.position -= new Vector2(0, 2);
				if (!insertBelow)
					indicatorRect.position += new Vector2(0, indicatorRect.height + 2);
				indicatorRect.height = 2;
				EditorGUI.DrawRect(indicatorRect, new Color(1, 1, 1, 0.5f));
			}

			EventType et = Event.current.type;
			if (et is not EventType.MouseDown and not EventType.MouseUp and not EventType.MouseDrag)
				return;

			if (et == EventType.MouseDown)
			{
				for (int i = 0; i < grabRects.Count; i++)
				{
					bool isMouseInRect = grabRects[i].Contains(Event.current.mousePosition);
					if (isMouseInRect)
					{
						grabbed = pinnedObjects[i];
						insertIndex = i;
						float mouseY = Event.current.mousePosition.y;
						float distance = mouseY - grabRects[i].center.y;
						insertBelow = distance < 0;
						Repaint();
					}
				}
			}
			else if (et == EventType.MouseDrag && grabbed != null)
			{
				float mouseY = Event.current.mousePosition.y;
				float closestDistance = float.MaxValue;

				for (int i = 0; i < grabRects.Count; i++)
				{
					Rect grabRect = grabRects[i];
					float distance = mouseY - grabRect.center.y;
					if (Mathf.Abs(distance) < Mathf.Abs(closestDistance))
					{
						closestDistance = distance;
						insertIndex = i;
						insertBelow = distance < 0;
					}
				}

				Repaint();
			}
			else if (et == EventType.MouseUp && grabbed != null)
			{
				int remove = saveData.quickAccess.IndexOf(grabbed);
				int insert = insertIndex + (insertBelow ? 0 : 1);
				if (insert > remove)
					insert -= 1;

				if (remove >= 0)
				{
					saveData.quickAccess.RemoveAt(remove);
					saveData.quickAccess.Insert(insert, grabbed);
					TryToSave();
				}

				grabbed = null;
				Repaint();
			}

			if (insertIndex >= grabRects.Count)
				insertIndex = grabRects.Count - 1;
		}



		void TryToLoad()
		{
			string json = EditorPrefs.GetString(saveKey, default);
			saveData ??= JsonUtility.FromJson<QuickAccessSaveData>(json);
			saveData.SetupAfterDeserialization();
		}

		void TryToSave()
		{
			if (saveData == null) return;
			string json = JsonUtility.ToJson(saveData);
			EditorPrefs.SetString(saveKey, json);
		}

		static Rect SliceOut(ref Rect self, bool right, float pixels)
		{
			float spacing = EditorGUIUtility.standardVerticalSpacing;
			Rect slice = self;
			slice.width = pixels;
			float newWidth = self.width - pixels - spacing;
			self.width = Mathf.Max(0, newWidth);

			if (right)
			{
				if (newWidth < 0)
					self.x -= newWidth;

				slice.x = self.xMax;
				slice.x += spacing;
			}
			else
			{
				self.x += pixels;
				self.x += spacing;
			}

			return slice;
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