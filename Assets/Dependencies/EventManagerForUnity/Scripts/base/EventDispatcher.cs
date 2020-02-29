using System;
using System.Collections.Generic;
using UnityEngine;

namespace kevincastejon
{
    /// <summary>
    ///
    /// EventDispatcher package encapsulates the delegate system into an event flow similar to the Javascript event system. These are main advantages:
    ///
    /// - No delegate to declare
    /// - Callback any method that accept an Event (or custom inherited class) object as parameter.
    /// - The events can be dispatched without throwing exception even if no listeners are registered
    /// - Extends easily base class Event to add properties suiting your needs
    ///
    /// Simple usage, without any extends
    ///
    /// <code>
    ///
    ///         //Instantiate an EventDispatcher and add a listener
    ///         EventDispatcher ed = new EventDispatcher();
    ///         ed.AddEventListener<Event>("someEventName", MyCallback);
    ///
    ///         //Then somewhere in your code, call the DispatchEvent method of your EventDispatcher instance
    ///         ed.DispatchEvent(new Event("someEventName"));
    ///
    ///         private void MyCallback(Event e){
    ///         Console.Writeline(e.Name+" event has been dispatched");
    ///         }
    ///
    /// </code>
    ///
    /// Advanced usage, with extending of EventDispatcher and Event
    ///
    /// <code>
    ///
    ///         public class RiceBag : EventDispatcher
    ///         {
    ///             private int _maxRiceGrain = 100;
    ///             private int _currentRiceGrainNumber = 0;
    ///
    ///             public void AddRice(int numberOfRiceGrain)
    ///             {
    ///                 _currentRiceGrainNumber += numberOfRiceGrain;
    ///                 if (_currentRiceGrainNumber > _maxRiceGrain) _currentRiceGrainNumber = _maxRiceGrain;
    ///                 DispatchEvent(new ContainerEvent(ContainerEvent.Names.ELEMENT_ADDED, _currentRiceGrainNumber, _maxRiceGrain));
    ///                 if (_currentRiceGrainNumber == _maxRiceGrain) DispatchEvent(new ContainerEvent(ContainerEvent.Names.FULL, _currentRiceGrainNumber, _maxRiceGrain));
    ///             }
    ///             public void RemoveRice(int numberOfRiceGrain)
    ///             {
    ///                 _currentRiceGrainNumber -= numberOfRiceGrain;
    ///                 if (_currentRiceGrainNumber < 0) _currentRiceGrainNumber = 0;
    ///                 DispatchEvent(new ContainerEvent(ContainerEvent.Names.ELEMENT_REMOVED, _currentRiceGrainNumber, _maxRiceGrain));
    ///                 if (_currentRiceGrainNumber == 0) DispatchEvent(new ContainerEvent(ContainerEvent.Names.EMPTY, _currentRiceGrainNumber, _maxRiceGrain));
    ///             }
    ///         }
    ///         public class ContainerEvent : Event
    ///         {
    ///             public enum Names { ELEMENT_ADDED, ELEMENT_REMOVED, FULL, EMPTY };
    ///             private int _numberOfElements;
    ///             private int _maxElements;
    ///
    ///             public ContainerEvent(object name, int numberOfElements, int maxElements) : base(name)
    ///             {
    ///                 _numberOfElements = numberOfElements;
    ///                 _maxElements = maxElements;
    ///             }
    ///             public int NumberOfElements { get { return (_numberOfElements); } }
    ///             public int MaxElements { get { return (_maxElements); } }
    ///             public float FillingRatio { get { return ((float)_numberOfElements / _maxElements); } }
    ///         }
    ///         public class TesterClass
    ///         {
    ///             public TesterClass()
    ///             {
    ///                 //Instantiate a RiceBag which extends EventDispatcher and add listeners to it
    ///                 RiceBag bag = new RiceBag();
    ///                 bag.AddEventListener<ContainerEvent>(ContainerEvent.Names.ELEMENT_ADDED, MyCallback);
    ///                 bag.AddEventListener<ContainerEvent>(ContainerEvent.Names.ELEMENT_REMOVED, MyCallback);
    ///                 bag.AddEventListener<ContainerEvent>(ContainerEvent.Names.EMPTY, MyCallback);
    ///                 bag.AddEventListener<ContainerEvent>(ContainerEvent.Names.FULL, MyCallback);
    ///
    ///                 //Then anywhere in your code, add and remove some rice off the bag
    ///                 bag.AddRice(26);
    ///                 bag.AddRice(48);
    ///                 bag.RemoveRice(12);
    ///                 bag.AddRice(70);
    ///                 bag.RemoveRice(200);
    ///             }
    ///             private void MyCallback(ContainerEvent e)
    ///             {
    ///                 //Monitors the events dispatched by the bag
    ///                 Debug.Log(e.Name + " - " + e.NumberOfElements + " rice grains on " + e.MaxElements + ". The bag is full at " + e.FillingRatio + "%");
    ///             }
    ///         }
    ///
    /// </code>
    ///
    /// </summary>
    public class EventDispatcher
    {
        private List<Action<Event>> listenersWrappers = new List<Action<Event>>();
        private List<object> listenersCallBack = new List<object>();
        private List<string> listenersType = new List<string>();
        private static List<EventDispatcher> dispatchers = new List<EventDispatcher>();
        private object defaultTarget = null;
        /// <summary>
        /// Alias convenience shortcut for AddEventListener<T> method
        /// </summary>
        /// <typeparam name="T">Any type inherited of Event or Event itself</typeparam>
        /// <param name="eventName">A string that represents the name of the event</param>
        /// <param name="callBack">A method that accepts as unique parameter an instance of the type specified in <typeparamref name="T"/> and that will be called when an event with the name specified in <paramref name="eventName"/> is dispatched</param>
        public void On<T>(object eventName, Action<T> callBack)
        where T : Event
        {
            this.AddEventListener<T>(eventName, callBack);
        }

        /// <summary>
        /// Adds an event listener on the instance that will call the <paramref name="callBack"/> method when an event with the name <paramref name="eventName"/> is dispatched
        /// </summary>
        /// <typeparam name="T">Any type inherited of Event or Event itself</typeparam>
        /// <param name="eventName">A string that represents the name of the event</param>
        /// <param name="callBack">A method that accepts as unique parameter an instance of the type specified in <typeparamref name="T"/> and that will be called when an event with the name specified in <paramref name="eventName"/> is dispatched</param>
        public void AddEventListener<T>(object eventName, Action<T> callBack)
        where T : Event
        {
            string stringType = eventName.ToString();
            listenersType.Add(stringType);
            listenersWrappers.Add(x => callBack((T)x));
            listenersCallBack.Add(callBack);
            if (listenersCallBack.Count == 1) { dispatchers.Add(this); }
        }
        /// <summary>
        /// Alias convenience shortcut for RemoveEventListener<T> method
        /// </summary>
        /// <typeparam name="T">The type of the event you want to remove the listener, must be Event or inherited</typeparam>
        /// <param name="eventName">A string that represents the name of the event you want to remove the listener </param>
        /// <param name="callBack">A method that you declared as callback for the event listener you want to remove </param>
        public void Off<T>(object eventName, Action<T> callBack)
            where T : Event
        {
            this.RemoveEventListener<T>(eventName, callBack);
        }
        /// <summary>
        /// Removes a previously added event listener on this instance, stop the calling of the callback method when the event is dispatched
        /// </summary>
        /// <typeparam name="T">The type of the event you want to remove the listener, must be Event or inherited</typeparam>
        /// <param name="eventName">A string that represents the name of the event you want to remove the listener </param>
        /// <param name="callBack">A method that you declared as callback for the event listener you want to remove </param>
        public void RemoveEventListener<T>(object eventName, Action<T> callBack)
            where T : Event
        {
            int max = listenersType.Count;
            for (int i = 0; i < max; i++)
            {
                if (listenersType[i] == eventName.ToString() && ((Action<T>)listenersCallBack[i]).Method == callBack.Method)
                {
                    listenersType.RemoveAt(i);
                    listenersWrappers.RemoveAt(i);
                    listenersCallBack.RemoveAt(i);
                    if (listenersCallBack.Count == 0) {
                        dispatchers.Remove(this);
                    }
                    break;
                }
            }
        }
        /// <summary>
        /// Dispatches an event, call all the callback methods registered with <see cref="AddEventListener{T}(object, Action{T})"/> for this event (depending on its name) and pass this event object as parameter to these methods
        /// </summary>
        /// <param name="evt">An Event or inherited object that will be pass as parameter to the callback methods registered to this event (depending on its name)</param>
        public void DispatchEvent(Event evt)
        {
            DispatchTargetedEvent(evt, defaultTarget);
        }
        /// <summary>
        /// Removes all event listeners registered to the specified <paramref name="eventName"/>. If not provided or null, then all event listeners of all type will be removed on this instance.
        /// </summary>
        /// <param name="eventName">A string that represents the name of the event you want to remove the listener </param>
        public void RemoveAllEventListeners(object eventName = null)
        {
            int max = listenersType.Count;
            for (int i = 0; i < max; i++)
            {
                if (eventName == null || listenersType[i] == eventName.ToString())
                {
                    listenersType.RemoveAt(i);
                    listenersWrappers.RemoveAt(i);
                    listenersCallBack.RemoveAt(i);
                    i--;
                    max--;
                }
            }
            if (listenersCallBack.Count == 0) { dispatchers.Remove(this); }
        }
        /// <summary>
        /// Returns true if an event listener of the specified type has been added with <see cref="AddEventListener{T}(object, Action{T})"/>
        /// </summary>
        /// <param name="eventName">A string that represents the name of the event</param>
        /// <returns></returns>
        public bool HasEventListener(object eventName)
        {
            int max = listenersType.Count;
            for (int i = 0; i < max; i++)
                if (listenersType[i] == eventName.ToString()) return (true);
            return (false);
        }
        /// <summary>
        /// Default target for dispatched events, if not specified the EventDispatcher instance is set as default target. Useful for encapsulation.
        /// </summary>
        public object DefaultTarget{
            get
            {
                return (defaultTarget);
            }
            set
            {
                defaultTarget = value;
            }
        }
        /// <summary>
        /// Utilitary static method for removing all event listeners of all type and names on all EventDispatcher's intances. You can specify a list of EventDispatcher's to be excluded from the removing
        /// </summary>
        /// <param name="excludes">A list of EventDispatcher's to be excluded from the removing</param>
        public static void RemoveAllListeners(List<EventDispatcher> excludes = null)
        {
            while (dispatchers.Count > 0)
            {
                if (excludes == null || excludes.Contains(dispatchers[0]) == false)
                    dispatchers[0].RemoveAllEventListeners();
            }
        }
        internal void DispatchTargetedEvent(Event e, object target = null)
        {
            if (ThreadManager.Instance == null) Debug.Log("ThreadManager is not initialized, please place the ThreadManager script on top on your scene hierarchy");
            else ThreadManager.DispatchEventFromMainThread(this, e, target);
        }
        internal void DoDispatch(Event e, object target = null)
        {
            if (target == null) e.Target = this;
            else e.Target = target;
            int max = listenersType.Count;
            List<Action<Event>> filteredListeners = new List<Action<Event>>();
            for (int i = 0; i < max; i++)
            {
                if (listenersType[i] == e.Name) filteredListeners.Add(listenersWrappers[i]);
            }
            max = filteredListeners.Count;
            for (int i = 0; i < max; i++)
            {
                filteredListeners[i](e);
            }
        }
    }
    /// <summary>
    /// An Event object is dispatched whenever an Event occurs
    /// </summary>
    public class Event
    {
        /// <summary>
        /// /// An enum containing all the names of the UDPManager events
        /// </summary>
        public enum Names { COMPLETE, FAIL, CANCEL, CONFIRM };
        private string _name;
        private object _target;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">A string (or enum element) that represents the event name</param>
        public Event(object name)
        {
            this._name = name.ToString();
        }
        /// <summary>
        /// A string (or enum element) that represents the event name
        /// </summary>
        public string Name
        {
            get
            {
                return (this._name);
            }
        }
        /// <summary>
        /// The EventDispatcher instance that dispatched the event
        /// </summary>
        public object Target
        {
            get
            {
                return (this._target);
            }
            set
            {
                this._target = value;
            }
        }
    }
}
