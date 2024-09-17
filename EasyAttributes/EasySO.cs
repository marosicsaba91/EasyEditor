using UnityEngine;
using System;
using EasyEditor;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class EasySO : PropertyAttribute 
{

}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EasySO))]
public class EasySODrawer : PropertyDrawer
{

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
		static void LogTypeError(Rect position, GUIContent label, string type) => EditorGUI.LabelField(position, label.text, $"{type} is Not supported!  Use {nameof(EasySO)} Attribute only for ScriptableObjects");

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
		Rect foldoutRect = header;
		foldoutRect.width = 50;

		Rect menuButtonRect = header.SliceOut(20, Side.Right);


		if (isNested && !isNestedInTarget)
		{
			Rect warningRect = header.SliceOut(115, Side.Right);
			EasyMessageDrawer.DrawMessage(warningRect, "Nested elsewhere!", EasyEditor.MessageType.Warning);
		}	

		if(subjectSO!= null)
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

		EditorGUI.BeginProperty(header, label, property);
		EditorGUI.indentLevel++;
		EditorGUI.PropertyField(header, property, label, true);
		EditorGUI.indentLevel--;
		  
		if (containerSO == null) return;

		if (GUI.Button(menuButtonRect, "..."))
		{
			GenericMenu menu = new();
			AddCreateItemOptions(menu, referencedType, property);

			if (menu.GetItemCount() > 0)
				menu.AddSeparator("");

			if (subjectSO != null && !isNestedInTarget && !isNested)
				menu.AddItem(new GUIContent("Nest"), false, () => Nest(subjectSO, containerSO));

			if (subjectSO != null && isNestedInTarget)
				menu.AddItem(new GUIContent("Un-Nest"), false, () => UnNest(subjectSO, containerSO));

			if (subjectSO != null && isNestedInTarget)
				menu.AddItem(new GUIContent("Delete"), false, () => Delete(property));

			menu.ShowAsContext();
		}

		if (property.isExpanded && property.objectReferenceValue != null)
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
			SerializedProperty prop = obj.GetIterator();
			prop.NextVisible(true);
			while (prop.NextVisible(false))
			{
				EditorGUILayout.PropertyField(prop, true);
			}
			obj.ApplyModifiedProperties();
			EditorGUI.indentLevel--;
		}

		position.y += EditorGUIUtility.standardVerticalSpacing;
		EditorGUI.EndProperty();
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