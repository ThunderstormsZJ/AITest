using System;

namespace WestWorld 
{

    public abstract class BaseGameEntity
    {
        public int ID { get; private set; }

        private static int m_iNextValidID = 0;

        public BaseGameEntity(int id)
        {
            UnityEngine.Assertions.Assert.IsTrue(id >= m_iNextValidID, "<BaseGameEntity:SetID>: invalid ID");
            ID = id;

            m_iNextValidID += 1;
        }
        public abstract void Update();

        public virtual bool HandleMessage(Telegram telegram)
        {
            return false;
        }

        public override string ToString()
        {
            return EnumHelper.GetDescription((EntityName)ID);
        }
    }
}
