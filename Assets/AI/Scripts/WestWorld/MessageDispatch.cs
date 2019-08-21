using System;
using System.Collections.Generic;
using UnityEngine;

namespace WestWorld
{
    public struct Telegram: IComparable
    {
        public double DispatchTime; // 延迟发送的时间
        public int Sender; // 发送者
        public int Receiver; // 接收者
        public int Msg; // 消息类型
        object ExtraInfo; // 额外消息

        public Telegram(int sender, int receiver, int msg, object extraInfo=null)
        {
            DispatchTime = 0;
            Sender = sender;
            Receiver = receiver;
            Msg = msg;
            ExtraInfo = extraInfo;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Telegram))
            {
                return false;
            }

            var telegram = (Telegram)obj;
            return Sender == telegram.Sender &&
                   Receiver == telegram.Receiver &&
                   Msg == telegram.Msg &&
                  (Math.Abs(DispatchTime - telegram.DispatchTime) < 0.25);
        }

        public override string ToString()
        {
            return String.Format("Time[{0}],Sender[{1}],Receiver[{2}],Msg[{3}]", DispatchTime, Sender, Receiver, Msg);
        }

        public override int GetHashCode()
        {
            var hashCode = -146911448;
            hashCode = hashCode * -1521134295 + Sender.GetHashCode();
            hashCode = hashCode * -1521134295 + Receiver.GetHashCode();
            hashCode = hashCode * -1521134295 + Msg.GetHashCode();
            hashCode = hashCode * -1521134295 + DispatchTime.GetHashCode();
            return hashCode;
        }

        public int CompareTo(object obj)
        {
            Telegram telegram = (Telegram)obj;
            if (this > telegram) {
                return 1;
            }
            else if(this == telegram)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        public static bool operator ==(Telegram t1, Telegram t2) => t1.Equals(t2);
        public static bool operator !=(Telegram t1, Telegram t2) => !t1.Equals(t2);
        public static bool operator >(Telegram t1, Telegram t2) => t1.DispatchTime > t2.DispatchTime;
        public static bool operator <(Telegram t1, Telegram t2) => t1.DispatchTime < t2.DispatchTime;
    }


    public class MessageDispatch
    {
        public static MessageDispatch Instance { get; } = new MessageDispatch();

        // Declaration a queue to dispatch message by time.
        private SortedSet<Telegram> telegrams = new SortedSet<Telegram>();

        private void Discharge(Telegram telegram)
        {
            // Get Receiver
            BaseGameEntity receiverEntity = EntityManager.Instance.GetEntityByID(telegram.Receiver);
            if (receiverEntity == null || !receiverEntity.HandleMessage(telegram))
            {
                AILog.Red("No Handle Or Entity Is Not Find[{0}].", telegram.Receiver);
            }
        }

        private void DischargeDelay()
        {
            // CheckOut The Queue.
            double currentTimeStamp = AIUtils.GetTimeStamp();

            while ((telegrams.Count != 0) && (telegrams.Min.DispatchTime < currentTimeStamp) && (telegrams.Min.DispatchTime > 0))
            {
                Discharge(telegrams.Min);
                telegrams.Remove(telegrams.Min);
            }
        }

        public void Start()
        {
            DischargeDelay();
        }

        public void DispatchMessage(double delay, int sender, int receiver, MessageType msg, object extraInfo = null)
        {
            Telegram telegram = new Telegram(sender, receiver, (int)msg, extraInfo);
            if (delay <= 0)
            {
                // 直接发送
                Discharge(telegram);
            }
            else
            {
                double dispatchTime = AIUtils.GetTimeStamp();
                telegram.DispatchTime = dispatchTime + delay;
                // 加入队列
                telegrams.Add(telegram);
            }
        }
    }
}
