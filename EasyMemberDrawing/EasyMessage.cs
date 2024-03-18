using System;

namespace EasyEditor
{
	public enum MessageType
	{
		Info,
		Warning,
		Error,
		None,
	}

	public enum MessageFormat
	{
		FullLength,
		FullLengthUnboxed,

		ContentLength,
		ContentLengthUnboxed,

		ContentLengthWithTitle,
		ContentLengthWithTitleUnboxed
	}

	public enum MessageSize
	{
		Normal,
		Small,
		Big,
		Huge
	}

	[Serializable]
	public class EasyMessage
	{
		[NonSerialized] public MessageType messageType;
		[NonSerialized] public MessageFormat messageFormat;
		[NonSerialized] public MessageSize messageSize;
		 
		readonly string _text;
		public string TextValue => _text;
		
		public EasyMessage(string text)
		{
			_text = text;
		}

		public int FontSize => messageSize switch
		{
			MessageSize.Small => 10,
			MessageSize.Normal => 12,
			MessageSize.Big => 16,
			MessageSize.Huge => 22,
			_ => 12,
		};

		public bool IsBoxed => messageFormat switch
		{
			MessageFormat.FullLengthUnboxed or
			MessageFormat.ContentLengthUnboxed or
			MessageFormat.ContentLengthWithTitleUnboxed => false,
			_ => true,
		};

		public bool IsFullLength => messageFormat switch
		{
			MessageFormat.FullLength or
			MessageFormat.FullLengthUnboxed => true,
			_ => false,
		};

		public bool ShowTitle => messageFormat switch
		{
			MessageFormat.ContentLengthWithTitle or
			MessageFormat.ContentLengthWithTitleUnboxed => true,
			_ => false,
		};

	}
}