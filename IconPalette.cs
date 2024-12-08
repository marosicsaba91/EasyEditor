#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
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

		bool allIcons = false;
		string search = "";
		int size = 64;

		void OnGUI()
		{

			allIcons = EditorGUILayout.Toggle("All Icons", allIcons);
			search = EditorGUILayout.TextField("Search", search);
			size = EditorGUILayout.IntSlider("Icon Size", size, 16, 256);


			scrollPos = GUILayout.BeginScrollView(scrollPos);
			if (allIcons)
			{
				EditorHelper.ProcessAllIcons();
				DrawAllIcons(search, size);
			}
			else
			{
				EditorHelper.ProcessNiceIcons();
				DrawNiceIcons(search, size);
			}
			GUILayout.EndScrollView();

		}

		static void DrawAllIcons(string search, int size)
		{
			DrawIcons(search, size, EditorHelper.allTextures, (i) => i.Value, (i) => i.Key, ToInfo);

			static string ToInfo(KeyValuePair<string, Texture> item)
			{
				string info = $"Size:  {item.Value.width} x {item.Value.height}\n";
				info += $"Use it this Way:    EditorGUIUtility.IconContent(\"{item.Key}\")";
				return info;
			}
		}

		static void DrawNiceIcons(string search, int size)
		{
			DrawIcons(search, size, EditorHelper.niceTextures, ToTexture, ToText, ToInfo);

			static Texture ToTexture(KeyValuePair<IconType, Dictionary<IconSize, Texture>> item) => item.Value.LastOrDefault().Value;
			static string ToText(KeyValuePair<IconType, Dictionary<IconSize, Texture>> item) => item.Key.ToString();
			static string ToInfo(KeyValuePair<IconType, Dictionary<IconSize, Texture>> item)
			{
				string info = "Sizes:   ";
				foreach ((IconSize size, Texture t) in item.Value)
					info += $"{size}: ({t.width} x {t.height})     ";
				info += $"\nUse it this Way:    EditorHelper.GetIcon(IconType.{item.Key})";
				return info;
			}
		}



		static void DrawIcons<T>(string search, int size, IEnumerable<T> container, Func<T, Texture> toTexture, Func<T, string> toName, Func<T, string> toInfo)
		{
			float windowWidth = EditorGUIUtility.currentViewWidth;
			float buttonWidth = size + 6;
			int iconsPerRow = Mathf.FloorToInt((windowWidth - 16) / (buttonWidth + EditorGUIUtility.standardVerticalSpacing + 1));

			int i = 0;
			EditorGUILayout.BeginHorizontal();
			foreach (T item in container)
			{
				Texture t = toTexture(item);
				string st = toName(item);
				if (search == string.Empty || st.ToLower().Contains(search.ToLower()))
				{
					if (i >= iconsPerRow)
					{
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
						i = 0;
					}

					if (GUILayout.Button(t, GUILayout.Width(buttonWidth), GUILayout.Height(buttonWidth)))
					{
						Debug.Log($"{st}\n{toInfo(item)}");
					}

					i++;
				}
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}
#endif