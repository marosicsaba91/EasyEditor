using System;

namespace EasyInspector
{
	public class LabelAttribute : FormattingAttribute 
	{
		public string TextValue { get; }
		bool _initialized;
		Func<object, string> _labelGetter;

		public void Initialize(object owner)
		{
#if UNITY_EDITOR
			if (_initialized)
				return;

			InspectorDrawingUtility.TryGetAGetterFromMember(owner.GetType(), TextValue, out _labelGetter);
			_initialized = true;
#endif
		}

		internal string GetLabel(object owner) =>
			_labelGetter != null ? _labelGetter.Invoke(owner) : TextValue;

		public LabelAttribute(string text)
		{
			TextValue = text;
		}
	}
}