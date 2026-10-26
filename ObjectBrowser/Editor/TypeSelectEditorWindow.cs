#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	[AutoStaticsCleanup]
	partial class TypeSelectEditorWindow : EditorWindow
	{
		public Type selectedType;
		string _search = string.Empty;
		bool _scriptableObjects = true;
		bool _monoBehaviours = true;
		Vector2 _scrollPosition;

		static Texture _soPic;
		static Texture _mbPic;

		Type _baseType = null;

		public static Type Open(Type baseType = null)
		{
			TypeSelectEditorWindow window = CreateInstance<TypeSelectEditorWindow>();
			window._baseType = baseType;

			_soPic = _soPic != null ? _soPic : EditorGUIUtility.IconContent("ScriptableObject Icon").image;
			_mbPic = _mbPic != null ? _mbPic : EditorGUIUtility.IconContent("cs Script Icon").image;

			window.ShowModal();
			return window.selectedType;
		}

		List<Type> _types;

		void OnGUI()
		{
			// Search field
			if (_baseType == null)
			{
				EditorGUILayout.BeginHorizontal();
				bool so = GUILayout.Toggle(_scriptableObjects, "ScriptableObjects", EditorStyles.toolbarButton);
				bool mb = GUILayout.Toggle(_monoBehaviours, "MonoBehaviours", EditorStyles.toolbarButton);
				if (so != _scriptableObjects || mb != _monoBehaviours)
				{
					_scriptableObjects = so;
					_monoBehaviours = mb;
					_types = UpdateTypes();
				}
				EditorGUILayout.EndHorizontal();
			}

			string searchResult = EditorGUILayout.TextField("Search", _search);
			if (searchResult != _search)
			{
				_search = searchResult;
				_types = UpdateTypes();
			}
			_types ??= UpdateTypes();

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			for (int i = 0; i < _types.Count; i++)
			{
				Type type = _types[i];
				Texture icon = type.IsSubclassOf(typeof(ScriptableObject)) ? _soPic : _mbPic;
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
			if (_baseType != null)
				types = TableViewCache.GetAllNonAbstractSubclassesOf(_baseType);
			else
			{
				if (_scriptableObjects)
					types.AddRange(TableViewCache.GetAllScriptableTypes());
				if (_monoBehaviours)
					types.AddRange(TableViewCache.GetAllMonoBehaviourTypes());
			}

			if (!string.IsNullOrEmpty(_search))
				types = types.FindAll(x => x.Name.ToLower().Contains(_search.ToLower()));

			types.Sort((t1, t2) => string.Compare(t1.Name, t2.Name, StringComparison.Ordinal));
			return types;
		}
	}
}

#endif