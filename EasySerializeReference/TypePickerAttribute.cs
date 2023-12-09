using UnityEngine;

public class TypePickerAttribute : PropertyAttribute
{
	public enum TypeToStringConversion
	{
		ShortName,
		FullName
	}

	public readonly TypeToStringConversion typeToStringConversion = TypeToStringConversion.ShortName;
	public readonly bool forceSmall = false;
	public readonly string filterMethod = null;

	public TypePickerAttribute(
		string filterMethod,
		TypeToStringConversion typeToStringConversion = TypeToStringConversion.ShortName,
		bool forceSmall = false)
	{
		this.filterMethod = filterMethod;
		this.forceSmall = forceSmall;
		this.typeToStringConversion = typeToStringConversion;
	}

	public TypePickerAttribute(
		string filterMethod,
		bool forceSmall)
	{
		this.filterMethod = filterMethod;
		this.forceSmall = forceSmall;
	}

	public TypePickerAttribute(
		TypeToStringConversion typeToStringConversion = TypeToStringConversion.ShortName,
		bool forceSmall = false)
	{
		this.forceSmall = forceSmall;
		this.typeToStringConversion = typeToStringConversion;
	}

	public TypePickerAttribute(bool forceSmall)
	{
		this.forceSmall = forceSmall;
	}
	public TypePickerAttribute()
	{
	}
}