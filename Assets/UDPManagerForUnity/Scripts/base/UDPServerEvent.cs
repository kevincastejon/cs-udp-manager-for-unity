namespace kevincastejon
{
    /// <summary>
    /// An UDPServerEvent object is dispatched whenever a UDPServer event occurs.
    /// 
    /// These are all the available names for this event:
    /// 
    /// <list type="UDPServerEvent">
    /// <item>CLIENT_CONNECTED</item> <description> - Dispatched when a client is connected to the instance </description>
    /// <item>CLIENT_RECONNECTED</item> <description> - Dispatched when an already connected client connects again to the instance (client with same address and port) </description>
    /// <item>CLIENT_PONG</item> <description> - Dispatched when the client has responded to a ping </description>
    /// <item>CLIENT_TIMED_OUT</item> <description> - Dispatched when the client has stopped responding </description>
    /// <item>CLIENT_SENT_DATA</item> <description> - Dispatched when the client has sent data to the instance </description>
    /// </list>
    /// </summary>
    public class UDPServerEvent : UDPManagerEvent
    {
        /// <summary>
        /// /// An enum containing all the names of the UDPManager events
        /// </summary>
        #pragma warning disable 108
        public enum Names { CLIENT_CONNECTED, CLIENT_RECONNECTED, CLIENT_PONG, CLIENT_TIMED_OUT, CLIENT_SENT_DATA };

        private UDPPeer _udpPeer;
        internal UDPServerEvent(object name, UDPPeer udpPeer, UDPDataInfo udpDataInfo = null) : base(name, udpDataInfo)
        {
            this._udpPeer = udpPeer;
        }
        /// <summary>
        /// An <see cref="UDPPeer"/> object that holds informations about the peer related to the event
        /// </summary>
        public UDPPeer UDPpeer
        {
            get
            {
                return (this._udpPeer);
            }
        }
    }
}