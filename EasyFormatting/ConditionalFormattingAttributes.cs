using System;
using System.Collections.Generic; 
using System.Linq; 
using Debug = UnityEngine.Debug;

namespace EasyEditor
{
	public abstract class ConditionalFormattingAttribute : FormattingAttribute
	{
		public readonly string[] conditionMembers;
		readonly List<Func<object, bool>> _conditionGetters = new();
		bool _initialized;

		public void Initialize(object owner)
		{
#if UNITY_EDITOR
			if (_initialized)
				return;
			_conditionGetters.Clear();
			foreach (string conditionMemberName in conditionMembers)
			{
				InspectorDrawingUtility.TryGetAGetterFromMember(owner.GetType(), conditionMemberName, out Func<object, bool> condition);
				if (condition != null)
					_conditionGetters.Add(condition);
				else
					Debug.LogError($"Condition Member {conditionMemberName} not found in {owner.GetType().Name}."+
					$"It might have been implemented in a base class. If so, make it protected.");
			}
			_initialized = true;
#endif
		}
		public ConditionalFormattingAttribute(params string[] conditionMembers) =>
			this.conditionMembers = conditionMembers;

		public bool CheckConditions(object owner) => _conditionGetters.All(t => t.Invoke(owner));
	}

	public class ShowIfAttribute : ConditionalFormattingAttribute
	{
		public ShowIfAttribute(params string[] conditionMembers) : base(conditionMembers) { }
	}

	public class HideIfAttribute : ConditionalFormattingAttribute
	{
		public HideIfAttribute(params string[] conditionMembers) : base(conditionMembers) { }
	}
	public class EnableIfAttribute : ConditionalFormattingAttribute
	{
		public EnableIfAttribute(params string[] conditionMembers) : base(conditionMembers) { }
	}
	public class DisableIfAttribute : ConditionalFormattingAttribute
	{
		public DisableIfAttribute(params string[] conditionMembers) : base(conditionMembers) { }
	}
}