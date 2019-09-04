using System;
using UnityEngine;

namespace WestWorld
{
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
