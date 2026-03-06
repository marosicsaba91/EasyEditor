using System;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class Range2Attribute : PropertyAttribute
	{
		public readonly float xMin;
		public readonly float xMax;
		public readonly float yMin;
		public readonly float yMax;
		public readonly float? height;

		/// <summary>
		///   <para>Attribute used to make a Vector2 or Vector2Int variable in a script be restricted to a specific range.</para>
		/// </summary>
		/// <param name="xMin">The minimum X allowed value.</param>
		/// <param name="xMax">The maximum X allowed value.</param>
		/// <param name="yMin">The minimum Y allowed value.</param>
		/// <param name="yMax">The maximum Y allowed value.</param> 
		public Range2Attribute(float xMin, float xMax, float yMin, float yMax)
		{
			this.xMin = xMin;
			this.xMax = xMax;
			this.yMin = yMin;
			this.yMax = yMax;
			height = null;
		}


		/// <summary>
		///   <para>Attribute used to make a Vector2 or Vector2Int variable in a script be restricted to a specific range.</para>
		/// </summary>
		/// <param name="xMin">The minimum X allowed value.</param>
		/// <param name="xMax">The maximum X allowed value.</param>
		/// <param name="yMin">The minimum Y allowed value.</param>
		/// <param name="yMax">The maximum Y allowed value.</param> 
		/// <param name="height">The height of the UI Control in the Inspector window.</param> 
		public Range2Attribute(float xMin, float xMax, float yMin, float yMax, float height)
		{
			this.xMin = xMin;
			this.xMax = xMax;
			this.yMin = yMin;
			this.yMax = yMax;
			this.height = height;
		}
	}
}


#if UNITY_EDITOR

namespace EasyEditor.Editor
{

	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(Range2Attribute))]
	public class Range2AttributeDrawer : PropertyDrawer
	{
		const float drawingHeight = 70;
		const float minimumDrawingHeight = 30;
		float DrawingHeight
		{
			get
			{
				Range2Attribute range = attribute as Range2Attribute;
				if (range?.height == null)
					return drawingHeight;
				return Mathf.Max(minimumDrawingHeight, range.height.Value);
			}
		}

		bool _clicked = false;


		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = base.GetPropertyHeight(property, label);
			Range2Attribute range = attribute as Range2Attribute;
			bool v2I = property.propertyType == SerializedPropertyType.Vector2Int;
			if (property.propertyType != SerializedPropertyType.Vector2 && !v2I)
			{
				EditorGUI.LabelField(position, label.text, "Use Range with Vector2 or Vector2Int.");
				return;
			}

			property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
			Vector2 oldValue = v2I
				? new Vector2(Mathf.RoundToInt(property.vector2IntValue.x), Mathf.RoundToInt(property.vector2IntValue.y))
				: property.vector2Value;
			position.x = EditorHelper.ContentStartX;
			position.width -= EditorHelper.LabelWidth;
			TrySetValue(EditorGUI.Vector2Field(position, GUIContent.none, oldValue));

			if (!property.isExpanded)
				return;

			Rect drawingRect = new()
			{
				x = position.x,
				width = position.width,
				y = position.yMax + EditorGUIUtility.standardVerticalSpacing,
				height = DrawingHeight
			};
			EditorHelper.DrawBox(drawingRect);

			Rect verticalSliderRect = new()
			{
				x = Lerp(oldValue.x, drawingRect.xMin + 2, drawingRect.xMax - 12, range.xMin,
					range.xMax),
				y = drawingRect.y + 2,
				height = drawingRect.height - 4,
				width = 0
			};

			Rect horizontalSliderRect = new()
			{
				x = drawingRect.x + 2,
				y = Lerp(oldValue.y, drawingRect.yMin - 2, drawingRect.yMax - 16, range.yMin,
					range.yMax),
				height = 0,
				width = drawingRect.width - 4
			};

			if (Event.current.type == EventType.Repaint)
			{
				GUI.skin.verticalSlider.Draw(verticalSliderRect, false, false, false, false);
				GUI.HorizontalSlider(horizontalSliderRect, oldValue.x, range.xMin, range.xMax);
			}
			else if (Event.current.type == EventType.MouseDown)
				_clicked = drawingRect.Contains(Event.current.mousePosition);
			else if (Event.current.type == EventType.MouseUp)
				_clicked = false;

			if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && _clicked)
			{
				Vector2 p = Event.current.mousePosition;
				Vector2 newPos = new(
					Lerp(p.x, range.xMin,
						range.xMax, drawingRect.xMin + 7, drawingRect.xMax - 8),
					Lerp(p.y, range.yMin,
						range.yMax, drawingRect.yMin + 7, drawingRect.yMax - 8));
				TrySetValue(newPos);
				Event.current.Use();
			}

			static float Lerp(float input, float minOutput, float maxOutput, float minInput = 0, float maxInput = 1)
			{
				if (input <= minInput)
					return minOutput;
				if (input >= maxInput)
					return maxOutput;
				return minOutput + (input - minInput) / (maxInput - minInput) * (maxOutput - minOutput);
			}

			void TrySetValue(Vector2 newValue)
			{
				if (oldValue == newValue)
					return;
				if (!v2I)
				{
					newValue.x = Mathf.Clamp(newValue.x, range.xMin, range.xMax);
					newValue.y = Mathf.Clamp(newValue.y, range.yMin, range.yMax);
					if (oldValue == newValue)
						return;
					property.vector2Value = newValue;
				}
				else
				{
					int ox = Mathf.RoundToInt(oldValue.x);
					int oy = Mathf.RoundToInt(oldValue.y);
					int x = Mathf.RoundToInt(newValue.x);
					int y = Mathf.RoundToInt(newValue.y);
					int xMin = Mathf.RoundToInt(range.xMin);
					int xMax = Mathf.RoundToInt(range.xMax);
					int yMin = Mathf.RoundToInt(range.yMin);
					int yMax = Mathf.RoundToInt(range.yMax);
					x = x < xMin ? xMin : x > xMax ? xMax : x;
					y = y < yMin ? yMin : y > yMax ? yMax : y;
					if (x == ox && y == oy)
						return;
					property.vector2IntValue = new Vector2Int(x, y);
				}

				property.serializedObject.ApplyModifiedProperties();
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.isExpanded)
				return base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing + DrawingHeight;
			return base.GetPropertyHeight(property, label);
		}
	}
}

#endif
