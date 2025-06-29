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
		public float priority;
	}

	class TableViewWindow : EditorWindow
	{
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

		static int resizedColumn = -2; // -1 first column, -2 none
		static float lastMouseX = 0;

		[MenuItem("Tools/Table View")]
		public static void Open()
		{
			TableViewWindow window = GetWindow<TableViewWindow>();
			window.titleContent = new GUIContent("Table View");
		}
		void OnGUI()
		{
			selectedButtonStyle = new(GUI.skin.button) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.5f, 0.7f, 1) } };

			TableViewSetting settings = TableViewSetting.Instance;
			if (settings == null)
			{
				EditorGUILayout.LabelField("TableViewSetting not found! Please create one in the project.");
				if (GUILayout.Button("Create TableViewSetting"))
				{
					settings = CreateInstance<TableViewSetting>();
					AssetDatabase.CreateAsset(settings, "Assets/TableViewSetting.asset");
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
				return;
			}
			settings.CleanupSetting();

			soPic = soPic != null ? soPic : EditorGUIUtility.IconContent("ScriptableObject Icon").image;
			mbPic = mbPic != null ? mbPic : EditorGUIUtility.IconContent("cs Script Icon").image;
			goPic = goPic != null ? goPic : EditorGUIUtility.IconContent("GameObject Icon").image;
			prPic = prPic != null ? prPic : EditorGUIUtility.IconContent("Prefab Icon").image;
			newPic = newPic != null ? newPic : EditorGUIUtility.IconContent("CreateAddNew").image;

			bool hasSelected = settings.TryGetSelectedType(out TableViewSetting_Type selectedTypeSetting);
			Type selectedType = hasSelected ? selectedTypeSetting.ObjectType : null;

			Rect fullWindowRect = position;
			fullWindowRect.position = Vector2.zero;
			DraWindowHeader(settings.AllTypeInfo, selectedType, settings, ref fullWindowRect);

			if (hasSelected)
				DrawObjectClass(settings, selectedTypeSetting, ref fullWindowRect);
		}

		static void DraWindowHeader(IReadOnlyList<TableViewSetting_Type> types, Type selected, TableViewSetting settings, ref Rect fullWindowRect)
		{
			const float actionButtonWidth = 26;
			const float lineHight = 22;
			const float buttonWidthExtra = 100;

			float fullHeaderWidth = 0;
			for (int i = 0; i < types.Count; i++)
			{
				TableViewSetting_Type typeDisplaySetting = types[i];
				Type type = typeDisplaySetting.ObjectType;
				string name = type.Name;
				float width = GetButtonWidth(name);
				fullHeaderWidth += width;
			}

			Rect headerRect = fullWindowRect;
			headerRect.SliceOut(0, Side.Up, addSpace: true);
			headerRect.SliceOut(0, Side.Left, addSpace: true);
			Rect actionButtonsRect = headerRect.SliceOut(actionButtonWidth * 5 + spacing * 5, Side.Right);
			actionButtonsRect.height = lineHight;
			float fullWindowWidth = fullWindowRect.width;
			float availableWidth = headerRect.width;
			Rect backgroundRect = new(0, 0, fullWindowWidth, lineHight + spacing * 2);

			EditorHelper.DrawBox(backgroundRect, EditorHelper.buttonBackgroundColor);

			TableViewSetting_Type selectedTypeSetting = types.FirstOrDefault(x => x.ObjectType == selected);

			if (selected == null)
				GUI.enabled = false;

			// Show Prefabs / GameObjects


			Rect refreshRect = actionButtonsRect.SliceOut(actionButtonWidth, Side.Left);

			// Refresh button
			if (GUI.Button(refreshRect, new GUIContent("↻", "Refresh")))
				TableViewCache.ClearCache();

			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent("◄", "Step Left Selected Tab")))
				settings.MovePinnedTab(selectedTypeSetting, false);

			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent("►", "Step Right Selected Tab")))
				settings.MovePinnedTab(selectedTypeSetting, true);

			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent("✖", "Remove Selected Tab")))
				settings.RemovePinnedTab(selectedTypeSetting);

			GUI.enabled = true;
			if (GUI.Button(actionButtonsRect.SliceOut(actionButtonWidth, Side.Left), new GUIContent(newPic, "Add New Type Tab")))
			{
				Type selectedType = TypeSelectEditorWindow.Open();
				if (selectedType != null)
					settings.SetSelectedType(selectedType);
			}

			float x = headerRect.x;
			float y = headerRect.y;
			float fullHeaderHeight = lineHight;

			for (int i = 0; i < types.Count; i++)
			{
				TableViewSetting_Type typeDisplaySetting = types[i];
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
				List<Object> objects = TableViewCache.GetObjectsByType(type, typeDisplaySetting.showPrefabs);
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

		void DrawObjectClass(TableViewSetting settings, TableViewSetting_Type typeDisplaySetting, ref Rect fullWindowRect)
		{
			EditorGUILayout.Space(2);
			fullWindowRect.SliceOut(4, Side.Down, false);
			TableViewCache.CleanupObjectCache();

			List<Object> objects = TableViewCache.GetObjectsByType(typeDisplaySetting.ObjectType, typeDisplaySetting.showPrefabs);
			TableViewDrawer customTableDrawer = TableViewCache.GetCustomDrawer(typeDisplaySetting.ObjectType);
			objects.Sort(customTableDrawer.Compare);

			// Calculate Header Size 
			List<ObjectsBrowserColumn> columns = new();
			float headerHeight = SingleLineHeight;
			float fullWidth = typeDisplaySetting.nameWidth;
			if (objects.Count > 0)
			{
				columns = FindColumns(objects, typeDisplaySetting, customTableDrawer);
				// Calculate header height
				foreach (ObjectsBrowserColumn column in columns)
				{
					float labelWidth = GUI.skin.label.CalcSize(new GUIContent(column.label)).x;
					if (labelWidth > column.width)
						headerHeight = MathF.Max(headerHeight, labelWidth);
					fullWidth += column.width + spacing;
				}
			}

			// Draw Header
			Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Width(fullWidth), GUILayout.Height(headerHeight));
			headerRect.x -= scrollPosition.x;
			GUIStyle centered = new(GUI.skin.label) { alignment = TextAnchor.LowerCenter };

			// Draw Main Action
			Rect headerActionRect = headerRect.SliceOut(typeDisplaySetting.nameWidth, Side.Left);
			headerActionRect = headerActionRect.SliceOut(SingleLineHeight, Side.Down, false);
			Type selectedType = typeDisplaySetting.ObjectType;
			if (selectedType == null) return;

			if (selectedType.IsSubclassOf(typeof(MonoBehaviour)))
			{
				// Prefab / GameObject Switch
				bool showPrefabs = typeDisplaySetting.showPrefabs;
				GUIContent content = showPrefabs ?
					new(" Prefabs", prPic, "Show Prefab files in Project") :
					new(" GameObjects", goPic, "Show GameObjects In Scene");

				if (GUI.Button(headerActionRect, content))
					typeDisplaySetting.showPrefabs = !showPrefabs;
			}
			else if (selectedType.IsSubclassOf(typeof(ScriptableObject)))
			{
				// Create New SO Button
				DrawAddNewItemButton(headerActionRect, typeDisplaySetting);
			}

			// Draw First Resizer
			Rect resizeNameRect = headerRect.SliceOut(2, Side.Left, false);
			float newNameW = DrawColumnResizer(resizeNameRect, -1, typeDisplaySetting.nameWidth, minWidth: 100);
			if (newNameW != typeDisplaySetting.nameWidth)
			{
				typeDisplaySetting.nameWidth = newNameW;
				settings.SetSODirty();
				Repaint();
			}

			// Draw Draw Columns
			for (int i = 0; i < columns.Count; i++)
			{
				ObjectsBrowserColumn column = columns[i];
				Rect rect = headerRect.SliceOut(column.width, Side.Left, false);
				float labelWidth = GUI.skin.label.CalcSize(new GUIContent(column.label)).x;
				rect.x -= 2;
				if (labelWidth > column.width)
				{
					Rect r = new(0, 0, rect.height, rect.width);
					r.center = rect.center;
					headerHeight = MathF.Max(headerHeight, labelWidth);

					Vector2 pivot = rect.center;
					GUIUtility.RotateAroundPivot(-90, pivot);
					EditorGUI.LabelField(r, column.label);
					GUIUtility.RotateAroundPivot(90, pivot);
				}
				else
				{
					EditorGUI.LabelField(rect, column.label, centered);
				}

				// Resizer
				Rect resizeRect = (i == columns.Count - 1) ? rect.SliceOut(3, Side.Right, false) : headerRect.SliceOut(3, Side.Left, false);
				float newW = DrawColumnResizer(resizeRect, i, column.width, minWidth: 16);
				if (newW != column.width)
				{
					typeDisplaySetting.SetColumnWidth(typeDisplaySetting.fullTypeName, column.propertyName, newW);
					settings.SetSODirty();
					Repaint();
				}
			}
			fullWindowRect.SliceOut(headerHeight, Side.Up, addSpace: false);

			// Draw objects

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(fullWindowRect.width), GUILayout.Height(fullWindowRect.height));
			for (int objI = 0; objI < objects.Count; objI++)
			{
				Object obj = objects[objI];
				if (obj == null) continue;

				EditorGUILayout.BeginHorizontal();
				SerializedObject serializedObject = new(obj);

				Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(typeDisplaySetting.nameWidth));
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
						customTableDrawer.DrawProperty(rect, obj, property, column.propertyName);
					}
				}

				serializedObject.ApplyModifiedProperties();
				EditorGUILayout.EndHorizontal();

				if (openedObjects.Contains(obj))
					DrawFullObject(obj);
			}

			EditorGUILayout.EndScrollView();
		}

		float DrawColumnResizer(Rect resizeRect, int index, float columnW, float minWidth)
		{
			resizeRect = resizeRect.SliceOut(SingleLineHeight, Side.Down, false);
			resizeRect.x -= 2;
			resizeRect.width += 1;

			EditorGUI.DrawRect(resizeRect, new Color(0, 0, 0, 0.2f));
			Vector2 mouse = Event.current.mousePosition;
			if (Event.current.type == EventType.MouseDown && resizeRect.Contains(mouse))
			{
				resizedColumn = index;
				lastMouseX = mouse.x;
			}
			else if (resizedColumn == index && lastMouseX != mouse.x)
			{
				columnW = columnW + mouse.x - lastMouseX;
				columnW = MathF.Max(columnW, minWidth);
				lastMouseX = mouse.x;
			}
			else if (Event.current.type == EventType.MouseUp)
				resizedColumn = -2;

			return columnW;
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

		static List<ObjectsBrowserColumn> FindColumns(List<Object> objects, TableViewSetting_Type setting, TableViewDrawer customTableDrawer)
		{

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

					if (!customTableDrawer.ShowScript() && pName == "m_Script")
						continue;

					if (!customTableDrawer.ShowProperty(pName))
					{
						continue;
					}

					if (index < 0)
					{
						GUIContent label = customTableDrawer.GetTitle(pName, property.displayName);

						float width;
						if (customTableDrawer.OverridePropertyWidth(pName, out float w))
							width = w;
						else if (setting.TryGetColumnSettings(setting.fullTypeName, pName, out TableViewSetting_Column pds))
							width = pds.width;
						else
							width = GetDefaultPropertyWidthByType(property.propertyType);

						float priority = customTableDrawer.GetColumPriority(pName, propI);

						ObjectsBrowserColumn column = new() { propertyName = pName, label = label, width = width, priority = priority };
						columns.Add(column);
					}
				}
			}

			columns.Sort((a, b) => a.priority.CompareTo(b.priority));
			return columns;
		}

		static float GetDefaultPropertyWidthByType(SerializedPropertyType type) => type switch
		{

			SerializedPropertyType.String => 200,
			SerializedPropertyType.Integer => 50,
			SerializedPropertyType.Float => 50,
			SerializedPropertyType.Boolean => 16,
			SerializedPropertyType.Vector2 => 100,
			SerializedPropertyType.Vector3 => 150,
			SerializedPropertyType.Vector4 => 200,
			SerializedPropertyType.Vector2Int => 100,
			SerializedPropertyType.Vector3Int => 150,
			SerializedPropertyType.Quaternion => 200,
			SerializedPropertyType.Color => 50,
			SerializedPropertyType.Rect => 100,
			SerializedPropertyType.Bounds => 100,
			SerializedPropertyType.AnimationCurve => 100,
			SerializedPropertyType.ObjectReference => 200,
			_ => 200
		};


		static void DrawItemName(Object obj, Rect rect)
		{
			Rect FoldoutRect = rect.SliceOut(14, Side.Left);
			Rect selectButtonRect = rect.SliceOut(20, Side.Right);

			bool isOpened = openedObjects.Contains(obj);
			if (EditorGUI.Foldout(FoldoutRect, isOpened, GUIContent.none) != isOpened)
			{
				if (isOpened)
					openedObjects.Remove(obj);
				else
					openedObjects.Add(obj);
			}

			bool isSelected = Selection.activeObject == obj;
			GUIContent selectButtonContent = new("→", "Select this");
			GUIStyle selectButtonStyle = isSelected ? selectedButtonStyle : GUI.skin.button;
			if (GUI.Toggle(selectButtonRect, isSelected, selectButtonContent, selectButtonStyle) != isSelected)
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

		static void DrawAddNewItemButton(Rect rect, TableViewSetting_Type typeSetting)
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
			GUIContent buttonString = pathExists ?
				new("Create New", $"Create new {typeSetting.ObjectType} here: {_savePath}") :
				new("Create New", "Open a directory or select a file");

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
			TableViewCache.AddInstance(scriptableObjectType, newId);
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