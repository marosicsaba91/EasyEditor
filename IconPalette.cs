# if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{

	public class IconPalette : EditorWindow
	{
		Vector2 scrollPos;

		[MenuItem("Tools/Icon Palette")]
		public static void Open()
		{
			IconPalette window = GetWindow<IconPalette>();
			window.titleContent = new GUIContent("Icon Palette");
		}

		void OnGUI()
		{
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			EditorHelper.ProcessAllIcons();

			if (EditorHelper.iconTextures != null)
			{
				foreach ((IconType type, Dictionary<IconSize, Texture> group) in EditorHelper.iconTextures)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(type.ToString(), EditorStyles.boldLabel);
					foreach ((IconSize _, Texture t) in group)
					{
						float w = Mathf.Min(t.width, 64) + 6;
						float h = Mathf.Min(t.height, 64) + 6;
						GUILayout.Box(t, GUILayout.Width(w), GUILayout.Height(h));
					}

					EditorGUILayout.EndHorizontal();
				}
			}


			GUILayout.EndScrollView();
		}
	}
}
#endif