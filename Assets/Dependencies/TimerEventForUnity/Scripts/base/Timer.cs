using System.Collections.Generic;
using System.Timers;
namespace kevincastejon
{
    /// <summary>
    /// The TimerEvent package encapsulates the native System.Timers.Timer and the EventManager library together into a event-driven user friendly object.
    /// You can specify a delay in millisecond and a number of repeat or make it a endless loop
    /// You can then add event listeners to it to get the needed callbacks
    /// 
    /// Basic usage:
    /// 
    /// <code>
    ///         // Instantiates a timer that will fire 3 times every 1000 ms
    ///         Timer t = new Timer(1000, 3);
    ///         // Adds listeners to it
    ///         t.AddEventListener<TimerEvent>(TimerEvent.Names.TIMER, TimerHandler);
    ///         t.AddEventListener<TimerEvent>(TimerEvent.Names.TIMER_COMPLETE, TimerHandler);
    ///         // Starts the timer
    ///         t.Start();
    ///         
    ///         private void TimerHandler(TimerEvent e)
    ///         {
    ///         Timer t = e.Target as Timer;
    ///         Console.WriteLine(e.Name+" Repeat Count:"+t.RepeatCount+"/"+t.Repeat+"  Delay:"+t.Delay+"ms  Running:"+t.Running);
    ///         }
    /// 
    /// </code>
    /// 
    /// </summary>
    public class Timer : EventDispatcher
    {
        private double _delay;
        private int _repeat;
        private int _repeatCount = 0;
        private bool _running = false;
        private System.Timers.Timer _timer;
        private static List<Timer> timers = new List<Timer>();
        private static readonly object locker = new object();
        public string name;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="delay">The number of milliseconds between each time the Timer instance will dispatch a TimerEvent event</param>
        /// <param name="repeat">The number of times the Timer instance will repeat. 1 means the events will be dispatched once. 0 means it will repeat indefinitely until the <see cref="Stop"/> method is called</param>
        public Timer(double delay, int repeat = 0, string name = null)
        {
            this._delay = delay;
            if (this._delay == 0)
            {
                this._delay = 0.01f;
            }
            this._repeat = repeat;
            this.name = name;
            timers.Add(this);
        }
        /// <summary>
        /// Starts the countdown
        /// </summary>
        public void Start()
        {
            if (this.Running)
            {
                Stop();
            }
            _running = true;
            _repeatCount = 0;
            _timer = new System.Timers.Timer(Delay);
            _timer.Start();
            _timer.Elapsed += InternalCallback;
        }
        /// <summary>
        /// Stops the countdown
        /// </summary>
        public void Stop()
        {
            if (this.Running) this._running = false;
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= InternalCallback;
                _timer.Dispose();
            }

        }
        /// <summary>
        /// The number of milliseconds between each time the Timer instance will dispatch a TimerEvent event
        /// </summary>
        public double Delay
        {
            get { return (_delay); }
            set
            {
                _delay = value;
                if (_delay == 0)
                {
                    _delay = 0.01f;
                }
            }
        }
        /// <summary>
        /// The number of times the Timer instance will repeat. 1 means the events will be dispatched once. 0 means it will repeat undefinitely until the <see cref="Stop"/> method is called
        /// </summary>
        public int Repeat { get { return (_repeat); } set { _repeat = value; } }
        /// <summary>
        /// The current number of repeat count
        /// </summary>
        public int RepeatCount { get { return (_repeatCount); } }
        /// <summary>
        /// Is true if the Timer instance is currently running
        /// </summary>
        public bool Running { get { return (_running); } }

        private void InternalCallback(object source, ElapsedEventArgs e)
        {
            lock (locker)
            {
                this.DispatchEvent(new TimerEvent(TimerEvent.Names.TIMER));
                this._repeatCount++;

                if (this.RepeatCount == this.Repeat)
                {
                    _timer.Elapsed -= InternalCallback;
                    _timer.Dispose();
                    _timer = null;
                    this.DispatchEvent(new TimerEvent(TimerEvent.Names.TIMER_COMPLETE));
                }
                else
                {
                    _timer.Interval = Delay;
                }
            }
        }
        public void Destroy()
        {
            this.Stop();
            this.RemoveAllEventListeners();
            timers.Remove(this);
        }

        public static void DestroyAll()
        {
            while (timers.Count > 0)
            {
                timers[0].Destroy();
            }
        }
    }
    /// <summary>
    /// An TimerEvent object is dispatched whenever a Timer event occurs.
    /// 
    /// These are all the available names for this event:
    /// 
    /// <list type="UDPServerEvent">
    /// <item>TIMER</item> <description> - Dispatched by a Timer instance each time the delay has ended</description>
    /// <item>TIMER_COMPLETE</item> <description> - Dispatched each time the RepeatCount has reached the Repeat property on a Timer instance </description>
    /// </list>
    /// </summary>
    public class TimerEvent : Event
    {
        #pragma warning disable 108
        public enum Names { TIMER_COMPLETE, TIMER };

        public TimerEvent(object name) : base(name)
        {

        }
    }
}