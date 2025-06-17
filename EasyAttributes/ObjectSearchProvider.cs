#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EasyEditor.Internal
{
	public class ObjectSearchProvider : ScriptableObject, ISearchWindowProvider
	{
		Type _baseType;
		SerializedProperty _property;

		readonly List<Type> _concreteMBTypes = new();
		readonly List<Type> _concreteSOTypes = new();

		public void Setup(Type baseType, SerializedProperty property)
		{
			_property = property;

			if (!Equals(baseType, _baseType))
			{
				_concreteSOTypes.Clear();
				Type soType = typeof(ScriptableObject);
				Type mbType = typeof(MonoBehaviour);

				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					foreach (Type type in assembly.GetTypes())
						if (type != null && type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
						{
							if (soType.IsAssignableFrom(type))
								_concreteSOTypes.Add(type);
							else if (mbType.IsAssignableFrom(type))
								_concreteMBTypes.Add(type);
						}

				_baseType = baseType;
			}
		}

		public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
		{
			List<(string, Object)> items = new();

			// ScriptableObjects In Assets		
			foreach (Type t in _concreteSOTypes)
				foreach (string guid in AssetDatabase.FindAssets($"t:{t}"))
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
					items.Add((path, asset));
				}

			// MonoBehaviours In Scene
			foreach (Type concrete in _concreteMBTypes)
				foreach (Object obj in FindObjectsByType(concrete, FindObjectsInactive.Include, FindObjectsSortMode.None))
				{
					MonoBehaviour mb = obj as MonoBehaviour;
					Transform tr = mb.transform;
					string path = tr.name + " : " + obj.GetType().Name;
					while (tr.parent != null)
					{
						tr = tr.parent;
						path = tr.name + "/" + path;
					}
					string sceneName = mb.gameObject.scene.name;
					path = sceneName + "/" + path;

					items.Add((path, obj));
				}

			// Generate SearchTree
			List<SearchTreeEntry> list = new() { new SearchTreeGroupEntry(new GUIContent(_baseType.Name), 0) };
			List<string> groups = new();
			foreach ((string path, Object obj) in items)
			{
				string[] pathSteps = path.Split('/');
				string groupName = "";
				for (int i = 0; i < pathSteps.Length - 1; i++)
				{
					groupName += pathSteps[i];
					if (!groups.Contains(pathSteps[i]))
					{
						list.Add(new SearchTreeGroupEntry(new GUIContent(pathSteps[i]), i + 1));
						groups.Add(pathSteps[i]);
					}
					groupName += "/";
				}

				SearchTreeEntry entry = new(new GUIContent(pathSteps[^1], EditorGUIUtility.ObjectContent(obj, obj.GetType()).image))
				{
					level = pathSteps.Length,
					userData = obj
				};

				list.Add(entry);
			}

			return list;
		}


		public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
		{
			_property.objectReferenceValue = (Object)SearchTreeEntry.userData;
			_property.serializedObject.ApplyModifiedProperties();
			return true;
		}
	}
}
#endif