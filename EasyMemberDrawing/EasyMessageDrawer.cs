#if UNITY_EDITOR

using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EasyEditor
{
	[CustomPropertyDrawer(typeof(EasyMessage), useForChildren: true)]
	public class EasyMessageDrawer : PropertyDrawer
	{
		EasyMessage _message;
		Func<object, object> _textGetter;
		string[] _lines;
		bool _isInitialized = false;
		object _owner;


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			_owner = property.GetObjectWithProperty();
			_message = (EasyMessage)property.GetObjectOfProperty();
			if (!_isInitialized)
			{
				InspectorDrawingUtility.TryGetAGetterFromMember(_owner.GetType(), _message.TextValue, out _textGetter);
				_isInitialized = true;
			}

			_lines = GetLines(_owner, _message.TextValue);
			int messageLineCount = _lines.Length;

			if (messageLineCount == 0)
				return 0;
			int fontSize = _message.FontSize;
			bool boxed = _message.IsBoxed;
			int spacingSize = Mathf.RoundToInt(0.2f * fontSize);
			const float minimumBoxedHeight = 22;

			float h = messageLineCount * (fontSize + spacingSize) + (boxed ? 7 : 0);

			if (boxed)
				return Mathf.Max(h, minimumBoxedHeight);
			return h;

		}

		public string[] GetLines(object owner, string text)
		{
			string t = _textGetter != null ? _textGetter.Invoke(owner).ToString() : text;

			if (t == null || t.Length == 0)
				return Array.Empty<string>();

			return t.Split('\n');
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_lines.Length == 0)
				return;
			StringBuilder messageBuilder = new();

			int i = 0;
			foreach (string line in _lines)
			{
				if (i != 0)
					messageBuilder.Append("\n");
				messageBuilder.Append(line);
				i++;
			}

			bool showTitle = _message.ShowTitle;
			bool isFullLength = _message.IsFullLength;
			bool isBoxed = _message.IsBoxed;
			int fontSize = _message.FontSize;
			MessageType messageType = _message.messageType;
			string message = messageBuilder.ToString();

			if (showTitle)
				DrawMessage(position, label, message, messageType, fontSize, isBoxed);
			else
				DrawMessage(position, message, messageType, fontSize, isBoxed, isFullLength);
		}

		public static void DrawMessage(
			Rect position,
			GUIContent label,
			string message,
			MessageType messageType = MessageType.Info,
			int fontSize = 10,
			bool isBoxed = true)
		{
			DrawMessage(position, message, messageType, fontSize, isBoxed, false);

			Rect labelPosition = EditorHelper.LabelRect(position);
			GUI.Label(labelPosition, label);
		}

		public static void DrawMessage(
			Rect position,
			string message,
			MessageType messageType = MessageType.Info,
			int fontSize = 10,
			bool isBoxed = true,
			bool isFullLength = true)
		{
			IconType editorMessageType = ToEditorMessageType(messageType);
			IconSize size = position.height < 40 ? IconSize.Small : IconSize.Big;
			Texture icon = EditorHelper.GetIcon(editorMessageType, size);
			GUIContent content = new(icon)
			{
				text = message
			};

			GUIStyle style = isBoxed
				? new GUIStyle(EditorStyles.helpBox) { fontSize = fontSize }
				: new GUIStyle(EditorStyles.label) { fontSize = fontSize };

			Rect contentPosition = isFullLength ? position : EditorHelper.ContentRect(position);
			GUI.Label(contentPosition, content, style);
		}
		static IconType ToEditorMessageType(MessageType messageType) => messageType switch
		{
			MessageType.Info => IconType.Info,
			MessageType.Warning => IconType.Warning,
			MessageType.Error => IconType.Error,
			_ => IconType.None
		};
	}
}
#endif