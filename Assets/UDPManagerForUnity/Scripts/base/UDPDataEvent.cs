namespace kevincastejon
{
    /// <summary>
    /// An UDPDataEvent object is dispatched whenever a UDPClient event occurs.
    /// 
    /// These are all the available names for this event:
    /// <list type="UDPDataEvent">
    /// <item>SENT</item> <description> - Dispatched when the message is sent </description>
    /// <item>DELIVERED</item> <description> - Dispatched when the message is delivered </description>
    /// <item>RETRIED</item> <description> - Dispatched when the message sending is retried </description>
    /// <item>CANCELED</item> <description> - Dispatched when the message sending is canceled </description>
    /// </list>
    /// </summary>
    public class UDPDataEvent : Event
    {
        /// <summary>
        /// An enum containing all the names of the UDPManager events
        /// </summary>
        #pragma warning disable 108
        public enum Names { SENT, DELIVERED, RETRIED, CANCELED };
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">A string representing the event name</param>
        internal UDPDataEvent(object name) : base(name)
        {

        }
    }
}