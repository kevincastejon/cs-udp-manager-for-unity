using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using _EventDispatcher = kevincastejon.EventDispatcher;

namespace kevincastejon.unity
{
    [Serializable]
    public struct DicoListeners
    {
        public string eventName;
        public UEvent callback;
    }
    [Serializable]
    public class UEvent : UnityEvent<Event> { }
    public class EventDispatcher : MonoBehaviour
    {
        public List<DicoListeners> Listeners;
        _EventDispatcher Ed;
        // Start is called before the first frame update
        void Start()
        {
            Ed = new _EventDispatcher();
            Listeners.ForEach((DicoListeners dl) =>
            {
                On<Event>(dl.eventName, (Event e) =>
                {
                    dl.callback?.Invoke(e);
                });
            });
        }

        // Update is called once per frame
        void Update()
        {

        }
        /// <summary>
        /// Adds an event listener on the instance that will call the <paramref name="callBack"/> method when an event with the name <paramref name="eventName"/> is dispatched
        /// </summary>
        /// <typeparam name="T">Any type inherited of Event or Event itself</typeparam>
        /// <param name="name">A string that represents the name of the event</param>
        /// <param name="callBack">A method that accepts as unique parameter an instance of the type specified in <typeparamref name="T"/> and that will be called when an event with the name specified in <paramref name="eventName"/> is dispatched</param>
        public void On<T>(object name, Action<T> callBack)
        where T : Event
        {
            AddEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Adds an event listener on the instance that will call the <paramref name="callBack"/> method when an event with the name <paramref name="eventName"/> is dispatched
        /// </summary>
        /// <typeparam name="T">Any type inherited of Event or Event itself</typeparam>
        /// <param name="name">A string that represents the name of the event</param>
        /// <param name="callBack">A method that accepts as unique parameter an instance of the type specified in <typeparamref name="T"/> and that will be called when an event with the name specified in <paramref name="eventName"/> is dispatched</param>
        public void AddEventListener<T>(object name, Action<T> callBack)
        where T : Event
        {
            Ed.AddEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Removes a previously added event listener on this instance, stop invoking the callback method when the event is dispatched
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callBack"></param>
        public void Off<T>(object name, Action<T> callBack)
        where T : Event
        {
            RemoveEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Removes a previously added event listener on this instance, stop invoking the callback method when the event is dispatched
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callBack"></param>
        public void RemoveEventListener<T>(object name, Action<T> callBack)
        where T : Event
        {
            Ed.RemoveEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Dispatches an event, call all the callback methods registered with <see cref="AddEventListener{T}(object, Action{T})"/> for this event (depending on its name) and pass this event object as parameter to these methods
        /// </summary>
        /// <param name="e">An Event or inherited object that will be pass as parameter to the callback methods registered to this event (depending on its name)</param>
        public void DispatchEvent(Event e)
        {
            Ed.DispatchTargetedEvent(e, this);
        }
        /// <summary>
        /// Removes all event listeners registered to the specified <paramref name="eventName"/>. If not provided or null, then all event listeners of all type will be removed on this instance.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveAllEventListeners(object name = null)
        {
            Ed.RemoveAllEventListeners(name);
        }
        /// <summary>
        /// Returns true if an event listener of the specified type has been added with <see cref="AddEventListener{T}(object, Action{T})"/>
        /// </summary>
        /// <param name="name">A string that represents the name of the event</param>
        /// <returns></returns>
        public bool HasEventListener(object name)
        {
            return (Ed.HasEventListener(name));
        }
        private void OnDestroy()
        {
            RemoveAllEventListeners();
        }
    }
}
