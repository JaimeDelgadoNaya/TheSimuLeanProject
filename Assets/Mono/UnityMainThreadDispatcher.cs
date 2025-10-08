using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChapasGA.Mono
{
    /// <summary>
    /// Dispatcher para ejecutar acciones en el Main Thread de Unity desde background threads
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher instance;
        private readonly Queue<Action> executionQueue = new Queue<Action>();

        public static UnityMainThreadDispatcher Instance()
        {
            if (instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Update()
        {
            lock (executionQueue)
            {
                while (executionQueue.Count > 0)
                {
                    executionQueue.Dequeue().Invoke();
                }
            }
        }

        /// <summary>
        /// Encola una acción para ejecutarse en el Main Thread de Unity
        /// </summary>
        public void Enqueue(Action action)
        {
            lock (executionQueue)
            {
                executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Encola una coroutine para ejecutarse en el Main Thread de Unity
        /// </summary>
        public void EnqueueCoroutine(IEnumerator coroutine)
        {
            lock (executionQueue)
            {
                executionQueue.Enqueue(() => StartCoroutine(coroutine));
            }
        }
    }
}
