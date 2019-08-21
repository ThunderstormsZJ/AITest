using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WestWorld
{
    public class MinerWife: BaseGameEntity
    {
        public StateMachine<MinerWife> StateMachine { get; private set; }
        public bool IsCooking { get; set; }

        public MinerWife(int id) : base(id)
        {
            StateMachine = new StateMachine<MinerWife>(this);

            StateMachine.SetState(DoHouseWork.Instance);
            StateMachine.SetGlobaState(MinerWifeGlobaState.Instance);
        }

        public override bool HandleMessage(Telegram telegram)
        {
            return StateMachine.HandleMessage(telegram);
        }

        public override void Update()
        {
            StateMachine.Update();
        }
    }
}
