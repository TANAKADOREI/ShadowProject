using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowProject.Utils
{
    class TaskPool : IDisposable
    {
        private Queue<Action> m_task_queue;
        private Thread m_thread;
        private bool m_shutdown_req;
        private readonly object m_lock_key = new object();

        public TaskPool(uint thread_count)
        {
            m_shutdown_req = false;
            m_task_queue = new Queue<Action>();
            m_thread = new Thread(Update);
            m_thread.Start();
        }

        ~TaskPool()
        {
            Dispose();
        }

        public void WaitForRemainTask()
        {
            lock (m_lock_key)
            {
                foreach (var a in m_task_queue)
                {
                    a?.Invoke();
                }

                m_task_queue.Clear();
            }
        }

        private void Update()
        {
            Action action = null;

            while (true)
            {
                bool run = false;

                lock (m_lock_key)
                {
                    run = !m_shutdown_req;

                    if (m_task_queue.Count != 0)
                    {
                        action = m_task_queue.Dequeue();
                    }
                }

                action?.Invoke();

                if (!run) break;
            }

            WaitForRemainTask();
        }

        public void Add(Action action)
        {
            if (action == null) return;

            lock (m_lock_key)
            {
                m_task_queue.Enqueue(action);
            }
        }

        public void Dispose()
        {
            if (m_thread != null)
            {
                lock (m_lock_key)
                {
                    m_shutdown_req = true;
                }
                m_thread.Join();
                m_thread = null;
            }
        }
    }
}
