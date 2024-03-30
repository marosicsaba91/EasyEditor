using System;
using UnityEngine;

namespace EasyEditor
{
	public enum DisplayColor
	{
		White,
		Black,
		Gray,
		Red,
		Green,
		Blue,
		Cyan,
		Magenta,
		Yellow,
		Brown,
		Orange,
		Purple,
	}
	public class ColorAttribute : FormattingAttribute
	{
		static Color ToColor(DisplayColor displayColor) => displayColor switch
		{
			DisplayColor.Black => Color.black,
			DisplayColor.White => Color.white,
			DisplayColor.Gray => Color.gray,
			DisplayColor.Red => new Color(1f, 0.42f, 0.35f),
			DisplayColor.Green => new Color(0.54f, 0.75f, 0.35f),
			DisplayColor.Blue => new Color(0.27f, 0.51f, 0.82f),
			DisplayColor.Cyan => new Color(0.4f, 0.93f, 0.92f),
			DisplayColor.Magenta => new Color(1f, 0.49f, 0.64f),
			DisplayColor.Yellow => new Color(1f, 0.73f, 0.25f),
			DisplayColor.Brown => new Color(0.48f, 0.36f, 0.29f),
			DisplayColor.Orange => new Color(0.96f, 0.6f, 0.36f),
			DisplayColor.Purple => new Color(0.51f, 0.43f, 0.78f),
			_ => Color.white,
		};


		readonly DisplayColor? _fixColor;
		readonly string _colorMember;
		Func<object, Color> _colorGetter;
		bool _initialized;

		public ColorAttribute(string colorMemberName)
		{
			_colorMember = colorMemberName;
			_initialized = false;
		}

		public ColorAttribute(DisplayColor fixColor)
		{
			_fixColor = fixColor;
			_initialized = true;
		}

		public void Initialize(object owner)
		{
			if (_initialized)
				return;
#if UNITY_EDITOR

			InspectorDrawingUtility.TryGetAGetterFromMember(owner.GetType(), _colorMember, out _colorGetter);
			_initialized = true;
#endif
		}

		public bool TryGetColor(object owner, out Color color)
		{
			if (_fixColor.HasValue)
			{
				color = ToColor(_fixColor.Value);
				return true;
			}

			if (_colorGetter != null)
			{
				color = _colorGetter(owner);
				return true;
			}

			color = default;
			return false;
		}
	}
}