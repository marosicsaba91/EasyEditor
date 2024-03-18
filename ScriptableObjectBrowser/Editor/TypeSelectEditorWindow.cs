#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class TypeSelectEditorWindow : EditorWindow
{
	public Type selectedType;
	string search = string.Empty;
	bool scriptableObjects = true;
	bool monoBehaviours = true;
	Vector2 scrollPosition;

	static Texture soPic;
	static Texture mbPic;

	Type baseType = null;

	public static Type Open(Type baseType = null)
	{
		TypeSelectEditorWindow window = CreateInstance<TypeSelectEditorWindow>();
		window.baseType = baseType;

		soPic = soPic != null ? soPic : EditorGUIUtility.IconContent("ScriptableObject Icon").image;
		mbPic = mbPic != null ? mbPic : EditorGUIUtility.IconContent("cs Script Icon").image;

		window.ShowModal();
		return window.selectedType;
	}

	List<Type> types;

	void OnGUI()
	{
		// Search field
		if (baseType == null)
		{
			EditorGUILayout.BeginHorizontal();
			bool so = GUILayout.Toggle(scriptableObjects, "ScriptableObjects", EditorStyles.toolbarButton);
			bool mb = GUILayout.Toggle(monoBehaviours, "MonoBehaviours", EditorStyles.toolbarButton);
			if (so != scriptableObjects || mb != monoBehaviours)
			{
				scriptableObjects = so;
				monoBehaviours = mb;
				types = UpdateTypes();
			}
			EditorGUILayout.EndHorizontal();
		}

		string searchResult = EditorGUILayout.TextField("Search", search);
		if (searchResult != search)
		{
			search = searchResult;
			types = UpdateTypes();
		}
		types ??= UpdateTypes();

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		for (int i = 0; i < types.Count; i++)
		{
			Type type = types[i];
			Texture icon = type.IsSubclassOf(typeof(ScriptableObject)) ? soPic : mbPic;
			GUIContent content = new(" " + type.Name, icon);

			if (GUI.Button(EditorGUILayout.GetControlRect(), content))
			{
				selectedType = type;
				Close();
				break;
			}
		}

		EditorGUILayout.EndScrollView();
	}

	private List<Type> UpdateTypes()
	{
		List<Type> types = new();
		if (baseType != null)
			types = ObjectBrowserCache.GetAllNonAbstractSubclassesOf(baseType);
		else
		{
			if (scriptableObjects)
				types.AddRange(ObjectBrowserCache.GetAllScriptableTypes());
			if (monoBehaviours)
				types.AddRange(ObjectBrowserCache.GetAllMonoBehaviourTypes());
		}

		if (!string.IsNullOrEmpty(search))
			types = types.FindAll(x => x.Name.ToLower().Contains(search.ToLower()));

		types.Sort((t1, t2) => t1.Name.CompareTo(t2.Name));
		return types;
	}
}

#endif