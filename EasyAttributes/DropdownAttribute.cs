using System;
using UnityEngine;

namespace EasyEditor
{
	[Serializable]
	public class DropdownAttribute : PropertyAttribute
	{ 
		[NonSerialized] public string nameOfOptions;
		[NonSerialized] public bool enableNoneOption;

		public DropdownAttribute(string options, bool enableNone = true)
		{
			nameOfOptions = options;
			enableNoneOption = enableNone;
		} 
	}
}