using System;
using UnityEngine;

namespace EasyEditor
{
	[AttributeUsage(AttributeTargets.Field)]
	public abstract class FormattingAttribute : PropertyAttribute { }
	public class ReadOnlyAttribute : FormattingAttribute { }
}