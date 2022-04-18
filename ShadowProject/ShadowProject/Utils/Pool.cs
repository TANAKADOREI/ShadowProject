using System;
using System.Collections.Generic;
using System.Text;

namespace ShadowProject.Utils
{
    public class Pool<T>
    {
        public class Item : IDisposable
        {
            private int ID;
            private T OBJ;
            Pool<T> m_owner;

            public Item(Pool<T> owner)
            {
                this.m_owner = owner;
                m_owner.Borrow(out ID, out OBJ);
            }

            ~Item()
            {
                Dispose();
            }

            public T Get => OBJ;

            public void Dispose()
            {
                m_owner.Return(ref ID, ref OBJ);
            }
        }

        Func<T> CreateNewObjEvent;
        T[] m_pool;
        Stack<int> m_available_index;

        public Pool(Func<T> CreateNewObjEvent,int capacity)
        {
            this.CreateNewObjEvent = CreateNewObjEvent;
            m_pool = new T[capacity];
            m_available_index = new Stack<int>();

            for(int i = 0; i<capacity; i++)
            {
                m_pool[i] = CreateNewObjEvent();
                m_available_index.Push(i);
            }
        }

        private void Borrow(out int ID,out T OBJ)
        {
            if (m_available_index.Count > 0)
            {
                ID = m_available_index.Pop();
                OBJ = m_pool[ID];
            }
            else
            {
                ID = -1;
                OBJ = CreateNewObjEvent();
            }
        }

        private void Return(ref int ID,ref T OBJ)
        {
            if (ID >= 0)
            {
                m_available_index.Push(ID);
            }
            ID = -1;
            OBJ = default;
        }

        public Item Get()
        {
            return new Item(this);
        }
    }
}
