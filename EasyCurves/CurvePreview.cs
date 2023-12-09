#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EasyInspector
{
	public class CurvePreview
	{
		public static readonly float valueLabelHeight = EditorGUIUtility.singleLineHeight;
		public const float valueLabelWidth = 60;
		public const float defaultZoom = 1.25f;

		public float zoom = defaultZoom;
		public Vector2 offset = Vector2.zero;
		Vector2 _dragStart;

		public float Zoom
		{
			get => zoom;
			set => zoom = Mathf.Clamp(value, 0.001f, 1000);
		}

		static readonly GUIStyle centerRightAlignedText = new(GUI.skin.label)
		{ alignment = TextAnchor.UpperCenter };


		static readonly GUIStyle upperRightAlignedText = new(GUI.skin.label)
		{ alignment = TextAnchor.MiddleRight };


		public void Draw(Rect position, AnimationCurve curve, Color color) =>
			Draw(position, curve.Evaluate, AnimationCurveContainingRect(curve), color);

		public static Rect AnimationCurveContainingRect(AnimationCurve function)
		{
			if (function == null || function.keys.Length == 0)
				return new Rect(x: -1, y: -1, width: 2, height: 2);

			Vector2 min = new(float.MaxValue, float.MaxValue);
			Vector2 max = new(float.MinValue, float.MinValue);

			foreach (Keyframe keyframe in function.keys)
			{
				if (keyframe.time < min.x)
					min.x = keyframe.time;
				if (keyframe.time > max.x)
					max.x = keyframe.time;

				if (keyframe.value < min.y)
					min.y = keyframe.value;
				if (keyframe.value > max.y)
					max.y = keyframe.value;
			}

			return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
		}

		public void Draw(Rect position, Func<float, float> function, Rect mustContainArea, Color color, bool showMargin = true)
		{
			if (showMargin)
			{
				position.y += valueLabelHeight;
				position.height -= valueLabelHeight;
			}

			mustContainArea.position -= mustContainArea.size * ((Zoom - 1) / 2);
			mustContainArea.size *= Zoom;
			if (Event.current.type != EventType.MouseDrag)
				mustContainArea.position += offset;


			Rect ranges = GetUndistortedArea(position, mustContainArea);

			bool showValuePreview = false;
			Vector2 valuePreviewPixel = Vector2.zero;
			float valuePreviewY = 0;
			float valuePreviewX = 0;

			// Handle Zooming
			if (position.Contains(Event.current.mousePosition))
			{
				if (Event.current.type == EventType.MouseMove)
				{
					Debug.Log("USE");
					Event.current.Use();
				}

				else if (Event.current.type == EventType.ScrollWheel)
				{
					Zoom *= 1 + Event.current.delta.y / 20;
					Event.current.Use();
				}
				else if (Event.current.type == EventType.MouseDown)
				{
					if (Event.current.button == 0) // Left Click
						_dragStart = ScreenToFunctionPosition(Event.current.mousePosition, position, ranges);
					else
						Reset();

					Event.current.Use();
				}
				else if (Event.current.type == EventType.MouseDrag)
				{
					if (Event.current.button == 0) //  Not Left Click
						offset = _dragStart - ScreenToFunctionPosition(Event.current.mousePosition, position, ranges);
					mustContainArea.position += offset;
					Event.current.Use();
				}
				else
				{
					valuePreviewPixel.x = Event.current.mousePosition.x;
					valuePreviewX = ScreenToFunctionPositionX(valuePreviewPixel.x, position, ranges);
					valuePreviewY = function(valuePreviewX);
					valuePreviewPixel.y = Lerp(
						valuePreviewY,
						position.yMax,
						position.yMin,
						ranges.yMin,
						ranges.yMax);

					const float minimumDistanceToShowValuePreview = 15;
					if (Mathf.Abs(valuePreviewPixel.y - Event.current.mousePosition.y) <
						minimumDistanceToShowValuePreview)
						showValuePreview = true;
				}
			}

			int intent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			EditorHelper.DrawBox(position);
			DrawGrid(position, ranges, EditorHelper.tableBorderColor, showMargin);
			EditorHelper.DrawFunction(position, ranges, function, color);

			if (showValuePreview)
			{
				Rect r = new(
					Event.current.mousePosition.x - valueLabelWidth, valuePreviewPixel.y - valueLabelHeight / 2,
					valueLabelWidth, valueLabelHeight
				);

				const float valuePreviewLineHeight = 10;
				float x = (valuePreviewPixel.x - position.x) / position.width;
				float y1 = (valuePreviewPixel.y - position.y - valuePreviewLineHeight / 2) / position.height;
				float y2 = (valuePreviewPixel.y - position.y + valuePreviewLineHeight / 2) / position.height;
				EditorHelper.DrawLine(position, x, y1, x, y2, color);
				EditorGUI.LabelField(r, "Y:" + valuePreviewY.ToString("0.00"), centerRightAlignedText);
				r.y -= valueLabelHeight;
				EditorGUI.LabelField(r, "X:" + valuePreviewX.ToString("0.00"), centerRightAlignedText);
			}

			EditorGUI.indentLevel = intent;
		}

		Vector2 ScreenToFunctionPosition(Vector2 pos, Rect position, Rect ranges) => new(
			ScreenToFunctionPositionX(pos.x, position, ranges),
			ScreenToFunctionPositionY(pos.y, position, ranges));

		float ScreenToFunctionPositionX(float x, Rect position, Rect ranges) => LerpUnclamped(
			x,
			ranges.xMin,
			ranges.xMax,
			position.xMin,
			position.xMax);

		float ScreenToFunctionPositionY(float y, Rect position, Rect ranges) => LerpUnclamped(
			y,
			ranges.yMax,
			ranges.yMin,
			position.yMin,
			position.yMax);

		public void Reset()
		{
			zoom = defaultZoom;
			offset = Vector2.zero;
		}

		static Rect GetUndistortedArea(Rect position, Rect mustContainArea)
		{
			float ratio = position.width / position.height;
			Rect ranges = new()
			{
				size = ratio < mustContainArea.width / mustContainArea.height
					? new Vector2(mustContainArea.width, mustContainArea.width / ratio)
					: new Vector2(mustContainArea.height * ratio, mustContainArea.height),
				center = mustContainArea.center
			};

			return ranges;
		}

		static void DrawGrid(Rect position, Rect ranges, Color color, bool showMargin)
		{
			const int smallInBig = 5;
			const int logBase = 5;
			const int roundDigit = 5;

			const float axisAlpha = 0.8f;
			const float bigGridAlpha = 0.5f;
			const float smallGridAlpha = 0.25f;

			Color axisColor = new(color.r, color.g, color.b, axisAlpha);

			float minSize = Mathf.Min(ranges.width, ranges.height);
			float log = Mathf.Log(minSize, logBase);
			int exponentOf10 = Mathf.FloorToInt(log);
			float rate = Mod(log, 1);
			float bigStep = Mathf.Pow(logBase, exponentOf10);
			float smallStep = bigStep / smallInBig;

			Color bigGridColor = new(color.r, color.g, color.b, Mathf.Lerp(bigGridAlpha, smallGridAlpha, rate));
			Color smallGridColor = new(color.r, color.g, color.b, Mathf.Lerp(smallGridAlpha, 0, rate));

			if (!showMargin)
				return;


			// Horizontal Values:
			float start = Mathf.Floor(ranges.xMin / smallStep) * smallStep;
			float end = Mathf.Ceil(ranges.xMax / smallStep) * smallStep;
			int index = Mod(Mathf.FloorToInt(ranges.xMin / smallStep), smallInBig);
			for (float value = start; value <= end; value += smallStep)
			{
				bool big = index % smallInBig == 0;
				value = (float)Math.Round(value, roundDigit);
				Color c =
					value == 0 ? axisColor :
					big ? bigGridColor :
					smallGridColor;
				DrawHorizontalValue(position, value, ranges.xMin, ranges.xMax, c, big);
				index++;
			}

			// Vertical Values:
			start = Mathf.Floor(ranges.yMin / smallStep) * smallStep;
			end = Mathf.Ceil(ranges.yMax / smallStep) * smallStep;
			index = Mod(Mathf.FloorToInt(ranges.yMin / smallStep), smallInBig);
			for (float value = start; value <= end; value += smallStep)
			{
				bool big = index % smallInBig == 0;
				value = (float)Math.Round(value, roundDigit);
				Color c =
					value == 0 ? axisColor :
					big ? bigGridColor :
					smallGridColor;
				DrawVerticalValue(position, value, ranges.yMin, ranges.yMax, c, big);
				index++;
			}
		}

		static void DrawHorizontalValue(Rect position, float value, float min, float max, Color color,
			bool showValue = true, bool showLine = true)
		{
			if (value < min || value > max)
				return;

			float rate = (value - min) / (max - min);

			if (showLine)
				EditorHelper.DrawLine(position, rate, 0, rate, 1, color);
			if (!showValue)
				return;

			float x = Mathf.Lerp(
				position.x,
				position.x + position.width, rate);
			int xPixel = Mathf.RoundToInt(x - valueLabelWidth / 2f);
			Rect pos = new(xPixel, position.y - valueLabelHeight, valueLabelWidth, valueLabelHeight);
			EditorGUI.LabelField(pos, NormalString(value), centerRightAlignedText);
		}

		static void DrawVerticalValue(Rect position, float value, float min, float max, Color color,
			bool showValue = true, bool showLine = true)
		{
			if (value < min || value > max)
				return;

			float rate = 1 - (value - min) / (max - min);

			if (showLine)
				EditorHelper.DrawLine(position, 0, rate, 1, rate, color);
			if (!showValue)
				return;

			float y = Mathf.Lerp(
				position.y,
				position.y + position.height, rate);
			int yPixel = Mathf.RoundToInt(y - valueLabelHeight / 2f);
			Rect pos = new(position.x - valueLabelWidth, yPixel, valueLabelWidth, valueLabelHeight);
			EditorGUI.LabelField(pos, NormalString(value), upperRightAlignedText);
		}

		static string NormalString(float value)
		{
			if (value == 0)
				return "0";

			float abs = Mathf.Abs(value);
			if (abs >= 100)
				return value.ToString("0.");
			if (abs >= 10)
				return value.ToString("0.#");
			if (abs >= 0.02)
				return value.ToString("0.##");
			return value.ToString("0.#E+0");
		}

		static float Lerp(float input, float minOutput, float maxOutput, float minInput = 0,
			float maxInput = 1)
		{
			if (input <= minInput)
				return minOutput;
			if (input >= maxInput)
				return maxOutput;
			return minOutput + (input - minInput) / (maxInput - minInput) * (maxOutput - minOutput);
		}
		static float LerpUnclamped(float input, float minOutput, float maxOutput,
			float minInput = 0, float maxInput = 1)
		{
			return minOutput + (input - minInput) / (maxInput - minInput) * (maxOutput - minOutput);
		}
		public static int Mod(int n, int m) => (n % m + m) % m;
		public static float Mod(float n, float m) => (n % m + m) % m;
	}
}
#endif