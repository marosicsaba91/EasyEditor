#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyEditor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assemblies;

[AutoStaticsCleanup]
[CustomPropertyDrawer(typeof(TypePickerAttribute))]
public partial class TypePickerDrawer : PropertyDrawer
{
	static EditorWindow _openDropdownWindow;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		TypePickerAttribute att = attribute as TypePickerAttribute;
		Type currentType = property.GetObjectOfProperty()?.GetType();
		bool drawProp = true; 
		Rect pickerRect = default;
		Type managedReferenceFieldType = null;

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

			managedReferenceFieldType = property.GetManagedReferenceFieldType();
			if (managedReferenceFieldType == null)
				return;

			if (label == GUIContent.none || string.IsNullOrEmpty(label.text))
			{
				pickerRect = position;
				drawProp = currentType != null;
			}
			else
			{
				pickerRect = EditorGUI.PrefixLabel(position, label);
				drawProp = currentType != null;
			}

		}
		
		bool pickerClicked = Event.current.type == EventType.MouseDown &&
			Event.current.button == 0 && pickerRect.Contains(Event.current.mousePosition);
		if (pickerClicked)
			DrawTypePicker(pickerRect, property, GUIContent.none, managedReferenceFieldType, att);

		if (drawProp)
			EditorGUI.PropertyField(position, property, label, includeChildren: true);

		if (!pickerClicked)
			DrawTypePicker(pickerRect, property, GUIContent.none, managedReferenceFieldType, att);
	}

	public static void DrawTypePicker(
		Rect position,
		SerializedProperty property,
		GUIContent label,
		Type managedReferenceFieldType,
		TypePickerAttribute attribute = null,
		List<Type> disabledTypesTypes = null
		)
	{
		position.height = EditorGUIUtility.singleLineHeight;
		List<Type> inheritedTypes = GetInheritedNonAbstractTypes(managedReferenceFieldType);

		if (label != GUIContent.none)
		{
			float labelWidth = GUI.skin.label.CalcSize(label).x;
			position.SliceOut(labelWidth, Side.Left);
		}

		Type currentType = property.GetObjectOfProperty()?.GetType();
		if (attribute != null)
			inheritedTypes = ApplyTypeFilter(property, attribute, inheritedTypes);
		if (disabledTypesTypes != null && disabledTypesTypes.Count > 0)
			inheritedTypes = inheritedTypes.Where(type => !disabledTypesTypes.Contains(type)).ToList();

		TypePickerAttribute.TypeToStringConversion conversion = attribute?.typeToStringConversion ?? TypePickerAttribute.TypeToStringConversion.ShortName;
		inheritedTypes = inheritedTypes
			.OrderBy(type => TypeToString(type, conversion), StringComparer.OrdinalIgnoreCase)
			.ToList();

		int tempIndent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		GUIContent buttonContent = new(TypeToString(currentType, conversion));
		if (EditorGUI.DropdownButton(position, buttonContent, FocusType.Keyboard))
		{
			if (!ReferenceEquals(_openDropdownWindow, null))
			{
				if (_openDropdownWindow != null)
					_openDropdownWindow.Close();
				ClearOpenDropdownWindow();
				return;
			}

			TypeDropdown dropdown = new(new AdvancedDropdownState(), inheritedTypes, conversion, selectedType =>
			{
				ClearOpenDropdownWindow();
				if (selectedType == currentType) return;
				SetManagedReferenceType(property, selectedType);
			});
			dropdown.Show(position);
			EditorWindow focusedWindow = EditorWindow.focusedWindow;
			if (focusedWindow != null && focusedWindow.GetType().Name == "AdvancedDropdownWindow")
			{
				_openDropdownWindow = focusedWindow;
				EditorApplication.update += ClearDestroyedDropdownWindow;
			}
		}

		EditorGUI.indentLevel = tempIndent;
	}

	static void ClearDestroyedDropdownWindow()
	{
		if (!ReferenceEquals(_openDropdownWindow, null) && _openDropdownWindow == null)
			ClearOpenDropdownWindow();
	}

	static void ClearOpenDropdownWindow()
	{
		_openDropdownWindow = null;
		EditorApplication.update -= ClearDestroyedDropdownWindow;
	}

	static void SetManagedReferenceType(SerializedProperty property, Type selectedType)
	{
		Undo.RecordObject(property.serializedObject.targetObject, "Reference Type Changed");
		if (selectedType == null)
		{
			property.managedReferenceValue = null;
		}
		else
		{
			if (selectedType.IsSubclassOf(typeof(UnityEngine.Object)))
			{
				Debug.LogWarning("SerializedReference don't work with UnityEngine.Object types");
			}
			else
			{
				object newInstance = Activator.CreateInstance(selectedType);
				TrySetupProperties(property, newInstance, selectedType);
				property.managedReferenceValue = newInstance;
			}
		}

		property.serializedObject.ApplyModifiedProperties();
	}

	sealed class TypeDropdown : AdvancedDropdown
	{
		readonly IReadOnlyList<Type> _types;
		readonly TypePickerAttribute.TypeToStringConversion _conversion;
		readonly Action<Type> _onSelected;

		public TypeDropdown(
			AdvancedDropdownState state,
			IReadOnlyList<Type> types,
			TypePickerAttribute.TypeToStringConversion conversion,
			Action<Type> onSelected) : base(state)
		{
			_types = types;
			_conversion = conversion;
			_onSelected = onSelected;
			minimumSize = new(280, 320);
		}

		protected override AdvancedDropdownItem BuildRoot()
		{
			AdvancedDropdownItem root = new("Select Type");
			root.AddChild(new TypeDropdownItem("None", null)); // { icon = EditorGUIUtility.IconContent("d_winbtn_win_close").image as Texture2D });

			foreach (Type type in _types)
			{
				TypeDropdownItem item = new(TypeToString(type, _conversion), type)
				{
					icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D
				};
				root.AddChild(item);
			}

			return root;
		}

		protected override void ItemSelected(AdvancedDropdownItem item)
		{
			if (item is TypeDropdownItem typeItem)
				_onSelected(typeItem.Type);
		}
	}

	sealed class TypeDropdownItem : AdvancedDropdownItem
	{
		public Type Type { get; }

		public TypeDropdownItem(string name, Type type) : base(name)
		{
			Type = type;
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

		List<Type> inheritedTypes = CurrentAssemblies.GetLoadedAssemblies()
			.SelectMany(s => s.GetTypes())
			.Where(baseType.IsAssignableFrom)
			.Where(type => !type.IsAbstract)
			.ToList();

		_inheritedNonAbstractTypes.Add(baseType, inheritedTypes);

		return inheritedTypes;
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property);
	}
}
#endif
