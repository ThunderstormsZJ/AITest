using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WestWorld
{
    //==========================Miner State======================
    public class EnterMineAndDigForNugget : State<Miner>
    {
        public static EnterMineAndDigForNugget Instance { get; } = new EnterMineAndDigForNugget();

        public override void Enter(Miner miner)
        {
            if (miner.Location != LocationType.GoldMine)
            {
                AILog.Bule("[{0}]: Enter Glod Mine", miner);
                miner.Location = LocationType.GoldMine;
            }
        }

        public override void Execute(Miner miner)
        {
            miner.AddToGoldCarried(1);
            miner.IncreaseFatigue();

            if (miner.PocketsFull())
            {
                miner.StateMachine.ChangeState(VisitBankAndDepositGold.Instance);
            }

            if (miner.IsThirsty())
            {
                miner.StateMachine.ChangeState(QuenchThirst.Instance);
            }
        }

        public override void Exit(Miner miner)
        {
            AILog.Bule("[{0}]: Leave Gold Mine", miner);
        }
    }

    public class VisitBankAndDepositGold : State<Miner>
    {
        public static VisitBankAndDepositGold Instance { get; } = new VisitBankAndDepositGold();

        public override void Enter(Miner miner)
        {
            if (miner.Location != LocationType.Bank)
            {
                AILog.Bule("[{0}]: Enter Bank", miner);
                miner.Location = LocationType.Bank;
            }
        }

        public override void Execute(Miner miner)
        {
            miner.AddToWealth(miner.GoldCarried);
            miner.GoldCarried = 0;

            AILog.Bule("[{0}]: Save Money, Remain Money {1}", miner, miner.MoneyInBank);

            if (miner.IsComfort())
            {
                miner.StateMachine.ChangeState(GoHomeAndSleepTilRested.Instance);
            }
            else
            {
                miner.StateMachine.ChangeState(EnterMineAndDigForNugget.Instance);
            }
        }

        public override void Exit(Miner miner)
        {
            AILog.Bule("[{0}]: Leave Bank", miner);
        }
    }

    public class GoHomeAndSleepTilRested : State<Miner>
    {
        public static GoHomeAndSleepTilRested Instance { get; } = new GoHomeAndSleepTilRested();

        public override void Enter(Miner miner)
        {
            if (miner.Location != LocationType.Shack)
            {
                AILog.Bule("[{0}]: Enter Home", miner);

                miner.Location = LocationType.Shack;

                // Tell wife I'am home.
                MessageDispatch.Instance.DispatchMessage(0, miner.ID, (int)EntityName.Rose, MessageType.Msg_InHome);
            }
        }

        public override void Execute(Miner miner)
        {
            if (!miner.IsTired())
            {
                miner.StateMachine.ChangeState(EnterMineAndDigForNugget.Instance);
            }
            else
            {
                // Sleep
                miner.DecreaseFatigue();

                AILog.Bule("[{0}]: ZZZZ....", miner);
            }
        }

        public override void Exit(Miner miner)
        {
        }

        public override bool OnMessage(Miner entity, Telegram telegram)
        {
            switch (telegram.Msg)
            {
                case (int)MessageType.Msg_StewReady:
                    entity.StateMachine.ChangeState(EatStew.Instance);
                    break;
                default:
                    break;
            }
            return false;
        }
    }

    public class QuenchThirst : State<Miner>
    {
        public static QuenchThirst Instance { get; } = new QuenchThirst();

        public override void Enter(Miner miner)
        {
            if (miner.Location != LocationType.Saloon)
            {
                miner.Location = LocationType.Saloon;

                AILog.Bule("[{0}]: Enter Saloon", miner);
            }
        }

        public override void Execute(Miner miner)
        {
            if (miner.IsThirsty())
            {
                AILog.Bule("[{0}]: Buy Whiskey And Feel Good", miner);

                miner.BuyAndDrinkWhiskey();

                miner.StateMachine.ChangeState(EnterMineAndDigForNugget.Instance);
            }
            else
            {
                
            }
        }

        public override void Exit(Miner miner)
        {
            AILog.Bule("[{0}]: Leave Saloon", miner);
        }
    }

    public class EatStew : State<Miner>
    {
        public static EatStew Instance { get; } = new EatStew();

        public override void Enter(Miner entity)
        {
            AILog.Bule("[{0}]: Start Eat Stew", entity);
        }

        public override void Execute(Miner entity)
        {
            entity.StateMachine.RevertToPreviousState();
        }

        public override void Exit(Miner entity)
        {
        }
    }
    //==========================Miner State End======================


    //==========================MinerWife State======================
    public class MinerWifeGlobaState : State<MinerWife>
    {
        public static MinerWifeGlobaState Instance { get; } = new MinerWifeGlobaState();
        public override void Enter(MinerWife entity)
        {
            
        }

        public override void Execute(MinerWife entity)
        {
            // 1 in 10 change of needing the bathroom
            if (Random.Range(0f, 1.0f) < 0.1f && !entity.StateMachine.IsInState(VisitBathroom.Instance))
            {
                entity.StateMachine.ChangeState(VisitBathroom.Instance);
            }
        }

        public override void Exit(MinerWife entity)
        {
        }

        public override bool OnMessage(MinerWife wife, Telegram telegram)
        {
            switch (telegram.Msg)
            {
                case (int)MessageType.Msg_InHome:
                    wife.StateMachine.ChangeState(CookStew.Instance);
                    break;
            }

            return false;
        }
    }

    public class DoHouseWork : State<MinerWife>
    {
        public static DoHouseWork Instance { get; } = new DoHouseWork();

        public override void Enter(MinerWife entity)
        {
            AILog.Green("[{0}]: Start Do House Work.", entity);
        }

        public override void Execute(MinerWife entity)
        {
            switch (Random.Range(0, 3))
            {
                case 0:
                    AILog.Green("[{0}]: Mop The Floor.", entity);
                    break;
                case 1:
                    AILog.Green("[{0}]: Wash The Dish.", entity);
                    break;
                case 2:
                    AILog.Green("[{0}]: Make The Bed.", entity);
                    break;
                default:
                    break;
            }
        }

        public override void Exit(MinerWife entity)
        {

        }
    }

    public class VisitBathroom : State<MinerWife>
    {
        public static VisitBathroom Instance { get; } = new VisitBathroom();

        public override void Enter(MinerWife entity)
        {
            AILog.Green("[{0}]: GO To Bathroom.", entity);
        }

        public override void Execute(MinerWife entity)
        {
            entity.StateMachine.RevertToPreviousState();
        }

        public override void Exit(MinerWife entity)
        {

        }
    }

    public class CookStew : State<MinerWife>
    {
        public static CookStew Instance { get; } = new CookStew();

        public override void Enter(MinerWife entity)
        {
            if (!entity.IsCooking)
            {
                AILog.Green("[{0}]: Start Cook", entity);

                entity.IsCooking = true;
                MessageDispatch.Instance.DispatchMessage(2, entity.ID, entity.ID, MessageType.Msg_StewReady);
            }
        }

        public override void Execute(MinerWife entity)
        {
            AILog.Green("[{0}]: Cooking", entity);
        }

        public override void Exit(MinerWife entity)
        {
        }

        public override bool OnMessage(MinerWife entity, Telegram telegram)
        {
            switch (telegram.Msg)
            {
                case (int)MessageType.Msg_StewReady:
                    // 通知Miner吃饭
                    MessageDispatch.Instance.DispatchMessage(0, entity.ID, (int)EntityName.Miner_Jack, MessageType.Msg_StewReady);

                    entity.IsCooking = false;
                    entity.StateMachine.ChangeState(DoHouseWork.Instance);
                    break;
            }
            return false;
        }
    }

    //==========================MinerWife State End======================
}

