using System;
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