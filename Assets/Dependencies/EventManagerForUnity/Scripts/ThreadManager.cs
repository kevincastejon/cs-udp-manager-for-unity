using kevincastejon;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace kevincastejon
{
    /// <summary>
    /// Unity reserved class that handles multi threaded events
    /// <c>Only initialize this class if you're using threaded events in Unity environment!</c>
    /// </summary>
    public class ThreadManager : MonoBehaviour
    {

        private static ThreadManager _instance;

        private List<EventDispatcher> eds = new List<EventDispatcher>();
        private List<object> ts = new List<object>();
        private List<kevincastejon.Event> es = new List<kevincastejon.Event>();
        /// <summary>
        /// Call this method once if you need threaded events into Unity
        /// </summary>
        public static void Init()
        {
            if (Instance == null)
                new GameObject("threadManager", typeof(ThreadManager)).GetComponent<ThreadManager>();
            else throw new Exception("ThreadManager already initialized");
        }
        // Use this for initialization
        void Start()
        {
            if (Instance == null)
                _instance = this;
            else throw new Exception("ThreadManager already initialized");
        }

        // Update is called once per frame
        void Update()
        {
            while (eds.Count > 0)
            {
                eds[0].DoDispatch(es[0], ts[0]);
                eds.RemoveAt(0); es.RemoveAt(0); ts.RemoveAt(0);
            }
        }
        internal static ThreadManager Instance { get { return (_instance); } }

        internal static void DispatchEventFromMainThread(EventDispatcher ed, kevincastejon.Event e, object target = null)
        {
            if (Instance)
            {
                Instance.eds.Add(ed); Instance.es.Add(e); Instance.ts.Add(target);
            }
        }
    }
}
