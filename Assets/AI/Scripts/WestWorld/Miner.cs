using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WestWorld
{
    public class Miner : BaseGameEntity
    {
        private readonly int ComfortLevel = 5;
        private readonly int MaxMuggets = 3;
        private readonly int ThirstLevel = 5;
        private readonly int TirednessThreshold = 5;

        public StateMachine<Miner> StateMachine { get; private set; }
        // 位置
        public LocationType Location { get; set; }
        // 包中的金块
        public int GoldCarried { get; set; }
        // 疲劳值 价值越高，越累
        public int Fatigue { get; private set; }
        // 价值越高，越口渴
        public int Thirsty { get; private set; }
        // 银行存了多少钱
        public int MoneyInBank { get; private set; }

        public Miner(int id) : base(id)
        {
            GoldCarried = 0;
            Fatigue = 0;
            Thirsty = 0;
            MoneyInBank = 0;
            Location = LocationType.Shack;
            StateMachine = new StateMachine<Miner>(this);
            StateMachine.SetState(GoHomeAndSleepTilRested.Instance);
        }

        public override void Update()
        {
            Thirsty += 1;

            StateMachine.Update();
        }

        public override bool HandleMessage(Telegram telegram)
        {
            return StateMachine.HandleMessage(telegram);
        }

        public void AddToGoldCarried(int gold)
        {
            GoldCarried += gold;
        }

        public void DecreaseFatigue() { Fatigue -= 1; }
        public void IncreaseFatigue() { Fatigue += 1; }

        public bool PocketsFull()
        {
            return GoldCarried >= MaxMuggets;
        }

        public bool IsThirsty()
        {
            return Thirsty >= ThirstLevel;
        }

        public void AddToWealth(int val)
        {
            MoneyInBank += val;
            if (MoneyInBank < 0) MoneyInBank = 0;
        }

        public bool IsComfort()
        {
            return MoneyInBank >= ComfortLevel;
        }

        public bool IsTired()
        {
            return Fatigue >= TirednessThreshold;
        }

        public void BuyAndDrinkWhiskey()
        {
            Thirsty = 0;
            MoneyInBank -= 2;
        }
    }
}
