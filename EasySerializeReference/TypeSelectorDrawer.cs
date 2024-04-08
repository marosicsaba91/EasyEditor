#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyEditor;
using UnityEditor;
using UnityEngine;

namespace Asteroids.Editor
{
	[CustomPropertyDrawer(typeof(TypePickerAttribute))]
	public class TypeSelectorDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			TypePickerAttribute att = attribute as TypePickerAttribute;
			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				Debug.LogWarning($"{property.serializedObject.targetObject.name} / " +
								 $"{property.serializedObject.targetObject.GetType()} / " +
								 $"{property.name}" +
								 $" field is not Managed Reference Types." +
								 $"Use [TypePickerAttribute] only with [SerializeReference]");
			}
			else
			{

				Type managedReferenceFieldType = GetManagedReferenceFieldType(property);
				if (managedReferenceFieldType == null)
					return;

				DrawTypePicker(position, property, label, managedReferenceFieldType, att);
			}

			// Draw the property of the selected type
			EditorGUI.PropertyField(position, property, label, includeChildren: true);
		}

		static void DrawTypePicker(Rect position, SerializedProperty property, GUIContent label, Type managedReferenceFieldType, TypePickerAttribute att)
		{
			position.height = EditorGUIUtility.singleLineHeight;
			List<Type> inheritedTypes = GetInheritedNonAbstractTypes(managedReferenceFieldType);

			int currentTypeIndex;

			float labelWidth = GUI.skin.label.CalcSize(label).x;
			position.SliceOut(labelWidth + 12, Side.Left);

			Type currentType = GetRealTypeFromTypeName(property.managedReferenceFullTypename);
			inheritedTypes = ApplyTypeFilter(property, att, inheritedTypes);

			if (currentType == null)
				currentTypeIndex = 0;
			else
			{
				currentTypeIndex = inheritedTypes.IndexOf(currentType) + 1;

				if (!property.IsExpandable() || att.forceSmall)
				{
					position.width = 20;
					position.x -= 20;
				}
			}


			IEnumerable<string> typeNames = inheritedTypes.Select(t => TypeToString(t, att.typeToStringConversion));
			string[] options = new[] { "- Select Type -" }.Concat(typeNames).ToArray();

			int tempIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			int resultTypeIndex = EditorGUI.Popup(position, currentTypeIndex, options);
			EditorGUI.indentLevel = tempIndent;

			if (resultTypeIndex != currentTypeIndex)
			{
				Undo.RecordObject(property.serializedObject.targetObject, "Reference Type Changed");
				if (resultTypeIndex == 0)
				{
					property.managedReferenceValue = null;
				}
				else
				{
					Type newType = inheritedTypes[resultTypeIndex - 1];
					if (newType.IsSubclassOf(typeof(UnityEngine.Object)))
					{
						Debug.LogWarning("SerializedReference don't work with UnityEngine.Object types");
					}
					else
					{
						object newInstance = Activator.CreateInstance(newType);
						TrySetupProperties(property, newInstance, newType);
						property.managedReferenceValue = newInstance;
					}
				}

				property.serializedObject.ApplyModifiedProperties();
			}
		}

		static string TypeToString(Type t, TypePickerAttribute.TypeToStringConversion conversation)
		{
			if (t == null)
				return "- Select Type -";

			if (conversation == TypePickerAttribute.TypeToStringConversion.ShortName)
				return t.Name;
			return t.ToString();
		}

		static List<Type> ApplyTypeFilter(SerializedProperty property, TypePickerAttribute att, List<Type> inheritedTypes)
		{
			if (att.filterMethod != null && att.filterMethod.Length > 0)
			{
				object containingObject = property.GetObjectWithProperty();
				Type t = property.GetObjectWithProperty().GetType();
				MethodInfo methodInfo = t.GetMethod(att.filterMethod,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				if (methodInfo == null)
					Debug.LogError($"Method {att.filterMethod} not found in {t}");
				else
				{
					bool Filter(Type type) => (bool)methodInfo.Invoke(containingObject, new object[] { type });
					inheritedTypes = inheritedTypes.Where(Filter).ToList();
				}
			}

			return inheritedTypes;
		}

		static void TrySetupProperties(SerializedProperty oldValue, object newInstance, Type newType)
		{
			try
			{
				IEnumerable<FieldInfo> allFieldsOfNewType = AllFields(newType).ToArray();
				object oldInstance = oldValue.GetObjectOfProperty();
				Type oldType = oldInstance.GetType();
				IEnumerable<FieldInfo> allFieldsOfOldType = AllFields(oldType).ToArray();
				foreach (FieldInfo field in allFieldsOfNewType)
				{
					if (!allFieldsOfOldType.Contains(field))
						continue;
					object newValue = field.GetValue(oldInstance);
					field.SetValue(newInstance, newValue);
				}
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public static IEnumerable<FieldInfo> AllFields(Type type)
		{
			const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			FieldInfo[] self = type.GetFields(binding);
			if (type.BaseType == null)
				return self;
			return self.Concat(AllFields(type.BaseType));
		}

		static readonly Dictionary<Type, List<Type>> _inheritedNonAbstractTypes = new();
		static List<Type> GetInheritedNonAbstractTypes(Type baseType)
		{
			if (_inheritedNonAbstractTypes.TryGetValue(baseType, out List<Type> inherited))
				return inherited;

			List<Type> inheritedTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(baseType.IsAssignableFrom)
				.Where(type => !type.IsAbstract)
				.ToList();

			_inheritedNonAbstractTypes.Add(baseType, inheritedTypes);

			return inheritedTypes;
		}

		static Type GetPropertyFieldType(SerializedProperty property) => property.propertyType switch
		{
			SerializedPropertyType.Boolean => typeof(bool),
			SerializedPropertyType.Float => typeof(float),
			SerializedPropertyType.Integer => typeof(int),
			SerializedPropertyType.String => typeof(string),
			SerializedPropertyType.Bounds => typeof(Bounds),
			SerializedPropertyType.Character => typeof(char),
			SerializedPropertyType.Color => typeof(Color),
			SerializedPropertyType.Enum => typeof(Enum),
			SerializedPropertyType.Gradient => typeof(Gradient),
			SerializedPropertyType.Quaternion => typeof(Quaternion),
			SerializedPropertyType.Rect => typeof(Rect),
			SerializedPropertyType.Vector2 => typeof(Vector2),
			SerializedPropertyType.Vector3 => typeof(Vector3),
			SerializedPropertyType.Vector4 => typeof(Vector4),
			SerializedPropertyType.AnimationCurve => typeof(AnimationCurve),
			SerializedPropertyType.BoundsInt => typeof(BoundsInt),
			SerializedPropertyType.LayerMask => typeof(LayerMask),
			SerializedPropertyType.RectInt => typeof(RectInt),
			SerializedPropertyType.Vector2Int => typeof(Vector2Int),
			SerializedPropertyType.Vector3Int => typeof(Vector3Int),
			SerializedPropertyType.ManagedReference => GetManagedReferenceFieldType(property),
			_ => property.GetObjectOfProperty()?.GetType(),
		};

		static Type GetManagedReferenceFieldType(SerializedProperty property)
		{
			Type realPropertyType = GetRealTypeFromTypeName(property.managedReferenceFieldTypename);
			if (realPropertyType != null)
				return realPropertyType;
			return null;
		}

		static Type GetRealTypeFromTypeName(string stringType)
		{
			(string AssemblyName, string ClassName) names = GetSplitNamesFromTypename(stringType);
			Type realType = Type.GetType($"{names.ClassName}, {names.AssemblyName}");
			return realType;
		}

		static (string AssemblyName, string ClassName) GetSplitNamesFromTypename(string typename)
		{
			if (string.IsNullOrEmpty(typename))
				return ("", "");

			string[] typeSplitString = typename.Split(char.Parse(" "));
			string typeClassName = typeSplitString[1];
			string typeAssemblyName = typeSplitString[0];
			return (typeAssemblyName, typeClassName);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property);
		}
	}
}
#endif