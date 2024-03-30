#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	public enum ObjectsBrowserView { TableView, ListView }

	class ObjectsBrowserWindow : EditorWindow
	{
		readonly List<Object> openedObjects = new();
		static string _savePath = "ScriptableObjects";

		Texture soPic;
		Texture mbPic;
		Texture goPic;
		Texture prPic;

		Vector2 scrollPosition;
		Vector2 scrollPosition2;
		GUIStyle selectedButtonSt;

		GUIStyle SelectedButtonStyle
		{
			get
			{
				selectedButtonSt ??= new(GUI.skin.button) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.5f, 0.7f, 1) } };
				return selectedButtonSt;
			}
		}

		[MenuItem("Tools/Objects Browser")]
		public static void Open()
		{
			ObjectsBrowserWindow window = GetWindow<ObjectsBrowserWindow>();
			window.titleContent = new GUIContent("Objects Browser");

		}

		void OnGUI()
		{
			ObjectBrowserSetting settings = ObjectBrowserSetting.Instance;
			settings.CleanupSetting();
			IReadOnlyList<TypeDisplaySetting> displayedTypes = settings.GetPinnedTypesInOrder();

			soPic = soPic != null ? soPic : EditorGUIUtility.IconContent("ScriptableObject Icon").image;
			mbPic = mbPic != null ? mbPic : EditorGUIUtility.IconContent("cs Script Icon").image;
			goPic = goPic != null ? goPic : EditorGUIUtility.IconContent("GameObject Icon").image;
			prPic = prPic != null ? prPic : EditorGUIUtility.IconContent("Prefab Icon").image;

			bool hasSelected = settings.TryGetSelectedType(out TypeDisplaySetting selectedTypeSetting);

			Rect fullWindowRect = position;
			fullWindowRect.position = Vector2.zero;
			DrawTypeTabs(displayedTypes, selectedTypeSetting.ObjectType, settings, ref fullWindowRect);
			DrawFooter(selectedTypeSetting.ObjectType, settings, ref fullWindowRect);

			if (hasSelected)
				DrawObjectClass(selectedTypeSetting, settings.ShowPrefabs, settings.SelectedView, ref fullWindowRect);


			ObjectBrowserSetting.TrySave();
		}

		void DrawFooter(Type selected, ObjectBrowserSetting settings, ref Rect fullWindowRect)
		{
			Rect line = fullWindowRect.SliceOut(22, Side.Down);

			EditorHelper.DrawBox(line, EditorHelper.buttonBackgroundColor);
			line.position += new Vector2(0, 2);
			line.height -= 4;

			// Refresh button
			Rect rect = line.SliceOut(110, Side.Right);
			if (GUI.Button(rect, "Refresh"))
				ObjectBrowserCache.ClearCache();

			// View switch
			rect = line.SliceOut(110, Side.Right);
			if (GUI.Button(rect, settings.SelectedView.ToString()))
				settings.SelectedView = settings.SelectedView == ObjectsBrowserView.ListView ? ObjectsBrowserView.TableView : ObjectsBrowserView.ListView;

			// Show Prefabs / GameObjects
			bool isMonoBehaviour = selected != null && selected.IsSubclassOf(typeof(MonoBehaviour));
			if (isMonoBehaviour)
			{
				bool showPrefabs = settings.ShowPrefabs;
				GUIContent content = new(
					showPrefabs ? " Prefabs" : " GameObjects",
					showPrefabs ? prPic : goPic,
					showPrefabs ? "Show Prefab files in Project" : "Show GameObjects In Scene");

				rect = line.SliceOut(110, Side.Right);
				if (GUI.Button(rect, content))
					settings.ShowPrefabs = !showPrefabs;
			}

		}

		void DrawTypeTabs(IReadOnlyList<TypeDisplaySetting> types, Type selected, ObjectBrowserSetting settings, ref Rect fullWindowRect)
		{
			float tabHeight = 34;
			fullWindowRect.SliceOut(tabHeight, Side.Up);
			scrollPosition2 = EditorGUILayout.BeginScrollView(scrollPosition2, GUILayout.Width(position.width), GUILayout.Height(tabHeight));

			EditorGUILayout.BeginHorizontal();

			for (int i = 0; i < types.Count; i++)
			{
				if (i >= types.Count)
					break;
				TypeDisplaySetting typeDisplaySetting = types[i];
				Type type = typeDisplaySetting.ObjectType;
				Texture texture = type.IsSubclassOf(typeof(ScriptableObject)) ? soPic : type.IsSubclassOf(typeof(MonoBehaviour)) ? mbPic : prPic;

				bool isSelected = selected == type;
				List<Object> objects = ObjectBrowserCache.GetObjectsByType(type, settings.ShowPrefabs);
				int count = objects.Count;

				const float actionButtonWidth = 16;
				Rect tabRect = EditorGUILayout.GetControlRect();
				Rect r = tabRect;
				Rect moveLeftRect = r.SliceOut(actionButtonWidth, Side.Left, addSpace: false);
				Rect closeRect = r.SliceOut(actionButtonWidth, Side.Right, addSpace: false);
				Rect moveRightRect = r.SliceOut(actionButtonWidth, Side.Right, addSpace: false);

				GUIStyle labelStyle = GUI.skin.label;
				if (GUI.Button(closeRect, GUIContent.none, labelStyle))
				{
					settings.RemovePinnedTab(typeDisplaySetting);
					EditorGUILayout.EndHorizontal();
					return;
				}
				if (GUI.Button(moveLeftRect, GUIContent.none, labelStyle))
				{
					settings.MovePinnedTab(typeDisplaySetting, false);
					EditorGUILayout.EndHorizontal();
					return;
				}
				if (GUI.Button(moveRightRect, GUIContent.none, labelStyle))
				{
					settings.MovePinnedTab(typeDisplaySetting, true);
					EditorGUILayout.EndHorizontal();
					return;
				}

				if (isSelected)
					GUI.color = new Color(0.8f, 0.8f, 0.8f);
				GUIStyle style = isSelected ? SelectedButtonStyle : GUI.skin.button;
				if (GUI.Button(tabRect, new GUIContent($" {type.Name} ({count})", texture), style))
				{
					settings.SetSelectedType(type);
				}
				GUI.color = Color.white;
				GUI.Label(closeRect, "✖");
				GUI.Label(moveLeftRect, "◄");
				GUI.Label(moveRightRect, "►");
			}

			if (GUILayout.Button($"+", GUILayout.Width(30)))
			{
				EditorGUILayout.EndHorizontal();
				Type selectedType = TypeSelectEditorWindow.Open();
				if (selectedType != null)
					settings.SetSelectedType(selectedType);
			}
			else

				EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndScrollView();

		}

		const float foldoutWidth = 15;
		const float spacing = 2;

		void DrawObjectClass(TypeDisplaySetting typeDisplaySetting, bool preferFiles, ObjectsBrowserView selectedView, ref Rect fullWindowRect)
		{
			ObjectBrowserCache.CleanupObjectCache();
			List<Object> objects = ObjectBrowserCache.GetObjectsByType(typeDisplaySetting.ObjectType, preferFiles);

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(fullWindowRect.width), GUILayout.Height(fullWindowRect.height));
			if (selectedView == ObjectsBrowserView.ListView)
				DrawListView(typeDisplaySetting, objects);
			else if (selectedView == ObjectsBrowserView.TableView)
				DrawTableView(typeDisplaySetting, objects);

			Type selected = typeDisplaySetting.ObjectType;
			bool isScriptableObject = selected != null && selected.IsSubclassOf(typeof(ScriptableObject));
			if (isScriptableObject)
				DrawAddNewItemButton(EditorGUILayout.GetControlRect(), typeDisplaySetting);
			EditorGUILayout.EndScrollView();
		}

		void DrawListView(TypeDisplaySetting typeDisplaySetting, List<Object> objects)
		{
			for (int i = 0; i < objects.Count; i++)
			{
				Object so = objects[i];
				if (so == null)
				{
					Type type = typeDisplaySetting.ObjectType;
					ObjectBrowserCache.RemoveInstance(type, so);
					i--;
				}
				else
					DrawFoldObject(objects[i]);
			}
		}

		void DrawFoldObject(Object obj)
		{
			bool isOpen = openedObjects.Contains(obj);
			Rect rect = EditorGUILayout.GetControlRect();
			Rect foldoutRect = new(0, rect.y, foldoutWidth, rect.height);
			bool shouldOpen = EditorGUI.Foldout(foldoutRect, isOpen, GUIContent.none);
			if (shouldOpen && !isOpen)
				openedObjects.Add(obj);
			else if (!shouldOpen && isOpen)
				openedObjects.Remove(obj);

			Rect nameRect = new(foldoutWidth + spacing, rect.y, rect.width - (foldoutWidth + spacing), rect.height);
			DrawItemName(obj, nameRect, false);

			if (isOpen)
			{
				EditorGUI.indentLevel++;
				SerializedObject serializedObject = new(obj);

				// Draw SerializedObject
				serializedObject.Update();
				SerializedProperty property = serializedObject.GetIterator();
				for (int i = 0; property.NextVisible(true); i++)
					EditorGUILayout.PropertyField(property, true);

				EditorGUILayout.Space();
				EditorGUI.indentLevel--;
			}
		}

		struct Column
		{
			public string propertyName;
			public string label;
			public float width;
		}

		void DrawTableView(TypeDisplaySetting typeDisplaySetting, List<Object> objects)
		{
			Rect headerRect = EditorGUILayout.GetControlRect();

			if (objects.Count != 0)
			{
				List<Column> columns = FindColumns(objects, typeDisplaySetting);

				const float nameWidth = 200;
				for (int objI = 0; objI < objects.Count; objI++)
				{
					EditorGUILayout.BeginHorizontal();
					Object obj = objects[objI];
					if (obj == null)
					{

						EditorGUILayout.EndHorizontal();
						continue;
					}

					SerializedObject serializedObject = new(obj);

					Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(nameWidth));
					DrawItemName(obj, rect, true);

					float maxHeigh = 0;
					SerializedProperty[] properties = new SerializedProperty[columns.Count];
					for (int i = 0; i < columns.Count; i++)
					{
						Column column = columns[i];
						SerializedProperty prop = serializedObject.FindProperty(column.propertyName);
						properties[i] = prop;
						if (prop != null)
						{
							float height = EditorGUI.GetPropertyHeight(prop, GUIContent.none, true);
							if (height > maxHeigh)
								maxHeigh = height;
						}
					}

					for (int i = 0; i < columns.Count; i++)
					{
						Column column = columns[i];
						rect = EditorGUILayout.GetControlRect(
							GUILayout.Width(column.width),
							GUILayout.Height(maxHeigh));

						SerializedProperty prop = properties[i];
						if (prop != null)
							EditorGUI.PropertyField(rect, prop, GUIContent.none, true);
					}
					serializedObject.ApplyModifiedProperties();

					EditorGUILayout.EndHorizontal();
				}

				// Draw header
				Rect viewRect = new(headerRect.x, headerRect.y, nameWidth, headerRect.height);
				headerRect.x += nameWidth + EditorGUIUtility.standardVerticalSpacing;

				for (int i = 0; i < columns.Count; i++)
				{
					Column column = columns[i];
					Rect rect = new(headerRect.x, headerRect.y, column.width, headerRect.height);
					EditorGUI.LabelField(rect, column.label);
					headerRect.x += column.width + EditorGUIUtility.standardVerticalSpacing;
				}
			}
		}

		private static List<Column> FindColumns(List<Object> objects, TypeDisplaySetting setting)
		{
			float defaultPropertyWidth = 200;
			List<Column> columns = new();
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
					if (index < 0)
					{
						float width = defaultPropertyWidth;
						if (setting.TryGetProperty(pName, out PropertyDisplaySetting pds))
							width = pds.width;
						Column column = new() { propertyName = pName, label = property.displayName, width = width };
						columns.Add(column);
					}
				}
			}

			return columns;
		}

		static void DrawItemName(Object obj, Rect rect, bool selectButtonFirst)
		{
			const float selectButtonWidth = 20;
			Rect nameRect, selectButtonRect;
			if (selectButtonFirst)
			{
				selectButtonRect = new(rect.x, rect.y, selectButtonWidth, rect.height);
				nameRect = new(rect.x + selectButtonWidth + spacing, rect.y, rect.width - (selectButtonWidth + spacing), rect.height);
			}
			else
			{
				nameRect = new(rect.x, rect.y, rect.width - (selectButtonWidth + spacing), rect.height);
				selectButtonRect = new(rect.xMax - selectButtonWidth, rect.y, selectButtonWidth, rect.height);
			}

			string newName = EditorGUI.TextField(nameRect, obj.name);
			if (newName != obj.name)
			{
				EditorUtility.SetDirty(obj);
				string fullPath = AssetDatabase.GetAssetPath(obj);
				AssetDatabase.RenameAsset(fullPath, newName);
				obj.name = newName;
			}

			if (GUI.Button(selectButtonRect, "→"))
				Selection.activeObject = obj;
		}

		void DrawAddNewItemButton(Rect rect, TypeDisplaySetting typeSetting)
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
			string warning = "< Select a folder or file to add path! >";
			string buttonString = $"Create New:   {(pathExists ? _savePath : warning)}";

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