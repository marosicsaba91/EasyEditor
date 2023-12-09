#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyInspector
{
	public static class SerializedPropertyExtensions
	{
		public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
		{
			property = property.Copy();
			SerializedProperty nextElement = property.Copy();
			bool hasNextElement = nextElement.NextVisible(false);
			if (!hasNextElement)
			{
				nextElement = null;
			}

			property.NextVisible(true);
			while (true)
			{
				if (SerializedProperty.EqualContents(property, nextElement))
				{
					yield break;
				}

				yield return property;

				bool hasNext = property.NextVisible(false);
				if (!hasNext)
				{
					break;
				}
			}
		}

		public static void Record(this SerializedProperty property)
		{
			Object targetObject = property.serializedObject.targetObject;
			Undo.RecordObject(targetObject, $"{property.name} has changed on {targetObject.name}");
		}

		public static Type GetTargetType(this SerializedObject obj)
		{
			if (obj == null)
				return null;

			if (obj.isEditingMultipleObjects)
			{
				Object c = obj.targetObjects[0];
				return c.GetType();
			}

			return obj.targetObject.GetType();
		}

		/// Gets the object the property represents.
		public static object GetObjectOfProperty(this SerializedProperty prop)
		{
			if (prop == null)
				return null;

			string path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			string[] elements = path.Split('.');
			foreach (string element in elements)
			{
				if (TryDecomposeIndexedName(element, out int index, out string elementName))
					obj = GetValue_(obj, elementName, index);
				else
					obj = GetValue_(obj, element);
			}

			return obj;
		}

		// Sets value from SerializedProperty - even if value is nested
		const BindingFlags mask = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		public static void SetValue(this SerializedProperty property, object newValue, string actionName = null)
		{
			List<(object containingObject, FieldInfo field, int index)> parentAndFields = new();
			object containingObject = property.serializedObject.targetObject;

			string propertyPath = property.propertyPath;
			propertyPath = propertyPath.Replace(".Array.data[", "[");
			string[] path = propertyPath.Split('.');
			foreach (string element in path)
			{
				Type containingType = containingObject.GetType();
				if (!TryDecomposeIndexedName(element, out int index, out string elementName))
					elementName = element;

				FieldInfo field = containingType.GetField(elementName, mask);
				parentAndFields.Add((containingObject, field, index));
				containingObject = index >= 0
					? GetValue_(containingObject, field, index)
					: field.GetValue(containingObject);
			}

			Object target = property.serializedObject.targetObject;
			Undo.RecordObject(target, actionName ?? "Property Changed");
			bool changed = false;

			for (int i = parentAndFields.Count - 1; i >= 0; --i)
			{
				FieldInfo field = parentAndFields[i].field;
				object containerObject = parentAndFields[i].containingObject;
				int index = parentAndFields[i].index;

				if (index >= 0)
					changed |= TrySetValue_(containerObject, field, index, newValue);
				else
					changed |= TrySetValue_(containerObject, field, newValue);
				newValue = containerObject;
			}

			if (changed && property.serializedObject.targetObject.GetType() == typeof(ScriptableObject))
				EditorUtility.SetDirty(target);
		}

		static bool TryDecomposeIndexedName(string indexedName, out int index, out string name)
		{
			if (!indexedName.Contains("["))
			{
				index = -1;
				name = null;
				return false;
			}

			name = indexedName.Substring(0, indexedName.IndexOf("[", StringComparison.Ordinal));
			string insideBrackets = indexedName.Substring(indexedName.IndexOf("[", StringComparison.Ordinal))
				.Replace("[", "")
				.Replace("]", "");
			index = Convert.ToInt32(insideBrackets);
			return true;
		}

		public static object GetValue(this SerializedProperty property)
		{
			Object target = property.serializedObject.targetObject;
			FieldInfo fieldInfo = GetFieldInfo(property);
			return fieldInfo.GetValue(target);
		}

		static object GetValue_(object source, string name)
		{
			if (source == null)
				return null;
			Type type = source.GetType();


			while (type != null)
			{
				FieldInfo field = type.GetField(name, mask);
				if (field != null)
					return field.GetValue(source);

				PropertyInfo p =
					type.GetProperty(name, mask);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}

			return null;
		}


		static object GetValue_(object source, string name, int index)
		{
			if (GetValue_(source, name) is not IEnumerable enumerable)
				return null;
			IEnumerator enm = enumerable.GetEnumerator();

			for (int i = 0; i <= index; i++)
			{
				if (!enm.MoveNext())
					return null;
			}

			return enm.Current;
		}

		static object GetValue_(object source, FieldInfo field, int index)
		{
			if (source == null)
				return null;
			if (field == null)
				return null;

			Type fieldType = field.FieldType;
			if (IsSubclassOf_GenericsSupported(fieldType, typeof(Array)) ||
				IsSubclassOf_GenericsSupported(fieldType, typeof(List<>)))
			{
				IList list = (IList)field.GetValue(source);
				return list[index];
			}

			return null;
		}

		static bool TrySetValue_(object source, FieldInfo field, object newValue)
		{
			if (source == null)
				return false;
			if (field == null)
				return false;
			object oldValue = field.GetValue(source);
			if (Equals(oldValue, newValue))
				return false;

			field.SetValue(source, newValue);
			return true;
		}

		static bool TrySetValue_(object source, FieldInfo field, int index, object newValue)
		{
			if (source == null)
				return false;
			if (field == null)
				return false;

			Type fieldType = field.FieldType;
			if (IsSubclassOf_GenericsSupported(fieldType, typeof(Array)) ||
				IsSubclassOf_GenericsSupported(fieldType, typeof(List<>)))
			{
				IList list = (IList)field.GetValue(source);
				if (!list[index].Equals(newValue))
				{
					list[index] = newValue;
					return true;
				}
			}

			return false;
		}


		public static void CopyPropertyValueTo(this SerializedProperty source, SerializedProperty destination)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			switch (source.propertyType)
			{
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.LayerMask:
				case SerializedPropertyType.Character:
					destination.intValue = source.intValue;
					break;
				case SerializedPropertyType.Boolean:
					destination.boolValue = source.boolValue;
					break;
				case SerializedPropertyType.Float:
					destination.floatValue = source.floatValue;
					break;
				case SerializedPropertyType.String:
					destination.stringValue = source.stringValue;
					break;
				case SerializedPropertyType.Color:
					destination.colorValue = source.colorValue;
					break;
				case SerializedPropertyType.ObjectReference:
					destination.objectReferenceValue = source.objectReferenceValue;
					break;
				case SerializedPropertyType.ExposedReference:
					destination.exposedReferenceValue = source.exposedReferenceValue;
					break;
				case SerializedPropertyType.Enum:
					destination.enumValueIndex = source.enumValueIndex;
					break;
				case SerializedPropertyType.Vector2:
					destination.vector2Value = source.vector2Value;
					break;
				case SerializedPropertyType.Vector3:
					destination.vector3Value = source.vector3Value;
					break;
				case SerializedPropertyType.Vector4:
					destination.vector4Value = source.vector4Value;
					break;
				case SerializedPropertyType.Vector2Int:
					destination.vector2IntValue = source.vector2IntValue;
					break;
				case SerializedPropertyType.Vector3Int:
					destination.vector3IntValue = source.vector3IntValue;
					break;
				case SerializedPropertyType.Rect:
					destination.rectValue = source.rectValue;
					break;
				case SerializedPropertyType.Bounds:
					destination.boundsValue = source.boundsValue;
					break;
				case SerializedPropertyType.RectInt:
					destination.rectIntValue = source.rectIntValue;
					break;
				case SerializedPropertyType.BoundsInt:
					destination.boundsIntValue = source.boundsIntValue;
					break;

				case SerializedPropertyType.AnimationCurve:
					destination.animationCurveValue = source.animationCurveValue;
					break;
				case SerializedPropertyType.Quaternion:
					destination.quaternionValue = source.quaternionValue;
					break;
				case SerializedPropertyType.Generic:
				{
					IEnumerator<SerializedProperty> sourceEnumerator = source.GetChildren().GetEnumerator();
					IEnumerator<SerializedProperty> destinationEnumerator = destination.GetChildren().GetEnumerator();
					for (int i = 0; i < source.GetChildPropertyCount(includeGrandChildren: false); i++)
					{
						sourceEnumerator.MoveNext();
						destinationEnumerator.MoveNext();
						sourceEnumerator.Current.CopyPropertyValueTo(destinationEnumerator.Current);
					}

					sourceEnumerator.Dispose();
					destinationEnumerator.Dispose();

					break;
				}
			}

			if (!source.isArray || !destination.isArray)
				return;

			for (int i = 0; i < source.arraySize; i++)
			{
				SerializedProperty sourceElement = source.GetArrayElementAtIndex(i);
				if (destination.arraySize - 1 < i)
					destination.InsertArrayElementAtIndex(i);
				SerializedProperty destinationElement = destination.GetArrayElementAtIndex(i);
				sourceElement.CopyPropertyValueTo(destinationElement);
			}

			for (int i = destination.arraySize - 1; i > source.arraySize - 1; i--)
				destination.DeleteArrayElementAtIndex(i);
		}


		public static double GetNumericValue(this SerializedProperty prop)
		{
			switch (prop.propertyType)
			{
				case SerializedPropertyType.Integer:
					return prop.intValue;
				case SerializedPropertyType.Boolean:
					return prop.boolValue ? 1d : 0d;
				case SerializedPropertyType.Float:
					return prop.type == "double" ? prop.doubleValue : prop.floatValue;
				case SerializedPropertyType.ArraySize:
					return prop.arraySize;
				case SerializedPropertyType.Character:
					return prop.intValue;
				default:
					return 0d;
			}
		}

		public static bool IsNumericValue(this SerializedProperty prop) => prop.propertyType switch
		{
			SerializedPropertyType.Integer or SerializedPropertyType.Boolean or
			SerializedPropertyType.Float or SerializedPropertyType.ArraySize or
			SerializedPropertyType.Character => true,
			_ => false,
		};

		public static int GetChildPropertyCount(this SerializedProperty property, bool includeGrandChildren = false)
		{
			SerializedProperty pStart = property.Copy();
			SerializedProperty pEnd = property.GetEndProperty();
			int cnt = 0;

			pStart.Next(true);
			while (!SerializedProperty.EqualContents(pStart, pEnd))
			{
				cnt++;
				pStart.Next(includeGrandChildren);
			}

			return cnt;
		}


		public static FieldInfo GetFieldInfo(this SerializedProperty property)
		{
			const BindingFlags bindings =
				BindingFlags.Instance |
				BindingFlags.Static |
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy;
			Object targetObject = property.serializedObject.targetObject;
			Type targetType = targetObject.GetType();
			return targetType.GetField(property.propertyPath, bindings);
		}

		public static bool IsExpandable(this SerializedProperty property)
		{
			if (property.propertyType is SerializedPropertyType.Integer or SerializedPropertyType.Boolean or
				SerializedPropertyType.String or SerializedPropertyType.Vector2 or SerializedPropertyType.Vector3 or
				SerializedPropertyType.AnimationCurve or SerializedPropertyType.LayerMask)
				return false;
			return true;
		}

		/// Gets the object that the property is a member of
		public static object GetObjectWithProperty(this SerializedProperty prop)
		{
			string path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			string[] elements = path.Split('.');
			foreach (string element in elements.Take(elements.Length - 1))
			{
				if (element.Contains("["))
				{
					string elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
					int index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal))
						.Replace("[", "").Replace("]", ""));
					obj = GetValueAt(obj, elementName, index);
				}
				else
				{
					obj = GetFieldValue(obj, element);
				}
			}

			return obj;



			object GetValueAt(object source, string name, int index)
			{
				IEnumerable enumerable = GetFieldValue(source, name) as IEnumerable;
				if (enumerable == null)
					return null;

				IEnumerator enm = enumerable.GetEnumerator();
				while (index-- >= 0)
					enm.MoveNext();
				return enm.Current;
			}

			object GetFieldValue(object source, string name)
			{
				if (source == null)
					return null;

				foreach (Type type in GetHierarchyTypes(source.GetType()))
				{
					FieldInfo f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
					if (f != null)
						return f.GetValue(source);

					PropertyInfo p = type.GetProperty(name,
						BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
					if (p != null)
						return p.GetValue(source, null);
				}

				return null;


				IEnumerable<Type> GetHierarchyTypes(Type sourceType)
				{
					yield return sourceType;
					while (sourceType.BaseType != null)
					{
						yield return sourceType.BaseType;
						sourceType = sourceType.BaseType;
					}
				}
			}
		}

		public static string AsStringValue(this SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.String:
					return property.stringValue;

				case SerializedPropertyType.Character:
				case SerializedPropertyType.Integer:
					if (property.type == "char")
						return Convert.ToChar(property.intValue).ToString();
					return property.intValue.ToString();

				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue != null ? property.objectReferenceValue.ToString() : "null";

				case SerializedPropertyType.Boolean:
					return property.boolValue.ToString();

				case SerializedPropertyType.Enum:
					return property.GetValue().ToString();

				default:
					return string.Empty;
			}
		}

		public static int GetUniquePropertyId(this SerializedProperty property)
			=> property.serializedObject.targetObject.GetType().GetHashCode()
			   + property.propertyPath.GetHashCode();

		static bool IsSubclassOf_GenericsSupported(Type subType, Type baseType)
		{
			if (baseType.IsGenericType)			
				return IsSubclassOfRawGeneric(subType, baseType);
			
			return subType.IsSubclassOf(baseType);
		}

		static bool IsSubclassOfRawGeneric(Type typeToTest, Type genericBaseClass)
		{
			while (typeToTest != null && typeToTest != typeof(object))
			{
				Type cur = typeToTest.IsGenericType ? typeToTest.GetGenericTypeDefinition() : typeToTest;
				if (genericBaseClass == cur)
				{
					return true;
				}

				typeToTest = typeToTest.BaseType;
			}

			return false;
		}
	}
}
#endif