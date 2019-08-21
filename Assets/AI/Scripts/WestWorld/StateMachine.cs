

using UnityEngine;

namespace WestWorld
{
    public class StateMachine<T>
    {
        // 状态
        public State<T> CurrentState { get; private set; }
        public State<T> PreviousState { get; private set; }

        // Globa State represent can change a special state in every state.
        public State<T> GlobaState { get; private set; }
        public T Entity { get; private set; }

        public StateMachine(T entity)
        {
            Entity = entity;
        }

        public void SetState(State<T> newState)
        {
            CurrentState = newState;
        }

        public void SetGlobaState(State<T> newState)
        {
            GlobaState = newState;
        }

        public void RevertToPreviousState()
        {
            ChangeState(PreviousState);
        }

        public void ChangeState(State<T> newState)
        {
            if (CurrentState == null && newState == null)
            {
                Debug.LogError("State Error");
                return;
            }

            PreviousState = CurrentState;

            CurrentState.Exit(Entity);

            CurrentState = newState;

            CurrentState.Enter(Entity);
        }

        public bool IsInState(State<T> state)
        {
            return CurrentState == state;
        }

        public bool HandleMessage(Telegram telegram)
        {
            if (CurrentState!=null && CurrentState.OnMessage(Entity, telegram))
            {
                return true;
            }

            if (GlobaState!=null && GlobaState.OnMessage(Entity, telegram))
            {
                return true;
            }

            return false;
        }

        public void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.Execute(Entity);
            }

            if (GlobaState != null)
            {
                GlobaState.Execute(Entity);
            }
        }
    }
}