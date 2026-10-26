#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; 
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assemblies;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	[AutoStaticsCleanup]
	public static partial class TableViewCache
	{
		static List<Type> _allScriptableObjectTypeCached;
		static List<Type> _allMonoBehaviourTypesCached;
		static readonly Dictionary<Type, List<Type>> _subTypeCache = new();

		static readonly Dictionary<Type, List<Object>> _scriptableObjectCache = new();
		static readonly Dictionary<Type, List<Object>> _monoBehaviourScenesCache = new();
		static readonly Dictionary<Type, List<Object>> _monoBehaviourPrefabsCache = new();

		static readonly Dictionary<Type, TableViewDrawer> _customDrawerCache = new();
		static TableViewDrawer _dummyDrawer;

		public static List<Type> GetAllScriptableTypes()
		{
			if (_allScriptableObjectTypeCached == null)
				AnaliseAndCacheAppDomain();
			return _allScriptableObjectTypeCached;
		}

		public static List<Type> GetAllMonoBehaviourTypes()
		{
			if (_allMonoBehaviourTypesCached == null)
				AnaliseAndCacheAppDomain();
			return _allMonoBehaviourTypesCached;
		}

		public static void CleanupObjectCache()
		{
			CleanupDictionary(_scriptableObjectCache);
			CleanupDictionary(_monoBehaviourScenesCache);
			CleanupDictionary(_monoBehaviourPrefabsCache);
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
			if (_allMonoBehaviourTypesCached == null)
				AnaliseAndCacheAppDomain();

			if (!_subTypeCache.TryGetValue(baseType, out List<Type> subTypes))
			{
				subTypes = new();
				List<Type> list;
				if (baseType.IsSubclassOf(typeof(ScriptableObject)))
					list = _allScriptableObjectTypeCached;
				else if (baseType.IsSubclassOf(typeof(MonoBehaviour)))
					list = _allMonoBehaviourTypesCached;
				else
					return null;

				foreach (Type type in list!)
					if (baseType.IsAssignableFrom(type) && !type.IsAbstract)
						subTypes.Add(type);

				_subTypeCache.Add(baseType, subTypes);
			}
			return subTypes;
		}


		static void AnaliseAndCacheAppDomain()
		{
			_allScriptableObjectTypeCached = new();
			_allMonoBehaviourTypesCached = new();

			Type customDrawerType = typeof(TableViewDrawer);

			foreach (Assembly assembly in GetUserCreatedAssemblies(AppDomain.CurrentDomain))
				foreach (Type type in assembly.GetTypes())
				{
					if (type.IsGenericType) continue;
					if (typeof(EditorWindow).IsAssignableFrom(type)) continue;
					if (typeof(UnityEditor.Editor).IsAssignableFrom(type)) continue;

					if (type.IsSubclassOf(typeof(ScriptableObject)))
						_allScriptableObjectTypeCached.Add(type);
					else if (type.IsSubclassOf(typeof(MonoBehaviour)))
						_allMonoBehaviourTypesCached.Add(type);

					if (customDrawerType.IsAssignableFrom(type) && !type.IsAbstract && type.BaseType is { IsGenericType: true })
					{
						Type genericParameter = type.BaseType.GetGenericArguments()[0];
						if (!_customDrawerCache.ContainsKey(genericParameter))
							_customDrawerCache.Add(genericParameter, (TableViewDrawer)Activator.CreateInstance(type));
					}
				}
		}

		public static IEnumerable<Assembly> GetUserCreatedAssemblies(AppDomain appDomain)
		{
			foreach (Assembly assembly in CurrentAssemblies.GetLoadedAssemblies())
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
			if (!_scriptableObjectCache.TryGetValue(type, out List<Object> objects))
			{
				objects = FindAllScriptableObjectInstances(type);
				_scriptableObjectCache.Add(type, objects);
			}

			return objects;
		}

		static List<Object> GetInSceneMonoBehavioursByType(Type type)
		{
			if (!_monoBehaviourScenesCache.TryGetValue(type, out List<Object> objects))
			{
				objects = Object.FindObjectsByType(type, FindObjectsInactive.Exclude).ToList();
				_monoBehaviourScenesCache.Add(type, objects);
			}

			return objects;
		}

		static List<Object> GetPrefabMonoBehavioursByType(Type type)
		{
			if (!_monoBehaviourPrefabsCache.TryGetValue(type, out List<Object> prefabs))
			{
				prefabs = FindAllPrefabInstances(type);
				_monoBehaviourPrefabsCache.Add(type, prefabs);
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
				if (!_scriptableObjectCache.TryGetValue(idType!, out List<Object> identifiers))
				{
					identifiers = new();
					_scriptableObjectCache.Add(idType, identifiers);
				}
				identifiers.Add(newId);
				idType = idType.BaseType;
			}
		}

		public static void RemoveInstance(Type idType, Object identifier)
		{
			if (!_scriptableObjectCache.TryGetValue(idType, out List<Object> identifiers))
				return;

			identifiers.Remove(identifier);

 		}

		public static void ClearCache()
		{
			_scriptableObjectCache.Clear();
			_monoBehaviourScenesCache.Clear();
			_monoBehaviourPrefabsCache.Clear();
		}

		public static TableViewDrawer GetCustomDrawer(Type objectType)
		{
			if (_customDrawerCache.TryGetValue(objectType, out TableViewDrawer drawer))
				return drawer;
			else
			{
				_dummyDrawer ??= new();
				return _dummyDrawer;
			}
		}
	}
}
#endif