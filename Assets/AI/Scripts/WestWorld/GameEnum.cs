using System;
using UnityEngine;

namespace WestWorld
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class EnumDescription : Attribute
    {
        public string Description { get; private set;}
        public EnumDescription(string str) : base()
        {
            Description = str;
        }
    }

    public static class EnumHelper
    {
        public static string GetDescription(Enum value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Value");
            }
            string description = value.ToString();
            System.Reflection.FieldInfo fieldInfo =  value.GetType().GetField(description);
            EnumDescription[] attributes = (EnumDescription[])fieldInfo.GetCustomAttributes(typeof(EnumDescription), false);
            if (attributes != null && attributes.Length > 0)
            {
                description = attributes[0].Description;
            }
            return description;
        }
    }

    public enum EntityName
    {
        [EnumDescription("Jack")]
        Miner_Jack,
        [EnumDescription("Rose")]
        Rose
    }

    public enum LocationType { Shack, GoldMine, Bank, Saloon }

    public enum MessageType { Msg_InHome, Msg_StewReady }
}
