using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
#if UNITY_EDITOR
                // In Editor mode, create a simple instance without GameObject
                if (!Application.isPlaying)
                {
                    instance = new UnityMainThreadDispatcher();
                    instance.InitializeEditorMode();
                    UnityEngine.Debug.Log("[UnityMainThreadDispatcher] Initialized in Editor mode");
                    return instance;
                }
#endif
                var go = new GameObject("MainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
                UnityEngine.Debug.Log("[UnityMainThreadDispatcher] Initialized in Play mode");
            }
            return instance;
        }

        /// <summary>
        /// Manually dispose the dispatcher (especially useful in Edit mode)
        /// </summary>
        public static void DisposeInstance()
        {
            if (instance != null)
            {
#if UNITY_EDITOR
                if (instance.isEditorMode)
                {
                    EditorApplication.update -= instance.EditorUpdate;
                    UnityEngine.Debug.Log("[UnityMainThreadDispatcher] Disposed Editor mode instance");
                }
                else
#endif
                {
                    if (instance.gameObject != null)
                    {
                        if (Application.isPlaying)
                            Destroy(instance.gameObject);
                        else
                            DestroyImmediate(instance.gameObject);
                    }
                }
                instance = null;
            }
        }

#if UNITY_EDITOR
        private bool isEditorMode = false;

        private void InitializeEditorMode()
        {
            isEditorMode = true;
            EditorApplication.update += EditorUpdate;
        }

        private void EditorUpdate()
        {
            ProcessQueue();
        }

        ~UnityMainThreadDispatcher()
        {
            if (isEditorMode)
            {
                EditorApplication.update -= EditorUpdate;
            }
        }
#endif

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
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            lock (executionQueue)
            {
                while (executionQueue.Count > 0)
                {
                    try
                    {
                        executionQueue.Dequeue().Invoke();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[UnityMainThreadDispatcher] Error executing queued action: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Encola una acción para ejecutarse en el Main Thread de Unity
        /// </summary>
        public void Enqueue(Action action)
        {
            if (action == null)
            {
                UnityEngine.Debug.LogWarning("[UnityMainThreadDispatcher] Attempted to enqueue null action");
                return;
            }

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
            if (coroutine == null)
            {
                UnityEngine.Debug.LogWarning("[UnityMainThreadDispatcher] Attempted to enqueue null coroutine");
                return;
            }

            lock (executionQueue)
            {
                executionQueue.Enqueue(() => StartCoroutine(coroutine));
            }
        }

        /// <summary>
        /// Gets the current queue size (useful for debugging)
        /// </summary>
        public int GetQueueSize()
        {
            lock (executionQueue)
            {
                return executionQueue.Count;
            }
        }

        /// <summary>
        /// Clears all pending actions in the queue
        /// </summary>
        public void ClearQueue()
        {
            lock (executionQueue)
            {
                executionQueue.Clear();
                UnityEngine.Debug.Log("[UnityMainThreadDispatcher] Queue cleared");
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (isEditorMode)
            {
                EditorApplication.update -= EditorUpdate;
            }
#endif
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnDisable()
        {
            // Process any remaining queued actions before disabling
            ProcessQueue();
        }
    }
}
