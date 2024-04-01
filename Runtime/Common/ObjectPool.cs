/*
 *  不会自动增加容量的对象池
 */

using System;

namespace PluginLit.Core
{
    public class ObjectPool<T>
    {
        protected int _maxSize;
        protected int _index;
        protected T[] _pool;
        
        public ObjectPool(int maxSize)
        {
            _index = 0;
            _maxSize = maxSize;
            _pool = new T[maxSize];
        }


        public T Get()
        {
            T obj;
            if (_index > 0)
            {
                obj = GetOne();
            }
            else
            {
                obj = CreateOne();
                OnCreate(obj);
            }
            
            OnPreGet(obj);
            return obj;
        }

        public void Put(T obj)
        {
            if (_index >= _maxSize)
            {
                OnPreDrop(obj);
            }
            else
            {
                OnPrePut(obj);
                PutOne(obj);
            }
        }

        public void Prepare(int amount)
        {
            while (_index < amount && _index < _maxSize)
            {
                var obj = CreateOne();
                OnCreate(obj);
                Put(obj);
            }
        }

        public void Clear()
        {
            while (_index > 0)
            {
                OnPreDrop(GetOne());
            }
        }

        protected T GetOne()
        {
            _index--;
            var obj = _pool[_index];
            _pool[_index] = default(T);
            return obj;
        }

        protected void PutOne(T obj)
        {
            _pool[_index++] = obj;
        }

        protected virtual T CreateOne()
        {
            return Activator.CreateInstance<T>();
        }

        protected virtual void OnCreate(T obj)
        {
            
        }

        protected virtual void OnPreGet(T obj)
        {
            
        }

        protected virtual void OnPrePut(T obj)
        {
            
        }

        protected virtual void OnPreDrop(T obj)
        {
            
        }
    }
}