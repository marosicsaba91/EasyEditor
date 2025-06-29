#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	public static class TableViewCache
	{
		static List<Type> allScriptableObjectTypeCached;
		static List<Type> allMonoBehaviourTypesCached;
		static readonly Dictionary<Type, List<Type>> subTypeCache = new();

		static readonly Dictionary<Type, List<Object>> scriptableObjectCache = new();
		static readonly Dictionary<Type, List<Object>> monoBehaviourScenesCache = new();
		static readonly Dictionary<Type, List<Object>> monoBehaviourPrefabsCache = new();

		static readonly Dictionary<Type, TableViewDrawer> customDrawerCache = new();

		public static List<Type> GetAllScriptableTypes()
		{
			if (allScriptableObjectTypeCached == null)
				AnaliseAndCacheAppDomain();
			return allScriptableObjectTypeCached;
		}

		public static List<Type> GetAllMonoBehaviourTypes()
		{
			if (allMonoBehaviourTypesCached == null)
				AnaliseAndCacheAppDomain();
			return allMonoBehaviourTypesCached;
		}

		public static void CleanupObjectCache()
		{
			CleanupDictionary(scriptableObjectCache);
			CleanupDictionary(monoBehaviourScenesCache);
			CleanupDictionary(monoBehaviourPrefabsCache);
		}

		static void CleanupDictionary(Dictionary<Type, List<Object>> dictionary)
		{
			foreach ((Type _, List<Object> objects) in dictionary)
				for (int i = objects.Count - 1; i >= 0; i--)
					if (objects[i] == null)
						objects.RemoveAt(i);
		}

		public static List<Type> GetAllNonAbstractSubclassesOf(Type baseType)
		{
			if (allMonoBehaviourTypesCached == null)
				AnaliseAndCacheAppDomain();

			if (!subTypeCache.TryGetValue(baseType, out List<Type> subTypes))
			{
				subTypes = new();
				List<Type> list;
				if (baseType.IsSubclassOf(typeof(ScriptableObject)))
					list = allScriptableObjectTypeCached;
				else if (baseType.IsSubclassOf(typeof(MonoBehaviour)))
					list = allMonoBehaviourTypesCached;
				else
					return null;

				foreach (Type type in list)
					if (baseType.IsAssignableFrom(type) && !type.IsAbstract)
						subTypes.Add(type);

				subTypeCache.Add(baseType, subTypes);
			}
			return subTypes;
		}


		static void AnaliseAndCacheAppDomain()
		{
			allScriptableObjectTypeCached = new();
			allMonoBehaviourTypesCached = new();

			Type customDrawerType = typeof(TableViewDrawer);

			foreach (Assembly assembly in GetUserCreatedAssemblies(AppDomain.CurrentDomain))
				foreach (Type type in assembly.GetTypes())
				{
					if (type.IsGenericType) continue;
					if (typeof(EditorWindow).IsAssignableFrom(type)) continue;
					if (typeof(UnityEditor.Editor).IsAssignableFrom(type)) continue;

					if (type.IsSubclassOf(typeof(ScriptableObject)))
						allScriptableObjectTypeCached.Add(type);
					else if (type.IsSubclassOf(typeof(MonoBehaviour)))
						allMonoBehaviourTypesCached.Add(type);

					if (customDrawerType.IsAssignableFrom(type) && !type.IsAbstract && type.BaseType.IsGenericType)
					{
						Type genericParameter = type.BaseType.GetGenericArguments()[0];
						if (!customDrawerCache.ContainsKey(genericParameter))
							customDrawerCache.Add(genericParameter, (TableViewDrawer)Activator.CreateInstance(type));
					}
				}
		}

		public static IEnumerable<Assembly> GetUserCreatedAssemblies(AppDomain appDomain)
		{
			foreach (Assembly assembly in appDomain.GetAssemblies())
			{
				if (assembly.IsDynamic) continue;

				string assemblyName = assembly.GetName().Name;
				if (assemblyName.Contains("Unity")) continue;

				yield return assembly;
			}
		}

		public static List<Object> GetObjectsByType(Type type, bool preferFiles)
		{
			if (type.IsSubclassOf(typeof(ScriptableObject)))
				return GetScriptableObjectsByType(type);
			else if (type.IsSubclassOf(typeof(MonoBehaviour)))
				return preferFiles ? GetPrefabMonoBehavioursByType(type) : GetInSceneMonoBehavioursByType(type);
			else
				return null;
		}

		public static List<Object> GetScriptableObjectsByType(Type type)
		{
			if (!scriptableObjectCache.TryGetValue(type, out List<Object> objects))
			{
				objects = FindAllScriptableObjectInstances(type);
				scriptableObjectCache.Add(type, objects);
			}

			return objects;
		}

		static List<Object> GetInSceneMonoBehavioursByType(Type type)
		{
			if (!monoBehaviourScenesCache.TryGetValue(type, out List<Object> objects))
			{
				objects = Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
				monoBehaviourScenesCache.Add(type, objects);
			}

			return objects;
		}

		static List<Object> GetPrefabMonoBehavioursByType(Type type)
		{
			if (!monoBehaviourPrefabsCache.TryGetValue(type, out List<Object> prefabs))
			{
				prefabs = FindAllPrefabInstances(type);
				monoBehaviourPrefabsCache.Add(type, prefabs);
			}

			return prefabs;
		}

		static List<Object> FindAllPrefabInstances(Type type)
		{
			string[] guids = AssetDatabase.FindAssets("t:GameObject");
			List<Object> list = new();
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				Object mb = go.GetComponent(type);
				if (mb != null)
					list.Add(mb);
			}

			return list;
		}

		static List<Object> FindAllScriptableObjectInstances(Type type)
		{
			string[] guids = AssetDatabase.FindAssets("t:" + type.Name);
			List<Object> list = new();
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				ScriptableObject id = (ScriptableObject)AssetDatabase.LoadAssetAtPath(path, type);
				list.Add(id);
			}

			return list;
		}

		public static void AddInstance(Type idType, Object newId)
		{
			while (idType != typeof(ScriptableObject) && idType != typeof(MonoBehaviour) && idType != typeof(object))
			{
				if (!scriptableObjectCache.TryGetValue(idType, out List<Object> identifiers))
				{
					identifiers = new();
					scriptableObjectCache.Add(idType, identifiers);
				}
				identifiers.Add(newId);
				idType = idType.BaseType;
			}
		}

		public static void RemoveInstance(Type idType, Object identifier)
		{
			if (!scriptableObjectCache.TryGetValue(idType, out List<Object> identifiers))
				return;

			identifiers.Remove(identifier);
		}

		public static void ClearCache()
		{
			scriptableObjectCache.Clear();
			monoBehaviourScenesCache.Clear();
			monoBehaviourPrefabsCache.Clear();
		}

		public static TableViewDrawer GetCustomDrawer(Type objectType) =>
			customDrawerCache.TryGetValue(objectType, out TableViewDrawer drawer) ? drawer : null;
	}
}
#endif