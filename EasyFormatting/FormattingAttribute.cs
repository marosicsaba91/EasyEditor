using System;
using UnityEngine;

namespace EasyEditor
{
	[AttributeUsage(AttributeTargets.Field)]
	public abstract class FormattingAttribute : PropertyAttribute { }
}