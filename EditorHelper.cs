#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyEditor
{
	public static partial class EditorHelper
	{
		static readonly float tableMarginBrightness = EditorGUIUtility.isProSkin ? 0.2f : 0.9f;
		public static readonly Color tableMarginColor =
			new(tableMarginBrightness, tableMarginBrightness, tableMarginBrightness);

		static readonly float tableBackgroundBrightness = EditorGUIUtility.isProSkin ? 0.275f : 0.891f;
		public static readonly Color tableBackgroundColor =
			new(tableBackgroundBrightness, tableBackgroundBrightness, tableBackgroundBrightness);

		static readonly float tableBorderBrightness = EditorGUIUtility.isProSkin ? 0.15f : 0.7f;
		public static readonly Color tableBorderColor =
			new(tableBorderBrightness, tableBorderBrightness, tableBorderBrightness, 1);

		static readonly float tableEvenLineBrightness = EditorGUIUtility.isProSkin ? 0.165f : 0.95f;
		public static readonly Color tableEvenLineColor =
			new(tableEvenLineBrightness, tableEvenLineBrightness, tableEvenLineBrightness, 0.3f);

		static readonly float tableOddLineBrightness = EditorGUIUtility.isProSkin ? 0.125f : 0.85f;
		public static readonly Color tableOddLineColor =
			new(tableOddLineBrightness, tableOddLineBrightness, tableOddLineBrightness, 0.4f);

		static readonly float tableSelectedBrightness = EditorGUIUtility.isProSkin ? 0.8f : 0;
		public static readonly Color tableSelectedColor =
			new(tableSelectedBrightness, tableSelectedBrightness, tableSelectedBrightness, 0.2f);

		static readonly float tableHoverBrightness = EditorGUIUtility.isProSkin ? 1 : 0;
		public static readonly Color tableHoverColor =
			new(tableHoverBrightness, tableHoverBrightness, tableHoverBrightness, 0.08f);

		public static readonly Color successGreenColor = new(0.75f, 1f, 0.45f);
		public static readonly Color successBackgroundColor = new(0.75f, 1f, 0.45f, 0.2f);
		static readonly Color errorRedColorLight = new(0.95f, 0.2f, 0.25f);
		static readonly Color errorRedColorDark = new(0.95f, 0.4f, 0.39f);
		public static Color ErrorRedColor => EditorGUIUtility.isProSkin ? errorRedColorDark : errorRedColorLight;

		static readonly Color errorBackgroundColorLight = new(1f, 0.65f, 0.6f);
		static readonly Color errorBackgroundColorDark = new(0.56f, 0.23f, 0.21f);

		static readonly Color warningBackgroundColorLight = new(1f, 0.65f, 0.6f);
		static readonly Color warningBackgroundColorDark = new(0.56f, 0.23f, 0.21f);
		public static Color ErrorBackgroundColor =>
			EditorGUIUtility.isProSkin ? errorBackgroundColorDark : errorBackgroundColorLight;
		public static Color WarningBackgroundColor =>
			EditorGUIUtility.isProSkin ? warningBackgroundColorDark : warningBackgroundColorLight;

		public static readonly Color functionColor =
			EditorGUIUtility.isProSkin ? Color.yellow : new Color(0.2f, 0.56f, 1f);


		static readonly float buttonBackgroundBrightness = EditorGUIUtility.isProSkin ? 0.35f : 0.89f;
		public static readonly Color buttonBackgroundColor =
			new(buttonBackgroundBrightness, buttonBackgroundBrightness, buttonBackgroundBrightness, 1);

		static readonly float buttonBorderBrightness = EditorGUIUtility.isProSkin ? 0.14f : 0.70f;
		public static readonly Color buttonBorderColor =
			new(buttonBorderBrightness, buttonBorderBrightness, buttonBorderBrightness, 1);


		// Drawing Inspector
		public const float indentWidth = 15;
		public const float startSpace = 18;
		public const float endSpace = 5;
		public const float minLabelWidth = 122;
		public const float horizontalSpacing = 5;
		public static float FullWith => EditorGUIUtility.currentViewWidth;
		public static float LabelAndContentWidth => FullWith - startSpace - endSpace;
		static float BaseContentWidth => FullWith * 0.55f + 15;
		static float BaseLabelWidth => LabelAndContentWidth - BaseContentWidth;
		static float NotIndentedLabelWidth => Mathf.Max(minLabelWidth, BaseLabelWidth);
		public static float IndentLevel => EditorGUI.indentLevel;
		public static float IndentsWidth => IndentLevel * indentWidth;

		public static float LabelStartX => IndentsWidth + startSpace;
		public static float LabelWidth => NotIndentedLabelWidth - IndentsWidth;

		public static float ContentStartX => startSpace + IndentsWidth + LabelWidth;
		public static float ContentWidth(Rect position) =>
			position.xMax + EditorGUIUtility.standardVerticalSpacing
			- ContentStartX - EditorGUIUtility.standardVerticalSpacing;

		public static Rect ContentRect(Rect position) =>
			new(ContentStartX, position.y, ContentWidth(position), position.height);
		public static Rect LabelRect(Rect position) =>
			new(position.x, position.y, LabelWidth, EditorGUIUtility.singleLineHeight);

		// Box Drawing

		public static void DrawButtonLikeBox(Rect position, string label = null,
			TextAnchor alignment = TextAnchor.MiddleCenter)
		{
			DrawBox(position, buttonBackgroundColor, buttonBorderColor, borderInside: true);
			if (string.IsNullOrEmpty(label))
				return;
			GUIStyle style = new("Label") { alignment = alignment };
			position.x += 4;
			position.width -= 8;
			GUI.Label(position, label, style);
		}

		public static Rect DrawBox(Rect position, bool borderInside = true) =>
			DrawBox(position, tableBackgroundColor, tableBorderColor, borderInside);

		public static Rect DrawBox(Rect position, Color? backgroundColor, Color? borderColor = null,
			bool borderInside = true)
		{
			float x = Mathf.Round(borderInside ? position.x + 1 : position.x);
			float y = Mathf.Round(borderInside ? position.y + 1 : position.y);
			float w = Mathf.Round(borderInside ? position.width - 2 : position.width);
			float h = Mathf.Round(borderInside ? position.height - 2 : position.height);
			if (backgroundColor != null)
				EditorGUI.DrawRect(position, backgroundColor.Value);
			if (borderColor == null)
				return new Rect(x + 1, y + 1, w - 2, h - 2);

			EditorGUI.DrawRect(new Rect(x - 1, y - 1, 1, h + 2), borderColor.Value);
			EditorGUI.DrawRect(new Rect(x - 1, y - 1, w + 2, 1), borderColor.Value);
			EditorGUI.DrawRect(new Rect(x + w, y - 1, 1, h + 2), borderColor.Value);
			EditorGUI.DrawRect(new Rect(x - 1, y + h, w + 2, 1), borderColor.Value);

			return new Rect(x + 1, y + 1, w - 2, h - 2);
		}

		public static Rect DrawSuccessBox(Rect position, bool borderInside = true) =>
			DrawBox(position, successBackgroundColor, tableBorderColor, borderInside);

		public static Rect DrawErrorBox(Rect position, bool borderInside = true) =>
			DrawBox(position, ErrorBackgroundColor, tableBorderColor, borderInside);
		public static Rect DrawWarningBox(Rect position, bool borderInside = true) =>
			DrawBox(position, WarningBackgroundColor, tableBorderColor, borderInside);

		static Material _mat;

		static Material Mat
		{
			get
			{
				if (_mat == null)
					_mat = new Material(Shader.Find("Hidden/Internal-Colored"));
				return _mat;
			}
		}

		public static void DrawLine(Rect rect, Vector2 a, Vector2 b) => DrawLine(rect, b, b, tableBorderColor);

		public static void DrawLine(Rect rect, Vector2 a, Vector2 b, Color color) =>
			DrawLine(rect, a.x, a.y, b.x, b.y, color);

		public static void DrawLine(Rect rect, float x1, float y1, float x2, float y2) =>
			DrawLine(rect, x1, y1, x2, y2, tableBorderColor);

		public static void DrawLine(Rect rect, float x1, float y1, float x2, float y2, Color color)
		{
			GUI.BeginClip(rect);
			GL.PushMatrix();
			Mat.SetPass(0);
			GL.Begin(GL.LINES);

			GL.Color(color);
			GLVertex2(Mathf.Round(x1 * rect.width), Mathf.Round(y1 * rect.height));
			GLVertex2(Mathf.Round(x2 * rect.width), Mathf.Round(y2 * rect.height));


			GL.End();
			GL.PopMatrix();
			GUI.EndClip();
		}

		public const string scriptPropertyName = "m_Script";
		public static void DrawScriptLine(SerializedObject serializedObject)
		{
			GUI.enabled = false;
			EditorGUILayout.PropertyField(serializedObject.FindProperty(scriptPropertyName));
			GUI.enabled = true;
		}

		public static void DrawFunction(Rect rect, Rect functionArea, Func<float, float> function) =>
			DrawFunction(rect, functionArea, function, functionColor);

		public static void DrawFunction(Rect rect, Rect functionArea, Func<float, float> function, Color color)
		{
			GUI.BeginClip(rect);
			GL.PushMatrix();
			Mat.SetPass(0);
			GL.Begin(GL.LINE_STRIP);
			GL.Color(color);
			for (int xp = 0; xp < rect.width; xp++)
			{
				float x = LerpUnclamped(xp, functionArea.xMin, functionArea.xMax, 0, rect.width);
				float fx = function(x);
				float yp = Lerp(fx, rect.height, 0, functionArea.yMin, functionArea.yMax);
				GLVertex2(xp, yp);
			}

			GL.End();
			GL.PopMatrix();
			GUI.EndClip();
		}

		static float Lerp(float input, float minOutput, float maxOutput, float minInput = 0, float maxInput = 1)
		{
			if (input <= minInput)
				return minOutput;
			if (input >= maxInput)
				return maxOutput;
			return minOutput + (input - minInput) / (maxInput - minInput) * (maxOutput - minOutput);
		}

		static float LerpUnclamped(float input, float minOutput, float maxOutput, float minInput = 0, float maxInput = 1) =>
			minOutput + (input - minInput) / (maxInput - minInput) * (maxOutput - minOutput);

		static void GLVertex2(float x, float y)
		{
			if (y < 0)
				y = 0;
			GL.Vertex3(x, y, 0);
		}

		public static object AnythingField(Rect position, Type t, object value, GUIContent label, ref bool isExpanded)
		{
			if (t == typeof(bool))
				return EditorGUI.Toggle(position, label, (bool)value);
			if (t == typeof(int))
				return EditorGUI.IntField(position, label, (int)value);
			if (t == typeof(float))
				return EditorGUI.FloatField(position, label, (float)value);
			if (t == typeof(string))
				return EditorGUI.TextField(position, label, (string)value);
			if (t == typeof(Vector2))
				return EditorGUI.Vector2Field(position, label, (Vector2)value);
			if (t == typeof(Vector3))
				return EditorGUI.Vector3Field(position, label, (Vector3)value);
			if (t == typeof(Vector4))
				return EditorGUI.Vector4Field(position, label, (Vector4)value);
			if (t == typeof(Vector2Int))
				return EditorGUI.Vector2IntField(position, label, (Vector2Int)value);
			if (t == typeof(Vector3Int))
				return EditorGUI.Vector3IntField(position, label, (Vector3Int)value);
			if (t == typeof(Color))
				return EditorGUI.ColorField(position, label, (Color)value);
			if (t == typeof(Gradient))
				return EditorGUI.GradientField(position, label, (Gradient)value);
			if (t == typeof(Rect))
				return EditorGUI.RectField(position, label, (Rect)value);
			if (t == typeof(Bounds))
				return EditorGUI.BoundsField(position, label, (Bounds)value);
			if (t == typeof(AnimationCurve))
				return EditorGUI.CurveField(position, label, (AnimationCurve)value);
			if (t == typeof(double))
				return EditorGUI.DoubleField(position, label, (double)value);
			if (t == typeof(long))
				return EditorGUI.LongField(position, label, (long)value);
			if (t.IsSubclassOf(typeof(Object)))
				return EditorGUI.ObjectField(position, label, (Object)value, t, true);
			if (t == typeof(RectInt))
				return EditorGUI.RectIntField(position, label, (RectInt)value);
			if (t == typeof(BoundsInt))
				return EditorGUI.BoundsIntField(position, label, (BoundsInt)value);
			if (t.IsSubclassOf(typeof(Enum)))
				return EditorGUI.EnumPopup(position, label, (Enum)value);
			if (t == typeof(Matrix4x4))
				return Nice4X4MatrixDrawer.Draw(position, label, (Matrix4x4)value, ref isExpanded);  // Add Universal solution

			return null;
		}

		public static float GetStandardPanelHeight(int standardLineCount) =>
			standardLineCount * EditorGUIUtility.singleLineHeight + (standardLineCount - 1) * EditorGUIUtility.standardVerticalSpacing;

		public static T DrawEnumToggle<T>(T enumValue, Func<T, bool> isEnabled = null) where T : Enum => DrawEnumToggle(null as GUIContent, enumValue, isEnabled);
		public static T DrawEnumToggle<T>(string label, T enumValue, Func<T, bool> isEnabled = null) where T : Enum => DrawEnumToggle(new GUIContent(label), enumValue, isEnabled);

		public static T DrawEnumToggle<T>(GUIContent label, T enumValue, Func<T, bool> isEnabled = null) where T : Enum
		{
			List<T> enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
			EditorGUILayout.BeginHorizontal();

			if (label != null && label != GUIContent.none)
				EditorGUILayout.LabelField(label);

			for (int i = 0; i < enumValues.Count; i++)
			{
				if (isEnabled != null && !isEnabled(enumValues[i]))
					GUI.enabled = false;

				T value = enumValues[i];
				bool isSelected = value.Equals(enumValue);

				GUIStyle style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
				if (GUILayout.Toggle(isSelected, value.ToString(), style))
					enumValue = value;

				GUI.enabled = true;
			}

			EditorGUILayout.EndHorizontal();
			return enumValue;
		}


		public static T DrawEnumToggle<T>(Rect position, T enumValue, Func<T, bool> isEnabled = null) where T : Enum => DrawEnumToggle(position, null as GUIContent, enumValue, isEnabled);
		public static T DrawEnumToggle<T>(Rect position, string label, T enumValue, Func<T, bool> isEnabled = null) where T : Enum => DrawEnumToggle(position, new GUIContent(label), enumValue, isEnabled);

		public static T DrawEnumToggle<T>(Rect position, GUIContent label, T enumValue, Func<T, bool> isEnabled = null) where T : Enum
		{
			List<T> enumValues = System.Enum.GetValues(typeof(T)).Cast<T>().ToList();

			Rect labelRect = position.SliceOut(LabelWidth, Side.Left, false);
			if (label != null && label != GUIContent.none)
				EditorGUI.LabelField(labelRect, label);

			float buttonWidth = (position.width - ((enumValues.Count - 1) * EditorGUIUtility.standardVerticalSpacing)) / enumValues.Count;

			for (int i = 0; i < enumValues.Count; i++)
			{
				if (isEnabled != null && !isEnabled(enumValues[i]))
					GUI.enabled = false;

				T value = enumValues[i];
				bool isSelected = value.Equals(enumValue);

				GUIStyle style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
				Rect buttonRect = position.SliceOut(buttonWidth, Side.Left);
				if (GUI.Toggle(buttonRect, isSelected, value.ToString(), style))
					enumValue = value;

				GUI.enabled = true;
			}

			return enumValue;
		}
	}

}
#endif