using System.Collections.Generic;
using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Пул объектов (Object Pooling): объекты не уничтожаются, а выключаются и переиспользуются.
    /// Так при 150+ врагах нет лишних Instantiate/Destroy и сборки мусора.
    /// </summary>
    public class ComponentPool<T> where T : Component
    {
        readonly T prefab;
        readonly Transform parent;
        readonly Stack<T> free = new Stack<T>();

        public ComponentPool(T prefab, Transform parent, int prewarm)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                var item = Create();
                item.gameObject.SetActive(false);
                free.Push(item);
            }
        }

        T Create()
        {
            var item = Object.Instantiate(prefab, parent);
            item.name = prefab.name;
            return item;
        }

        /// <summary>Достаёт объект из пула (или создаёт новый) и ставит в нужную точку.</summary>
        public T Get(Vector3 position)
        {
            T item = free.Count > 0 ? free.Pop() : Create();
            item.transform.position = position;
            item.gameObject.SetActive(true);
            return item;
        }

        /// <summary>Возвращает объект в пул.</summary>
        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            free.Push(item);
        }
    }
}
