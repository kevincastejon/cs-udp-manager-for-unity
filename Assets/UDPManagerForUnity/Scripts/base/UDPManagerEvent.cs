namespace kevincastejon
{
    /// <summary>
    /// An UDPManagerEvent object is dispatched whenever a UDPManager event occurs.
    /// 
    /// These are all the available names for this event:
    /// <list type="UDPManagerEvent">
    /// <item>BOUND</item> <description> - Dispatched when the instance is bound to a local port </description>
    /// <item>DATA_CANCELED</item> <description> - Dispatched when a data sending is canceled </description>
    /// <item>DATA_DELIVERED</item> <description> - Dispatched when data has been delivered </description>
    /// <item>DATA_RECEIVED</item> <description> - Dispatched when data has been received </description>
    /// <item>DATA_RETRIED</item> <description> - Dispatched when data sending has been retried </description>
    /// <item>DATA_SENT</item> <description> - Dispatched when data has been sent </description>
    /// </list>
    /// </summary>
    public class UDPManagerEvent : Event
    {
        /// <summary>
        /// An enum containing all the names of the UDPManager events
        /// </summary>
        #pragma warning disable 108
        public enum Names { BOUND, DATA_RECEIVED, DATA_SENT, DATA_DELIVERED, DATA_RETRIED, DATA_CANCELED };
        internal const string _SEND_DATA = "sendData";
        private UDPDataInfo _udpDataInfo;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">A string representing the event name</param>
        /// <remarks>You can find all the available names for this event with <see cref="Names"/></remarks>
        /// <param name="udpDataInfo">An <see cref="UDPDataInfo"/> object containing more informations about the Event</param>
        internal UDPManagerEvent(object name, UDPDataInfo udpDataInfo) : base(name)
        {
            this._udpDataInfo = udpDataInfo;
        }
        /// <summary>
        /// The <see cref="UDPDataInfo"/> object that contains informations about the message related to the event
        /// </summary>
        public UDPDataInfo UDPdataInfo
        {
            get
            {
                return _udpDataInfo;
            }
        }
    }
}