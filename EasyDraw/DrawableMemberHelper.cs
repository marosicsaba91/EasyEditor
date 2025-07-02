#if UNITY_EDITOR

using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using System;

namespace EasyEditor.Internal
{
	static class DrawableMemberHelper
	{
		public static bool extraUIDrawing;

		internal static void DrawExtraInspectorGUI(Object target, SerializedObject serializedObject, List<DrawableMember> members)
		{
			if (members == null)
				return;

			serializedObject.Update();
			extraUIDrawing = true;
			foreach (DrawableMember dm in members)
				dm.TryDraw(target);
			extraUIDrawing = false;
		}


		internal static void FindDrawableMembers(Object target, SerializedObject serializedObject, List<DrawableMember> extraMembersToDraw)
		{
			if (target == null)
				return;
			Type type = target.GetType();
			BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

			extraMembersToDraw.Clear();
			foreach (MemberInfo member in type.GetMembers(bindings))
			{
				if (!Attribute.IsDefined(member, typeof(EasyDrawAttribute)))
					continue;

				EasyDrawAttribute attribute = (EasyDrawAttribute)Attribute.GetCustomAttribute(member, typeof(EasyDrawAttribute));


				if (DrawableMember.TryCreate(member, attribute, serializedObject, out DrawableMember drawableMember))
				{
					extraMembersToDraw ??= new();
					extraMembersToDraw.Add(drawableMember);
				}
			}

			extraMembersToDraw?.Sort((a, b) => a.index.CompareTo(b.index));
		}
	}
}
#endif