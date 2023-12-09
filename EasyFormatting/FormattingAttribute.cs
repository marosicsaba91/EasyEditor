using System;
using UnityEngine;

namespace EasyInspector
{
	[AttributeUsage(AttributeTargets.Field)]
	public abstract class FormattingAttribute : PropertyAttribute { }
	public class ReadOnlyAttribute : FormattingAttribute { }
}