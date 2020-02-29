using System;

namespace kevincastejon
{
    /// <summary>
    /// An UDPPeer object contains informations about the remote peer
    /// </summary>
    public class UDPPeer : EventDispatcher
    {
        private int _ID;
        private string _address;
        private int _port;
        private int _lastPing;
        private int _averagePing;
        private int _numPings;
        private Timer _pingTimer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        internal UDPPeer(string address, int port)
        {
            Random r = new Random();
            _ID = int.Parse(r.NextDouble().ToString().Substring(2, 9));
            _pingTimer = new Timer(1000, 1);
            this._address = address; this._port = port;
            _pingTimer.AddEventListener<TimerEvent>(TimerEvent.Names.TIMER_COMPLETE, this._TimerHandler);
        }
        /// <summary>
        /// A unique ID for the UDPPeer
        /// </summary>
        public int ID
        {
            get
            {
                return _ID;
            }
        }
        /// <summary>
        /// The IPV4 address of the remote user
        /// </summary>
        public string Address
        {
            get
            {
                return _address;
            }
        }
        /// <summary>
        /// The IPV4 port of the remote user
        /// </summary>
        public int Port
        {
            get
            {
                return _port;
            }
        }
        /// <summary>
        /// The time in milliseconds the last ping message took to be received and the delivery confirmation to be received
        /// </summary>
        public int LastPing
        {
            get
            {
                return _lastPing;
            }
        }
        /// <summary>
        /// The average time in milliseconds the ping messages took to be received and the delivery confirmations to be received
        /// </summary>
        public int AveragePing
        {
            get
            {
                return _averagePing;
            }
        }
        internal void StartPingTimer()
        {
            _pingTimer.Start();
        }
        internal void SetPing(int ping)
        {
            _lastPing = ping;
            _averagePing = ((_averagePing * _numPings) + ping) / (_numPings + 1);
            _numPings++;
        }
        internal void Close()
        {
            _pingTimer.Stop();
            _pingTimer.RemoveEventListener<TimerEvent>(TimerEvent.Names.TIMER, this._TimerHandler);

        }
        private void _TimerHandler(TimerEvent e)
        {
            this.DispatchEvent(new Event("ping"));
        }

    }

}