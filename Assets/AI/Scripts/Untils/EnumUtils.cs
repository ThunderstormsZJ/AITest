using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class EnumDescription : Attribute
{
    public string Description { get; private set; }
    public EnumDescription(string str) : base()
    {
        Description = str;
    }
}

public class EnumHelper
{
    public static string GetDescription(Enum value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("Value");
        }
        string description = value.ToString();
        System.Reflection.FieldInfo fieldInfo = value.GetType().GetField(description);
        EnumDescription[] attributes = (EnumDescription[])fieldInfo.GetCustomAttributes(typeof(EnumDescription), false);
        if (attributes != null && attributes.Length > 0)
        {
            description = attributes[0].Description;
        }
        return description;
    }
}
