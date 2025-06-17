#if UNITY_EDITOR

using Object = UnityEngine.Object;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using System;
using static UnityEngine.GraphicsBuffer;

namespace EasyEditor.Internal
{
    abstract class DrawableMember
    {
        protected GUIContent niceName; 
        public int index;

        public void TryDraw(Object target)
        {
            Draw(target);
        }

        protected abstract void Draw(Object target);

        public static bool TryCreate(MemberInfo member, EasyDrawAttribute eda, SerializedObject serializedObject, out DrawableMember drawableMember)
        {
            drawableMember = TryCreate_Private(member, serializedObject);
            if (drawableMember == null)
                return false;

            drawableMember.index = eda.index;
            drawableMember.niceName = new(ObjectNames.NicifyVariableName(member.Name));
            return true;
        }

        static DrawableMember TryCreate_Private(MemberInfo member, SerializedObject serializedObject)
        {
            MethodInfo methodInfo = member as MethodInfo;
            if (methodInfo != null && methodInfo.GetParameters().Length == 0)
                return new DrawableButtonMethod(methodInfo);

            PropertyInfo propertyInfo = member as PropertyInfo;
            if (propertyInfo != null)
                return new DrawableProperty(propertyInfo, serializedObject);

            FieldInfo fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
                return new DrawableProperty(fieldInfo, serializedObject);

            return new DrawableDummyMember();
        }
    }

    class DrawableButtonMethod : DrawableMember
    {
        readonly MethodInfo methodInfo;

        public DrawableButtonMethod(MethodInfo methodInfo)
        {
            this.methodInfo = methodInfo;
        }

        protected sealed override void Draw(Object target)
        {
            if (GUILayout.Button(niceName))
            {
                object result = methodInfo.Invoke(target, null);

                if (result != null)
                {
                    string message = $"{result} \nResult of Method '{methodInfo.Name}' invocation on object {target.name}";
                    Debug.Log(message, target);
                }
            }
        }
    }
    class DrawableDummyMember : DrawableMember
    {
        protected sealed override void Draw(Object target)
        {
            Rect r = EditorGUILayout.GetControlRect();
            GUI.Label(r, niceName);

            GUI.color = EditorHelper.ErrorRedColor;
            GUI.enabled = false;
            r.xMin += EditorGUIUtility.labelWidth;
            GUI.Label(r, "Incompatible with EasyDraw attribute!");


            GUI.enabled = true;
            GUI.color = Color.white;
        }
    }

    class DrawableProperty : DrawableMember
    {
        readonly PropertyInfo pInfo;
        readonly FieldInfo fInfo;

        readonly SerializedProperty serializedProperty;
        readonly SerializedObject serializedObject;

        public DrawableProperty(PropertyInfo pInfo, SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            serializedProperty = serializedObject.FindProperty(pInfo.Name);
            this.pInfo = pInfo;
        }
        public DrawableProperty(FieldInfo fInfo, SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            serializedProperty = serializedObject.FindProperty(fInfo.Name);
            this.fInfo = fInfo;
        }

        protected sealed override void Draw(Object target)
        {
            if (serializedProperty != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedProperty, true);

                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Type dateType = fInfo != null ? fInfo.FieldType : pInfo.PropertyType;
                object original = fInfo != null ? fInfo.GetValue(target) : pInfo.GetValue(target, null);


                bool isExp = true; // TODO

                GUI.enabled = fInfo != null || pInfo.SetMethod != null;

                float height = EditorHelper.GetAnythingHeight(dateType, isExp);
                Rect position = EditorGUILayout.GetControlRect(GUILayout.Height(height));

                object result = EditorHelper.AnythingField(position, dateType, original, niceName, ref isExp);
                if (!Equals(original, result))
                {
                    if(fInfo!= null)
                        fInfo.SetValue(target, result);
                    else
                        pInfo.SetValue(target, result, null);

                    serializedObject.ApplyModifiedProperties();
                }
                GUI.enabled = true;
            }
        }
    }
}
#endif