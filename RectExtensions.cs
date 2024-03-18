using UnityEngine;

namespace EasyEditor
{
	public enum Side
	{
		Up,
		Down,
		Left,
		Right
	}

	public static class RectExtensions
	{
		const float standardVerticalSpacing = 2;
		const float singleLineHeight = 16;

		public static Rect Combine(this Rect self, Rect other)
		{
			float left = Mathf.Min(self.position.x, other.position.x);
			float bottom = Mathf.Min(self.position.y, other.position.y);
			float right = Mathf.Max(self.position.x + self.size.x, other.position.x + other.size.x);
			float top = Mathf.Max(self.position.y + self.size.y, other.position.y + other.size.y);

			return new Rect(left, bottom, right - left, top - bottom);
		}

		public static Rect Crop(this Rect self, Rect crop)
		{
			float left = Mathf.Max(self.xMin, crop.xMin);
			float right = Mathf.Min(self.xMax, crop.xMax);
			float bottom = Mathf.Max(self.yMin, crop.yMin);
			float top = Mathf.Min(self.yMax, crop.yMax);

			return new Rect(left, bottom, right - left, top - bottom);
		}

		public static Rect ChangeLeft(this Rect self, float newValue)
		{
			Rect temp = self;
			temp.xMin = newValue;
			return temp;
		}

		public static Rect ChangeRight(this Rect self, float newValue)
		{
			Rect temp = self;
			temp.xMax = newValue;
			return temp;
		}

		public static Rect ChangeTop(this Rect self, float newValue)
		{
			Rect temp = self;
			temp.yMax = newValue;
			return temp;
		}

		public static Rect ChangeBottom(this Rect self, float newValue)
		{
			Rect temp = self;
			temp.yMin = newValue;
			return temp;
		}


		public static Vector2 TopLeft(this Rect self) => new(self.xMin, self.yMax);

		public static Vector2 TopRight(this Rect self) => new(self.xMax, self.yMax);

		public static Vector2 BottomLeft(this Rect self) => new(self.xMin, self.yMin);

		public static Vector2 BottomRight(this Rect self) => new(self.xMax, self.yMin);

		public static Vector2 LeftPoint(this Rect self) => new(self.xMin, self.center.y);

		public static Vector2 RightPoint(this Rect self) => new(self.xMax, self.center.y);

		public static Vector2 TopPoint(this Rect self) => new(self.center.x, self.yMax);

		public static Vector2 BottomPoint(this Rect self) => new(self.center.x, self.yMin);


		public static void RemoveOneSpace(this ref Rect self, Side side = Side.Up)
		{
			self.RemoveSpace(standardVerticalSpacing, side);
		}

		public static void RemoveSpace(this ref Rect self, float space, Side side = Side.Up)
		{
			if (side is Side.Up)
			{
				self.y += space;
				self.height -= space;
			}
			else if (side is Side.Down)
			{
				self.height -= space;
			}
			else if (side is Side.Left)
			{
				self.x += space;
				self.width -= space;
			}
			else if (side is Side.Right)
			{
				self.width -= space;
			}
		}

		public static Rect SliceOutLine(this ref Rect self, Side side = Side.Up, bool addSpace = true)
		{
			return self.SliceOut(singleLineHeight, side, addSpace);
		}

		public static Rect SliceOut(this ref Rect self, float pixels, Side side = Side.Up, bool addSpace = true)
		{
			Rect slice = self;
			if (side is Side.Up or Side.Down)
			{
				slice.height = pixels;

				float newHeight = self.height - pixels;
				if (addSpace)
					newHeight -= standardVerticalSpacing;
				self.height = Mathf.Max(0, newHeight);

				if (side == Side.Down)
				{

					if (newHeight < 0)
						self.y -= newHeight;

					slice.y = self.yMax;
					if (addSpace)
						slice.y += standardVerticalSpacing;
				}
				else
				{
					self.y += pixels;

					if (addSpace)
						self.y += standardVerticalSpacing;
				}
			}
			else
			{
				slice.width = pixels;
				float newWidth = self.width - pixels;
				if (addSpace) 
					newWidth -= standardVerticalSpacing;
				self.width = Mathf.Max(0, newWidth);

				if (side == Side.Right)
				{
					if (newWidth < 0)
						self.x -= newWidth;

					slice.x = self.xMax;
					if (addSpace)
						self.width -= standardVerticalSpacing;

				}
				else
				{
					self.x += pixels;
					if (addSpace)
						self.x += standardVerticalSpacing;
				}
			}

			return slice;
		}
	}
}

