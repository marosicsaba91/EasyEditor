#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	public struct ObjectsBrowserColumn
	{
		public string propertyName;
		public GUIContent label;
		public float width;
	}

	//public enum ObjectsBrowserView { TableView, ListView }

	class ObjectBrowserWindow : EditorWindow
	{
		const float foldoutWidth = 15;
		const float spacing = 2;

		static readonly List<Object> openedObjects = new();
		static string _savePath = "ScriptableObjects";

		static Texture soPic;
		static Texture mbPic;
		static Texture goPic;
		static Texture prPic;
		static Texture newPic;

		static Vector2 scrollPosition;
		static GUIStyle selectedButtonStyle;
		static float SingleLineHeight => EditorGUIUtility.singleLineHeight;

		static int resizedColumn = -1;
		static float lastMouseX = 0;

		[MenuItem("Tools/Object Browser")]
		public static void Open()
		{
			ObjectBrowserWindow window = GetWindow<ObjectBrowserWindow>();
			window.titleContent = new GUIContent("Object Browser");
		}

		void OnGUI()
		{
			selectedButtonStyle = new(GUI.skin.button) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.5f, 0.7f, 1) } };

			ObjectBrowserSetting settings = ObjectBrowserSetting.Instance;
			settings.CleanupSetting();
			IReadOnlyList<TypeDisplaySetting> displayedTypes = settings.GetPinnedTypesInOrder();

			soPic = soPic != null ? soPic : EditorGUIUtility.IconContent("ScriptableObject Icon").image;
			mbPic = mbPic != null ? mbPic : EditorGUIUtility.IconContent("cs Script Icon").image;
			goPic = goPic != null ? goPic : EditorGUIUtility.IconContent("GameObject Icon").image;
			prPic = prPic != null ? prPic : EditorGUIUtility.IconContent("Prefab Icon").image;
			newPic = newPic != null ? newPic : EditorGUIUtility.IconContent("CreateAddNew").image;

			bool hasSelected = settings.TryGetSelectedType(out TypeDisplaySetting selectedTypeSetting);
			Type selectedType = hasSelected ? selectedTypeSetting.ObjectType : null;

			Rect fullWindowRect = position;
			fullWindowRect.position = Vector2.zero;
			DrawTypeTabs(displayedTypes, selectedType, settings, ref fullWindowRect);

			if (hasSelected)
				DrawObjectClass(settings, selectedTypeSetting, settings.ShowPrefabs, ref fullWindowRect);

			ObjectBrowserSetting.TrySave();
		}

		static void DrawTypeTabs(IReadOnlyList<TypeDisplaySetting> types, Type selected, ObjectBrowserSetting settings, ref Rect fullWindowRect)
		{
			const float actionButtonWidth = 28;
			const float lineHight = 22;
			const float buttonWidthExtra = 100;

			float fullHeaderWidth = 0;
			for (int i = 0; i < types.Count; i++)
			{
				TypeDisplaySetting typeDisplaySetting = types[i];
				Type type = typeDisplaySetting.ObjectType;
				string name = type.Name;
				float width = GetButtonWidth(name);
				fullHeaderWidth += width;
			}

			Rect headerRect = fullWindowRect;
			headerRect.SliceOut(0, Side.Up, addSpace: true);
			headerRect.SliceOut(0, Side.Left, addSpace: true);
			Rect actionButtonsRect = headerRect.SliceOut(actionButtonWidth * 6 + spacing * 6, Side.Right);
			actionButtonsRect.height = lineHight;
			float fullWindowWidth = fullWindowRect.width;
			float availableWidth = headerRect.width;
			Rect backgroundRect = new(0, 0, fullWindowWidth, lineHight + spacing * 2);

			EditorHelper.DrawBox(backgroundRect, EditorHelper.buttonBackgroundColor);

			TypeDisplaySetting selectedTypeSetting = types.FirstOrDefault(x => x.ObjectType == selected);

			if (selected == null)
				GUI.enabled = false;

			// Show Prefabs / GameObjects
			bool isMonoBehaviour = selected != null && selected.IsSubclassOf(typeof(MonoBehaviour));
			Rect mbr = actionButtonsRect.SliceOut(actionButtonWidth, Side.Left);
			if (isMonoBehaviour)
			{
				bool showPrefabs = settings.ShowPrefabs;
				GUIContent content = new(
					showPrefabs ? prPic : goPic,
					showPrefabs ? "Show Prefab files in Project" : "Show GameObjects In Scene");

				if (GUI.Button(mbr, content))
					settings.ShowPrefabs = !showPrefabs;
			}

			// Refresh button
			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent("R", "Refresh")))
				ObjectBrowserCache.ClearCache();

			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent("◄", "Step Left Selected Tab")))
			{
				settings.MovePinnedTab(selectedTypeSetting, false);
				return;
			}
			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent("►", "Step Right Selected Tab")))
			{
				settings.MovePinnedTab(selectedTypeSetting, true);
				return;
			}
			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent("✖", "Remove Selected Tab")))
			{
				settings.RemovePinnedTab(selectedTypeSetting);
				return;
			}
			GUI.enabled = true;
			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent(newPic, "Add New Type Tab")))
			{
				Type selectedType = TypeSelectEditorWindow.Open();
				if (selectedType != null)
					settings.SetSelectedType(selectedType);
				return;
			}

			float x = headerRect.x;
			float y = headerRect.y;
			float fullHeaderHeight = lineHight;

			for (int i = 0; i < types.Count; i++)
			{
				TypeDisplaySetting typeDisplaySetting = types[i];
				Type type = typeDisplaySetting.ObjectType;
				string name = type.Name;

				float nextWidth = GetButtonWidth(name);
				if (x + nextWidth > availableWidth)
				{
					x = headerRect.x;
					y += lineHight + spacing;
					fullHeaderHeight += lineHight + spacing;
					backgroundRect.y += lineHight + spacing;
					EditorHelper.DrawBox(backgroundRect, EditorHelper.buttonBackgroundColor);
				}

				Rect tabRect = new(x, y, GetButtonWidth(name), lineHight);
				Texture texture = type.IsSubclassOf(typeof(ScriptableObject)) ? soPic : type.IsSubclassOf(typeof(MonoBehaviour)) ? mbPic : prPic;

				bool isSelected = selected == type;
				List<Object> objects = ObjectBrowserCache.GetObjectsByType(type, settings.ShowPrefabs);
				int count = objects.Count;

				if (isSelected)
					GUI.color = new Color(0.8f, 0.8f, 0.8f);
				GUIStyle style = isSelected ? selectedButtonStyle : GUI.skin.button;
				if (GUI.Button(tabRect, new GUIContent($" {name} ({count})", texture), style))
					settings.SetSelectedType(type);
				GUI.color = Color.white;

				x += nextWidth + spacing;
			}

			EditorGUILayout.GetControlRect(GUILayout.Height(fullHeaderHeight));
			fullWindowRect.SliceOut(fullHeaderHeight + spacing * 3, Side.Up, addSpace: false);

			static float GetButtonWidth(string name) =>
				GUI.skin.button.CalcSize(new GUIContent(name)).x + buttonWidthExtra;
		}

		void DrawObjectClass(ObjectBrowserSetting settings, TypeDisplaySetting typeDisplaySetting, bool preferFiles, ref Rect fullWindowRect)
		{
			ObjectBrowserCache.CleanupObjectCache();
			List<Object> objects = ObjectBrowserCache.GetObjectsByType(typeDisplaySetting.ObjectType, preferFiles);
			ObjectTableDrawer customTableDrawer = ObjectBrowserCache.GetCustomDrawer(typeDisplaySetting.ObjectType);
			fullWindowRect.SliceOut(2, Side.Down, false);

			//-----

			if (objects.Count == 0) return;
			List<ObjectsBrowserColumn> columns = FindColumns(objects, typeDisplaySetting, customTableDrawer);

			// Calculate header height
			float headerHeight = SingleLineHeight;
			foreach (ObjectsBrowserColumn column in columns)
			{
				float labelWidth = GUI.skin.label.CalcSize(new GUIContent(column.label)).x;
				if (labelWidth > column.width)
					headerHeight = MathF.Max(headerHeight, labelWidth);
			}
			if (headerHeight != 0)
				EditorGUILayout.GetControlRect(GUILayout.Height(headerHeight - SingleLineHeight));

			// Draw header
			const float nameWidth = 200;  //TODO: make it dynamic
			EditorGUILayout.BeginHorizontal();
			Rect newItemRect = EditorGUILayout.GetControlRect(GUILayout.Width(nameWidth), GUILayout.Height(SingleLineHeight));

			Type selected = typeDisplaySetting.ObjectType;
			bool isScriptableObject = selected != null && selected.IsSubclassOf(typeof(ScriptableObject));
			if (isScriptableObject)
				DrawAddNewItemButton(newItemRect, typeDisplaySetting);

			for (int i = 0; i < columns.Count; i++)
			{
				ObjectsBrowserColumn column = columns[i];
				Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(column.width));
				Vector2 pivot = new(rect.x + rect.height / 2, rect.center.y);
				float labelWidth = GUI.skin.label.CalcSize(new GUIContent(column.label)).x;

				if (labelWidth > column.width)
				{
					Rect r = rect;
					r.width = headerHeight;
					headerHeight = MathF.Max(headerHeight, labelWidth);
					GUIUtility.RotateAroundPivot(-90, pivot);
					EditorGUI.LabelField(r, column.label);
					GUIUtility.RotateAroundPivot(90, pivot);
				}
				else
				{
					Rect r = rect;
					r.x += 2;
					EditorGUI.LabelField(r, column.label);
				}

				// Resizer 
				Rect resizeRect = new(rect.xMax, rect.y, 4, rect.height);
				EditorGUI.DrawRect(resizeRect, new Color(0, 0, 0, 0.2f));
				Vector2 mouse = Event.current.mousePosition;
				if (Event.current.type == EventType.MouseDown && resizeRect.Contains(mouse))
				{
					resizedColumn = i;
					lastMouseX = mouse.x;
				}
				if (resizedColumn == i && lastMouseX != mouse.x)
				{
					const float minColumnWidth = 16;
					float width = column.width + mouse.x - lastMouseX;
					width = MathF.Max(width, minColumnWidth);
					column.width = width;
					lastMouseX = mouse.x;
					typeDisplaySetting.SetColumnWidth(typeDisplaySetting.fullTypeName, column.propertyName, width);
					settings.SetDirty();
					Repaint();
				}
				if (Event.current.type == EventType.MouseUp)
					resizedColumn = -1;
			}
			EditorGUILayout.EndHorizontal();
			fullWindowRect.SliceOut(headerHeight, Side.Up, addSpace: false);

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(fullWindowRect.width), GUILayout.Height(fullWindowRect.height));
			// Draw objects
			for (int objI = 0; objI < objects.Count; objI++)
			{
				Object obj = objects[objI];
				if (obj == null) continue;

				EditorGUILayout.BeginHorizontal();
				SerializedObject serializedObject = new(obj);

				Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(nameWidth));
				DrawItemName(obj, rect);

				// Find max height
				float maxHeigh = 0;
				SerializedProperty[] properties = new SerializedProperty[columns.Count];
				SerializedProperty property;
				for (int columnI = 0; columnI < columns.Count; columnI++)
				{
					ObjectsBrowserColumn column = columns[columnI];
					property = serializedObject.FindProperty(column.propertyName);
					properties[columnI] = property;

					if (property != null)
					{
						EditorGUI.GetPropertyHeight(property, GUIContent.none, false);  // This is needed because of its side effect
						float height = EditorGUIUtility.singleLineHeight;
						if (height > maxHeigh)
							maxHeigh = height;
					}
				}

				for (int columnI = 0; columnI < properties.Length; columnI++)
				{
					ObjectsBrowserColumn column = columns[columnI];
					property = serializedObject.FindProperty(column.propertyName);
					rect = EditorGUILayout.GetControlRect(
					   GUILayout.Width(column.width),
					   GUILayout.Height(maxHeigh));

					if (property != null)
					{
						if (customTableDrawer == null || !customTableDrawer.OverrideProperty_Base(rect, obj, property, column.propertyName))
							EditorGUI.PropertyField(rect, property, GUIContent.none, false);
					}
				}

				serializedObject.ApplyModifiedProperties();
				EditorGUILayout.EndHorizontal();

				if(openedObjects.Contains(obj))
					DrawFullObject(obj);
			}

			EditorGUILayout.EndScrollView();
		}


		static void DrawFullObject(Object obj)
		{
				EditorGUI.indentLevel++;
				SerializedObject serializedObject = new(obj);

				// Draw SerializedObject
				serializedObject.Update();
				SerializedProperty property = serializedObject.GetIterator();
				for (int i = 0; property.NextVisible(i == 0); i++)
					EditorGUILayout.PropertyField(property, true);

				EditorGUILayout.Space();
				serializedObject.ApplyModifiedProperties();
				EditorGUI.indentLevel--;
		}

		static List<ObjectsBrowserColumn> FindColumns(List<Object> objects, TypeDisplaySetting setting, ObjectTableDrawer customTableDrawer)
		{
			float defaultPropertyWidth = 200;

			List<ObjectsBrowserColumn> columns = new();
			for (int objI = 0; objI < objects.Count; objI++)
			{
				Object obj = objects[objI];
				if (obj == null) continue;
				SerializedObject serializedObject = new(obj);
				SerializedProperty property = serializedObject.GetIterator();
				for (int propI = 0; property.NextVisible(propI == 0); propI++)
				{
					string pName = property.name;
					int index = columns.FindIndex(c => c.propertyName == pName);
					if (customTableDrawer != null)
					{
						if (!customTableDrawer.ShowScript() && pName == "m_Script")
							continue;
						if (customTableDrawer.HideProperty(pName))
							continue;
					}
					if (index < 0)
					{
						string dataType = property.type;
						// Find Saved Width 

						GUIContent label = customTableDrawer != null ? customTableDrawer.GetGUIContent(pName, property.displayName) : new GUIContent(property.displayName);

						float width;
						if (setting.TryGetPropertySettings(setting.fullTypeName, pName, out PropertyDisplaySetting pds))
							width = pds.width;
						else
						{
							if (!typeToWidths.TryGetValue(dataType, out float typeWidth))
								if (dataType.StartsWith("PPtr<"))
									typeWidth = 150;
								else
									typeWidth = defaultPropertyWidth;

							float nameWidth = GUI.skin.label.CalcSize(label).x + 16;
							width = MathF.Max(nameWidth, typeWidth);
						}

						ObjectsBrowserColumn column = new() { propertyName = pName, label = label, width = width };
						columns.Add(column);
					}
				}
			}

			return columns;
		}

		static readonly Dictionary<string, float> typeToWidths = new()
		{
			{ "string", 200 },
			{ "int", 50 },
			{ "float", 50 },
			{ "bool", 16 },
			{ "Vector2", 100 },
			{ "Vector3", 150 },
			{ "Vector4", 200 },
			{ "Vector2Int", 100 },
			{ "Vector3Int", 150 },
			{ "Vector4Int", 200 },
			{ "Quaternion", 200 },
			{ "Color", 50 },
			{ "Rect", 100 },
			{ "Bounds", 100 },
			{ "AnimationCurve", 100 },
			{ "Gradient", 100 },
			{ "Object", 200 },
		};

		static void DrawItemName(Object obj, Rect rect)
		{ 
			Rect FoldoutRect = rect.SliceOut(16, Side.Left);
			Rect selectButtonRect = rect.SliceOut(20, Side.Left);

			bool isOpened = openedObjects.Contains(obj);
			if (EditorGUI.Foldout(FoldoutRect, isOpened, GUIContent.none) != isOpened)
			{
				if (isOpened)
					openedObjects.Remove(obj);
				else
					openedObjects.Add(obj);
			}

			if (GUI.Button(selectButtonRect, "→"))
				Selection.activeObject = obj;

			string newName = EditorGUI.TextField(rect, obj.name);
			if (newName != obj.name)
			{
				EditorUtility.SetDirty(obj);
				string fullPath = AssetDatabase.GetAssetPath(obj);
				AssetDatabase.RenameAsset(fullPath, newName);
				obj.name = newName;
			}


		}

		static void DrawAddNewItemButton(Rect rect, TypeDisplaySetting typeSetting)
		{
			string[] selectionGuids = Selection.assetGUIDs;
			for (int i = 0; i < selectionGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(selectionGuids[i]);
				if (Directory.Exists(path))
					_savePath = path;
				else if (File.Exists(path))
					_savePath = Path.GetDirectoryName(path);
			}

			bool pathExists = Directory.Exists(_savePath);
			string buttonString = pathExists ? $"Create New Here" : "Open a directory";

			GUI.enabled = pathExists;
			if (GUI.Button(rect, buttonString))
			{
				Type objectType = typeSetting.ObjectType;
				if (typeof(ScriptableObject).IsAssignableFrom(objectType))
				{
					_savePath = _savePath.Replace("Assets/", "");
					_savePath = _savePath.Replace("Assets\\", "");
					if (_savePath == "Assets")
						_savePath = "";
					GenerateNewScriptableObjectFile(_savePath, objectType);
				}
				else
					Debug.LogError("Non ScriptableObjects are Not supported!");
			}
			GUI.enabled = true;
		}

		static void GenerateNewScriptableObjectFile(string savePath, Type scriptableObjectType)
		{
			ScriptableObject newId = CreateAnyScriptableObjectInstance(ref scriptableObjectType);
			if (scriptableObjectType == null) return;

			if (newId == null)
			{
				Debug.LogError("Failed to create new instance of " + scriptableObjectType);
				return;
			}

			string fullPath = Application.dataPath + "/" + savePath;
			if (!Directory.Exists(fullPath))
				Directory.CreateDirectory(fullPath);

			string fileName = scriptableObjectType.Name;
			string filePath = fullPath + "/" + fileName + ".asset";
			while (File.Exists(filePath))
			{
				fileName += "_";
				filePath = fullPath + "/" + fileName + ".asset";
			}

			string relativePath = "Assets/" + savePath + "/" + fileName + ".asset";

			AssetDatabase.CreateAsset(newId, relativePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			ObjectBrowserCache.AddInstance(scriptableObjectType, newId);
		}

		static ScriptableObject CreateAnyScriptableObjectInstance(ref Type type)
		{
			bool isAbstract = type.IsAbstract;
			if (isAbstract)
			{
				type = TypeSelectEditorWindow.Open(type);
				if (type == null)
					return null;
			}
			return CreateInstance(type);
		}
	}
}
#endif