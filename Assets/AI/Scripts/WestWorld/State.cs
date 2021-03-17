using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WestWorld
{
    public abstract class State<T>
    {
        public abstract void Execute(T entity);
        public abstract void Enter(T entity);
        public abstract void Exit(T entity);

        public virtual bool OnMessage(T entity, Telegram telegram)
        {
            return false;
        }
    }
}
