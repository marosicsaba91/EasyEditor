#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	public static class InspectorDrawingUtility
	{
		public const BindingFlags bindings =
			BindingFlags.Instance |
			BindingFlags.Static |
			BindingFlags.Public |
			BindingFlags.NonPublic |
			BindingFlags.FlattenHierarchy;

		/// <summary>
		/// Key is Associated with drawer type (the T in [CustomPropertyDrawer(typeof(T))])
		/// Value is PropertyDrawer Type
		/// </summary>
		static readonly Dictionary<Type, Type> propertyDrawersInAssembly = new();

		static readonly Dictionary<int, PropertyDrawer>
			propertyDrawersCache = new();

		static readonly string ignoreScope = typeof(int).Module.ScopeName;

		/// <summary>
		/// Create PropertyDrawer for specified property if any PropertyDrawerType for such property is found.
		/// FieldInfo and Attribute will be inserted in created drawer.
		/// </summary>
		public static PropertyDrawer GetPropertyDrawerForProperty(SerializedProperty property, FieldInfo fieldInfo,
			Attribute attribute)
		{
			int propertyId = property.GetUniquePropertyId();
			if (propertyDrawersCache.TryGetValue(propertyId, out PropertyDrawer drawer))
				return drawer;

			Type targetType = fieldInfo.FieldType;
			Type drawerType = GetPropertyDrawerTypeForFieldType(targetType);
			if (drawerType != null)
			{
				drawer = InstantiatePropertyDrawer(drawerType, fieldInfo, attribute);

				if (drawer == null)
					Debug.LogWarning(
						$"Unable to instantiate CustomDrawer of type {drawerType} for {fieldInfo.FieldType}",
						property.serializedObject.targetObject);
			}

			propertyDrawersCache[propertyId] = drawer;
			return drawer;
		}

		public static PropertyDrawer InstantiatePropertyDrawer(Type drawerType, FieldInfo fieldInfo, Attribute insertAttribute)
		{
			try
			{
				PropertyDrawer drawerInstance = (PropertyDrawer)Activator.CreateInstance(drawerType);

				// Reassign the attribute and fieldInfo fields in the drawer so it can access the argument values
				FieldInfo fieldInfoField = drawerType.GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic);
				fieldInfoField?.SetValue(drawerInstance, fieldInfo);
				FieldInfo attributeField = drawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);
				attributeField?.SetValue(drawerInstance, insertAttribute);

				return drawerInstance;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Try to get PropertyDrawer for a target Type, or any Base Type for a target Type
		/// </summary>
		public static Type GetPropertyDrawerTypeForFieldType(Type drawerTarget)
		{
			// Ignore .net types from mscorlib.dll
			if (drawerTarget.Module.ScopeName.Equals(ignoreScope))
				return null;
			CacheDrawersInAssembly();

			// Of all property drawers in the assembly we need to find one that affects target type
			// or one of the base types of target type
			Type checkType = drawerTarget;
			while (checkType != null)
			{
				if (propertyDrawersInAssembly.TryGetValue(drawerTarget, out Type drawer))
					return drawer;
				checkType = checkType.BaseType;
			}

			return null;
		}

		static Type[] GetTypesSafe(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types;
			}
		}

		static void CacheDrawersInAssembly()
		{
			if (propertyDrawersInAssembly != null && propertyDrawersInAssembly.Count > 0)
				return;

			Type propertyDrawerType = typeof(PropertyDrawer);
			IEnumerable<Type> allDrawerTypesInDomain = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(GetTypesSafe)
				.Where(t => t != null && propertyDrawerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

			foreach (Type drawerType in allDrawerTypesInDomain)
			{
				CustomAttributeData propertyDrawerAttribute = CustomAttributeData.GetCustomAttributes(drawerType).FirstOrDefault();
				if (propertyDrawerAttribute == null)
					continue;
				Type drawerTargetType = propertyDrawerAttribute.ConstructorArguments.FirstOrDefault().Value as Type;
				if (drawerTargetType == null)
					continue;

				if (propertyDrawersInAssembly.ContainsKey(drawerTargetType))
					continue;
				propertyDrawersInAssembly.Add(drawerTargetType, drawerType);
			}
		}

		public static bool TryGetAGetterFromMember<T>(Type ownerType, string memberName, out Func<object, T> getter)
		{
			if (string.IsNullOrEmpty(memberName))
			{
				getter = default;
				return false;
			}

			if (TryGetFunctionOfType<T>(ownerType, memberName, out MethodInfo methodInfo))
			{
				getter = o => (T)methodInfo.Invoke(o, Array.Empty<object>());
				return true;
			}
			if (TryGetFieldInfoOfType<T>(ownerType, memberName, out FieldInfo fieldInfo))
			{
				getter = o => (T)fieldInfo.GetValue(o);
				return true;
			}
			if (TryGetPropertyInfoOfType<T>(ownerType, memberName, out PropertyInfo propertyInfo))
			{
				getter = o => (T)propertyInfo.GetValue(o);
				return true;
			}

			getter = default;
			return false;
		}

		public static bool TryGetFunctionOfType<T>(Type ownerType, string name, out MethodInfo methodInfo)
		{
			MethodInfo method = ownerType.GetMethod(name, bindings);

			if (method != null)
			{
				Type returnType = method.ReturnType;
				Type expectedType = typeof(T);
				bool isReturnTypeValid =
					returnType == expectedType ||
					returnType.IsSubclassOf(expectedType);

				ParameterInfo[] parameters = method.GetParameters();
				bool hasNoParameters = parameters == null || parameters.Length == 0;

				if (isReturnTypeValid && hasNoParameters)
				{
					methodInfo = method;
					return true;
				}
			}

			methodInfo = null;
			return false;
		}

		public static bool TryGetFieldInfoOfType<T>(Type ownerType, string name, out FieldInfo fieldInfo)
		{
			FieldInfo field = ownerType.GetField(name, bindings);

			if (field != null && field.FieldType == typeof(T))
			{
				fieldInfo = field;
				return true;
			}

			fieldInfo = null;
			return false;
		}

		public static bool TryGetPropertyInfoOfType<T>(Type ownerType, string name, out PropertyInfo propertyInfo)
		{
			PropertyInfo property = ownerType.GetProperty(name, bindings);

			if (property != null && property.PropertyType == typeof(T) && property.GetMethod != null)
			{
				propertyInfo = property;
				return true;
			}

			propertyInfo = null;
			return false;
		}

	}
}
#endif