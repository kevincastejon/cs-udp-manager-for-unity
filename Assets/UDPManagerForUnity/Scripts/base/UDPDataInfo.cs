namespace kevincastejon
{
    /// <summary>
    /// An UDPDataInfo object holds informations about data sent or received
    /// 
    /// You usually retrieve an UDPDataInfo object through an event object on a callback method
    /// 
    /// Basic usage:
    /// 
    /// <code>
    /// 
    ///         client.AddEventListener<UDPClientEvent>(UDPClientEvent.SERVER_SENT_DATA, MyCallback);
    /// 
    ///         private void MyCallback(UDPClientEvent e){
    ///         Console.WriteLine("["+e.UdpDataInfo.RemoteAddress+":"+e.UdpDataInfo.RemotePort+"]"+e.UdpDataInfo.Data);
    ///         }
    /// 
    /// </code>
    /// 
    /// You can also, if you prefer, retrieve the UDPDataInfo object when sending a message (the differents sending methods all returns UDPDataInfo objects excepts the UDPManager SendOutOfChannels method)
    /// 
    /// <code>
    /// 
    ///         UDPDataInfo dataInf = client.SendToServer("someChannel", JSO.FromObject(new{ myData=5 , myOtherData="some string"}));
    ///         dataInf.AddEventListener<UDPDataEvent>(UDPDataEvent.SENT, MyCallback);
    ///         dataInf.AddEventListener<UDPDataEvent>(UDPDataEvent.DELIVERED, MyCallback);
    ///         dataInf.AddEventListener<UDPDataEvent>(UDPDataEvent.RETRIED, MyCallback);
    ///         dataInf.AddEventListener<UDPDataEvent>(UDPDataEvent.CANCELED, MyCallback);
    /// 
    /// </code>
    /// 
    /// </summary>
    public class UDPDataInfo : EventDispatcher
    {

        private string _channelName;
        private string _remoteAddress;
        private int _remotePort;
        private int _ID;
        private dynamic _data;
        private Timer _retryTimer;
        private Timer _cancelTimer;
        private int _ping;
        private bool _received;
        private bool _canceled;
        private int _numRetry;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="channelName">A string representing the channel name</param>
        /// <param name="remoteAddress">The IPV4 address of the sender or the target</param>
        /// <param name="remotePort">The IPV4 port of the sender or the target</param>
        /// <param name="data">A <see cref="JavaScriptObject"/> that contains the data sent or received</param>
        /// <param name="ID">A unique ID for this message</param>
        internal UDPDataInfo(string channelName, string remoteAddress, int remotePort, object data, int ID)
        {

            this._channelName = channelName; this._ID = ID; this._data = data; this._remoteAddress = remoteAddress; this._remotePort = remotePort;
        }
        /// <summary>
        /// A string representing the channel name
        /// </summary>
        public string ChannelName
        {
            get
            {
                return (this._channelName);
            }
        }
        /// <summary>
        /// The IPV4 address of the sender or the target
        /// </summary>
        public string RemoteAddress
        {
            get
            {
                return (this._remoteAddress);
            }
        }
        /// <summary>
        /// The IPV4 port of the sender or the target
        /// </summary>
        public int RemotePort
        {
            get
            {
                return (this._remotePort);
            }
        }
        /// <summary>
        /// A <see cref="JavaScriptObject"/> that contains the data sent or received
        /// </summary>
        public dynamic Data
        {
            get
            {
                return (this._data);
            }
        }
        /// <summary>
        /// A unique ID for this message
        /// </summary>
        public int ID
        {
            get
            {
                return (this._ID);
            }
        }
        /// <summary>
        /// Is true if the message has been received
        /// </summary>
        public bool Received
        {
            get
            {
                return (this._received);
            }
        }
        /// <summary>
        /// Is true if the message has been canceled
        /// </summary>
        public bool Canceled
        {
            get
            {
                return (this._canceled);
            }
        }
        /// <summary>
        /// The number of times the message sending has been retried
        /// </summary>
        public int NumRetry
        {
            get
            {
                return (this._numRetry);
            }
        }
        /// <summary>
        /// The number of milliseconds the message took to reach its destination and the delivery confirmation to be received
        /// </summary>
        public int Ping
        {
            get
            {
                return (this._ping);
            }
        }
        internal void _SetCanceled(bool value)
        {
            this._canceled = value;
        }
        internal void _Send(double retryTime, double cancelTime)
        {
            _ping = (int)UDPManager.chrono.Elapsed.TotalSeconds;
            if (retryTime > 0)
            {
                _retryTimer = new Timer(retryTime, 0);
                _retryTimer.AddEventListener<TimerEvent>(TimerEvent.Names.TIMER, this._RetryTimerHandler);
                _retryTimer.Start();
            }
            if (cancelTime > 0)
            {
                _cancelTimer = new Timer(cancelTime, 1);
                _cancelTimer.AddEventListener<TimerEvent>(TimerEvent.Names.TIMER_COMPLETE, this._CancelTimerHandler);
                _cancelTimer.Start();
            }

            this.DispatchEvent(new UDPDataEvent(UDPDataEvent.Names.SENT));
        }

        private void _RetryTimerHandler(TimerEvent e)
        {
            this._numRetry++;
            this.DispatchEvent(new UDPDataEvent(UDPDataEvent.Names.RETRIED));
        }

        private void _CancelTimerHandler(TimerEvent e)
        {
            this._retryTimer.RemoveEventListener<TimerEvent>(TimerEvent.Names.TIMER, this._RetryTimerHandler);
            this._cancelTimer.RemoveEventListener<TimerEvent>(TimerEvent.Names.TIMER_COMPLETE, this._CancelTimerHandler);
            //JS BUG
            this._cancelTimer.Stop(); this._retryTimer.Stop();
            this._cancelTimer = null; this._retryTimer = null;
            this.DispatchEvent(new UDPDataEvent(UDPDataEvent.Names.CANCELED));

        }
        internal void _SetReceived(bool value)
        {
            this._retryTimer.RemoveEventListener<TimerEvent>(TimerEvent.Names.TIMER, this._RetryTimerHandler);
            this._cancelTimer.RemoveEventListener<TimerEvent>(TimerEvent.Names.TIMER_COMPLETE, this._CancelTimerHandler);
            this._cancelTimer.Stop(); this._retryTimer.Stop();
            this._ping = (int)UDPManager.chrono.Elapsed.TotalSeconds - _ping;
            this._cancelTimer = null; this._retryTimer = null;
            this._received = value;
            this.DispatchEvent(new UDPDataEvent(UDPDataEvent.Names.DELIVERED));
        }
        internal void Close()
        {
            this._retryTimer?.Stop();
            this._retryTimer?.RemoveAllEventListeners();
            this._cancelTimer?.Stop();
            this._cancelTimer?.RemoveAllEventListeners();
        }
    }
}