using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyEditor
{
	[Serializable]
	public class SceneReference
	{
		[SerializeField] string guid;
		[SerializeField] string sceneName;
	}



#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SceneReference))]
	public class SceneDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty guidProperty = property.FindPropertyRelative("guid");
			SerializedProperty nameProperty = property.FindPropertyRelative("sceneName");
			string oldGuid = guidProperty.stringValue;

			SceneAsset oldScene = GetSceneObject(oldGuid);
			SceneAsset newScene = (SceneAsset)EditorGUI.ObjectField(position, label, oldScene, typeof(SceneAsset), true);
			string newGuid = GetGiud(newScene);
			if (newScene == null || newGuid == null)
			{
				guidProperty.stringValue = null;
				nameProperty.stringValue = null;
			}
			else
			{
				guidProperty.stringValue = newGuid;
				nameProperty.stringValue = newScene.name;
			}

			if (newScene != null && !IsInBuildSettings(newScene.name))
			{
				Rect p = position;
				p.width = 18;
				p.x = EditorHelper.ContentStartX - p.width - EditorGUIUtility.standardVerticalSpacing;
				GUIContent content = new("⚠️", "Scene not found in build settings.");
				EditorHelper.DrawErrorBox(p, true);
				p.x += 2;
				EditorGUI.LabelField(p, content);
			}
		}

		protected SceneAsset GetSceneObject(string guid)
		{
			if (string.IsNullOrEmpty(guid))
				return null;


			string path = AssetDatabase.GUIDToAssetPath(guid);
			SceneAsset sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
			return sceneObject;
		}

		protected string GetGiud(SceneAsset sceneObject)
		{
			if (sceneObject == null)
			{
				return null;
			}

			string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sceneObject));
			return guid;
		}

		protected bool IsInBuildSettings(string sceneObjectName)
		{
			if (string.IsNullOrEmpty(sceneObjectName))
			{
				return false;
			}

			foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes)
			{
				if (editorScene.path.IndexOf(sceneObjectName, StringComparison.Ordinal) != -1)
					return true;

			}
			return false;
		}
	}
#endif


}