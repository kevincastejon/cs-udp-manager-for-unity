using System;
using UnityEngine;
using UnityEngine.Events;
using _Timer = kevincastejon.Timer;
namespace kevincastejon.unity
{
    [Serializable]
    public class UTimerEvent : UnityEvent<TimerEvent> { }
    public class Timer : MonoBehaviour
    {
        [Header("Duration in ms")]
        public int Duration = 1000;
        [Header("Number of repeat (0 is endless)")]
        public int NumberOfRepeat = 0;
        [Header("Autostart")]
        public bool autoStart = false;
        public UTimerEvent OnTimer;
        public UTimerEvent OnTimerComplete;
        _Timer t;
        // Start is called before the first frame update
        void Start()
        {
            t = new _Timer(Duration, NumberOfRepeat);
            t.DefaultTarget = this;
            t.On<TimerEvent>(TimerEvent.Names.TIMER.ToString(), (TimerEvent e) =>
            {
                OnTimer?.Invoke(e);
            });
            t.On<TimerEvent>(TimerEvent.Names.TIMER_COMPLETE.ToString(), (TimerEvent e) =>
            {
                OnTimerComplete?.Invoke(e);
            });
            if (autoStart)
            {
                Begin();
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Starts the countdown
        /// </summary>
        public void Begin()
        {
            t.Start();
        }
        /// <summary>
        /// Stops the countdown
        /// </summary>
        public void Stop()
        {
            t.Stop();
        }
        /// <summary>
        /// The number of milliseconds between each time the Timer instance will dispatch a TimerEvent event
        /// </summary>
        public double Delay
        { get { return (t.Delay); } set { t.Delay = value; } }
        /// <summary>
        /// The number of times the Timer instance will repeat. 1 means the events will be dispatched once. 0 means it will repeat undefinitely until the <see cref="Stop"/> method is called
        /// </summary>
        public int Repeat { get { return (t.Repeat); } set { t.Repeat= value; } }
        /// <summary>
        /// The current number of repeat count
        /// </summary>
        public int RepeatCount { get { return (t.RepeatCount); } }
        /// <summary>
        /// Is true if the Timer instance is currently running
        /// </summary>
        public bool Running { get { return (t.Running); } }
        public void Destroy()
        {
            t.Destroy();
        }

        public static void DestroyAll()
        {
            _Timer.DestroyAll();
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
            t.AddEventListener<T>(name, callBack);
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
            t.RemoveEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Dispatches an event, call all the callback methods registered with <see cref="AddEventListener{T}(object, Action{T})"/> for this event (depending on its name) and pass this event object as parameter to these methods
        /// </summary>
        /// <param name="e">An Event or inherited object that will be pass as parameter to the callback methods registered to this event (depending on its name)</param>
        public void DispatchEvent(Event e)
        {
            t.DispatchTargetedEvent(e, this);
        }
        /// <summary>
        /// Removes all event listeners registered to the specified <paramref name="eventName"/>. If not provided or null, then all event listeners of all type will be removed on this instance.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveAllEventListeners(object name = null)
        {
            t.RemoveAllEventListeners(name);
        }
        /// <summary>
        /// Returns true if an event listener of the specified type has been added with <see cref="AddEventListener{T}(object, Action{T})"/>
        /// </summary>
        /// <param name="name">A string that represents the name of the event</param>
        /// <returns></returns>
        public bool HasEventListener(object name)
        {
            return (t.HasEventListener(name));
        }

        private void OnDestroy()
        {
            t.Destroy();
        }
    }
}