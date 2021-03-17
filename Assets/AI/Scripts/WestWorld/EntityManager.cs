using System;
using System.Collections;
using System.Collections.Generic;

namespace WestWorld
{
    public class EntityManager
    {
        public static EntityManager Instance { get; } = new EntityManager();

        private Dictionary<int, BaseGameEntity> entityMap = new Dictionary<int, BaseGameEntity>();

        public BaseGameEntity GetEntityByID(int id)
        {
            BaseGameEntity entity;
            entityMap.TryGetValue(id, out entity);
            return entity;
        }

        public void RegisterEntity(BaseGameEntity entity)
        {
            entityMap.Add(entity.ID, entity);
        }

        public void RemoveEntity(BaseGameEntity entity)
        {
            entityMap.Remove(entity.ID);
        }
    }
}
