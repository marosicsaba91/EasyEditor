using UnityEngine;
using System;
using EasyEditor;
using System.Collections.Generic;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Advanced ScriptableObject Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Field)]

public class EasySOAttribute : PropertyAttribute
{
	public bool nesting = true;
	public bool autoCreate = true;
	public bool inline = true;
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EasySOAttribute))]
public class EasySODrawer : PropertyDrawer
{
	public enum VerticalDirection
	{
		Down = -1,
		Up = 1
	}


	static readonly Dictionary<Type, List<Type>> typeToSubType = new();

	// Static constructor to initialize the dictionary.

	static EasySODrawer()
	{
		Type soType = typeof(ScriptableObject);
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (type.IsAbstract) continue;
				if (!type.IsSubclassOf(soType)) continue;

				// Self:
				if (!typeToSubType.ContainsKey(type))
					typeToSubType[type] = new List<Type>();

				typeToSubType[type].Add(type);

				// Parents:
				Type nextType = type;
				while (nextType.IsSubclassOf(soType))
				{
					Type baseType = nextType.BaseType;

					if (baseType == null) break;

					if (!typeToSubType.ContainsKey(baseType))
						typeToSubType[baseType] = new List<Type>();

					typeToSubType[baseType].Add(type);
					nextType = baseType;
				}
			}
		}
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		static void LogTypeError(Rect position, GUIContent label, string type) => EditorGUI.LabelField(position, label.text, $"{type} is Not supported!  Use {nameof(EasySOAttribute)} Attribute only for ScriptableObjects");

		EasySOAttribute easySOAttribute = attribute as EasySOAttribute;
		bool nesting = easySOAttribute.nesting;
		bool autoCreate = easySOAttribute.autoCreate;
		bool inline = easySOAttribute.inline;

		if (property.propertyType != SerializedPropertyType.ObjectReference)
		{
			LogTypeError(position, label, property.propertyType.ToString());
			return;
		}

		ScriptableObject subjectSO = property.GetObjectOfProperty(out Type referencedType) as ScriptableObject;
		ScriptableObject containerSO = property.serializedObject.targetObject as ScriptableObject;

		if (!referencedType.IsSubclassOf(typeof(ScriptableObject)))
		{
			LogTypeError(position, label, referencedType.Name);
			return;
		}

		string subjectPath = AssetDatabase.GetAssetPath(subjectSO);
		string targetPath = AssetDatabase.GetAssetPath(containerSO);

		ScriptableObject objectOnSubjectPath = AssetDatabase.LoadAssetAtPath<ScriptableObject>(subjectPath);

		bool isNestedInTarget = subjectPath == targetPath;
		bool isNested = objectOnSubjectPath != subjectSO;

		// Foldout:
		Rect header = position;
		header.height = EditorGUIUtility.singleLineHeight;

		Rect menuButtonRect = header;
		if (nesting || autoCreate)
			menuButtonRect = header.SliceOut(20, Side.Right);

		if (isNested && !isNestedInTarget)
		{
			Rect warningRect = header.SliceOut(115, Side.Right);
			EasyMessageDrawer.DrawMessage(warningRect, "Nested elsewhere!", EasyEditor.MessageType.Warning);
		}

		if (inline)
		{
			Rect foldoutRect = header;
			foldoutRect.width = EditorHelper.LabelWidth;
			if (subjectSO != null)
				property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
		}
		else
			property.isExpanded = false;

		EditorGUI.indentLevel++;
		EditorGUI.BeginProperty(header, label, property);
		EditorGUI.PropertyField(header, property, label, includeChildren: true);
		EditorGUI.EndProperty();
		EditorGUI.indentLevel--;

		if (containerSO == null) return;

		if ((nesting || autoCreate) && GUI.Button(menuButtonRect, "..."))
		{
			GenericMenu menu = new();
			if (autoCreate)
			{
				AddCreateItemOptions(menu, referencedType, property);

				if (menu.GetItemCount() > 0 && nesting)
					menu.AddSeparator("");
			}

			if (nesting)
			{
				if (subjectSO != null && !isNestedInTarget && !isNested)
					menu.AddItem(new GUIContent("Nest"), false, () => Nest(subjectSO, containerSO));

				if (subjectSO != null && isNestedInTarget)
					menu.AddItem(new GUIContent("Un-Nest"), false, () => UnNest(subjectSO, containerSO));

				if (subjectSO != null && isNestedInTarget)
					menu.AddItem(new GUIContent("Delete"), false, () => Delete(property));
			}

			menu.ShowAsContext();
		}

		if (property.isExpanded && property.objectReferenceValue != null)
			DrawInline(property, subjectSO, containerSO, isNestedInTarget);

		position.y += EditorGUIUtility.standardVerticalSpacing;
	}

	void DrawInline(SerializedProperty property, ScriptableObject subjectSO, ScriptableObject containerSO, bool isNestedInTarget)
	{
		EditorGUI.indentLevel++;

		if (isNestedInTarget)
		{
			string name = subjectSO.name;
			string newName = EditorGUILayout.TextField("Name", name);
			if (name != newName)
			{
				subjectSO.name = newName;
				EditorUtility.SetDirty(subjectSO);
				EditorUtility.SetDirty(containerSO);
			}
		}
		SerializedObject obj = new(property.objectReferenceValue);
		SerializedProperty iteratedProperty = obj.GetIterator();

		iteratedProperty.NextVisible(enterChildren: true);
		while (iteratedProperty.NextVisible(enterChildren: false))
		{
			if (iteratedProperty.isArray)
				DrawArray(iteratedProperty);
			else
				EditorGUILayout.PropertyField(iteratedProperty, includeChildren: true);
		}
		obj.ApplyModifiedProperties();
		EditorGUI.indentLevel--;
	}


	private static void DrawArray(SerializedProperty property)
	{
		Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
		Rect counterRect = headerRect.SliceOut(70, Side.Right);

		SerializedProperty arrayProperty = property.Copy();
		int arraySize = property.arraySize;

		// Foldout
		bool isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, GUIContent.none, true);
		property.isExpanded = isExpanded;

		// Name of the array

		EditorGUI.indentLevel++;
		FontStyle originalFontStyle = EditorStyles.label.fontStyle;
		EditorStyles.label.fontStyle = FontStyle.Bold;
		EditorGUI.BeginProperty(headerRect, GUIContent.none, property);
		EditorGUI.PropertyField(headerRect, property, includeChildren: false);
		EditorGUI.EndProperty();
		EditorStyles.label.fontStyle = originalFontStyle;


		// Count field 
		property.NextVisible(enterChildren: true);

		float originalLabelWidth = EditorGUIUtility.labelWidth;
		int originalIndent = EditorGUI.indentLevel;
		EditorGUIUtility.labelWidth = 30 - EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.indentLevel = 0;

		EditorGUI.BeginProperty(counterRect, GUIContent.none, property);
		EditorGUI.PropertyField(counterRect, property, includeChildren: false);
		EditorGUI.EndProperty();

		EditorGUIUtility.labelWidth = originalLabelWidth;
		EditorGUI.indentLevel = originalIndent;

		// Draw the array elements
		if (!isExpanded)
		{
			for (int i = 0; i < arraySize; i++)
				if (!property.NextVisible(enterChildren: false)) break;
		}
		else
		{
			for (int i = 0; i < arraySize; i++)
			{
				bool isNext = property.NextVisible(enterChildren: false);
				if (!isNext) break;

				float height = EditorGUI.GetPropertyHeight(property, includeChildren: true);
				Rect itemRect = EditorGUILayout.GetControlRect(false, height);
				Rect menuRect = itemRect.SliceOut(40, Side.Right);
				menuRect.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.BeginProperty(itemRect, GUIContent.none, property);
				EditorGUI.PropertyField(itemRect, property, includeChildren: false);
				EditorGUI.EndProperty();

				if (GUI.Button(menuRect, new GUIContent($"{i}/{arraySize}", $"Actions on Element: {i}")))
				{
					int index = i;
					GenericMenu menu = new();
					menu.AddItem(new GUIContent("Delete"), false, () => Delete(arrayProperty, index));
					menu.AddItem(new GUIContent("Up"), false, () => Move(arrayProperty, index, VerticalDirection.Up));
					menu.AddItem(new GUIContent("Down"), false, () => Move(arrayProperty, index, VerticalDirection.Down));
					menu.AddItem(new GUIContent("Insert Over"), false, () => Insert(arrayProperty, index, VerticalDirection.Up));
					menu.AddItem(new GUIContent("Insert Under"), false, () => Insert(arrayProperty, index, VerticalDirection.Down));
					menu.ShowAsContext();
				}
			}
		}
		EditorGUI.indentLevel--;
	}

	static void Delete(SerializedProperty arrayProperty, int index)
	{
		arrayProperty.DeleteArrayElementAtIndex(index);
		arrayProperty.serializedObject.ApplyModifiedProperties();
	}
	static void Move(SerializedProperty arrayProperty, int index, VerticalDirection direction)
	{
		int newIndex = index - (int)direction;
		if (newIndex < 0 || newIndex >= arrayProperty.arraySize) return;

		arrayProperty.MoveArrayElement(index, newIndex);
		arrayProperty.serializedObject.ApplyModifiedProperties();
	}
	static void Insert(SerializedProperty arrayProperty, int index, VerticalDirection direction)
	{
		if (direction == VerticalDirection.Down)
			index++;

		arrayProperty.InsertArrayElementAtIndex(index);
		arrayProperty.serializedObject.ApplyModifiedProperties();
	}


	void AddCreateItemOptions(GenericMenu menu, Type baseType, SerializedProperty property)
	{

		if (!typeToSubType.TryGetValue(baseType, out List<Type> subTypes)) return;

		ScriptableObject targetSO = property.serializedObject.targetObject as ScriptableObject;
		if (targetSO != null)
		{
			foreach (Type subType in subTypes)
				menu.AddItem(new GUIContent($"Create new {subType} as Nested"), false, () => CreateAndNest(subType, targetSO, property));
		}

		if (menu.GetItemCount() > 0)
			menu.AddSeparator("");

		foreach (Type subType in subTypes)
			menu.AddItem(new GUIContent($"Create new {subType} as File"), false, () => Create(subType, targetSO, property));

		static void CreateAndNest(Type type, ScriptableObject container, SerializedProperty property)
		{
			ScriptableObject newObject = ScriptableObject.CreateInstance(type);
			newObject.name = type.Name;
			AssetDatabase.AddObjectToAsset(newObject, container);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			property.objectReferenceValue = newObject;
			property.serializedObject.ApplyModifiedProperties();

			EditorUtility.SetDirty(container);
		}

		static void Create(Type type, ScriptableObject container, SerializedProperty property)
		{
			ScriptableObject newObject = ScriptableObject.CreateInstance(type);
			newObject.name = type.Name;
			string path;
			if (container == null)
				path = "Assets/";
			else
			{
				path = AssetDatabase.GetAssetPath(container);
				path = path[..path.LastIndexOf('/')] + "/";
			}

			string fullPath = GetUnusedPath(path, type.ToString());

			AssetDatabase.CreateAsset(newObject, fullPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			property.objectReferenceValue = newObject;
			property.serializedObject.ApplyModifiedProperties();
		}
	}

	static string GetUnusedPath(string path, string name)
	{
		string typePath = path + name;
		string fullPath = typePath + ".asset";
		for (int i = 0; i < 250; i++)  // 250 is just a large number: The limit to prevent infinite loop
		{
			if (i != 0)
				fullPath = typePath + $"_{i}.asset";

			if (!AssetDatabase.AssetPathExists(fullPath))
				break;
		}

		return fullPath;
	}

	void Nest(ScriptableObject subject, ScriptableObject container)
	{
		string subjectPath = AssetDatabase.GetAssetPath(subject);
		AssetDatabase.RemoveObjectFromAsset(subject);
		AssetDatabase.AddObjectToAsset(subject, container);
		AssetDatabase.DeleteAsset(subjectPath);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

	}

	void UnNest(ScriptableObject subject, ScriptableObject container)
	{
		AssetDatabase.RemoveObjectFromAsset(subject);

		string path = AssetDatabase.GetAssetPath(container);
		path = path[..path.LastIndexOf('/')] + "/";

		string fullPath = GetUnusedPath(path, subject.name);
		AssetDatabase.CreateAsset(subject, fullPath);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	void Delete(SerializedProperty property)
	{
		ScriptableObject obj = property.objectReferenceValue as ScriptableObject;
		if (obj == null) return;

		property.objectReferenceValue = null;
		property.serializedObject.ApplyModifiedProperties();

		AssetDatabase.RemoveObjectFromAsset(obj);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		float h = EditorGUIUtility.singleLineHeight;

		if (property.objectReferenceValue != null && property.isExpanded)
			h += EditorGUIUtility.standardVerticalSpacing * 2;

		return h;
	}


}

#endif