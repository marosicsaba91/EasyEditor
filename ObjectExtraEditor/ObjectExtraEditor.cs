#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EasyEditor.Internal
{
    [CustomEditor(typeof(Object), true), CanEditMultipleObjects]
    public class ObjectExtraEditor : UnityEditor.Editor
    {
        List<DrawableMember> _extraMembersToDraw;
        public static bool extraUIDrawing;

        void OnEnable()
        {
            if (target == null)
                return;

            Type type = target.GetType();
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (MemberInfo member in type.GetMembers(bindings))
            {
                if (!Attribute.IsDefined(member, typeof(EasyDrawAttribute)))
                    continue;

                EasyDrawAttribute attribute = (EasyDrawAttribute)Attribute.GetCustomAttribute(member, typeof(EasyDrawAttribute));


                if (DrawableMember.TryCreate(member, attribute, serializedObject, out DrawableMember drawableMember))
                {
                    _extraMembersToDraw ??= new();
                    _extraMembersToDraw.Add(drawableMember);
                }
            }

            _extraMembersToDraw?.Sort((a, b) => a.index.CompareTo(b.index));
        }

        public override void OnInspectorGUI()
        {
            extraUIDrawing = false;
            base.OnInspectorGUI();

            extraUIDrawing = true;
            DrawExtraInspectorGUI();
            extraUIDrawing = false;
        }

        public void DrawExtraInspectorGUI()
        {
            if (_extraMembersToDraw == null)
                return;

            serializedObject.Update();
            extraUIDrawing = true;
            foreach (DrawableMember dm in _extraMembersToDraw)
                dm.TryDraw(target);
            extraUIDrawing = false;
        }
    }
}
#endif