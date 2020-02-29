using kevincastejon;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using Event = kevincastejon.Event;
using _UDPManager = kevincastejon.UDPManager;

namespace kevincastejon.unity
{
    [Serializable]
    public class UUDPManagerEvent : UnityEvent<UDPManagerEvent> { }
    [Serializable]
    public class UDPManagerListeners
    {
        public UUDPManagerEvent OnBound;
        public UUDPManagerEvent OnDataCanceled;
        public UUDPManagerEvent OnDataDelivered;
        public UUDPManagerEvent OnDataReceived;
        public UUDPManagerEvent OnDataRetried;
        public UUDPManagerEvent OnDataSent;
    }
    [Serializable]
    public class UChannel
    {
        public UChannel(string name) { Name = name; }
        public string Name;
        public bool GuarantiesDelivery = true;
        public bool MaintainOrder = true;
        public float RetryTime = 30;
        public float CancelTime = 1000;
    }
    public class UDPManager : MonoBehaviour
    {
        public int port;
        public UDPManagerListeners listeners;
        public List<UChannel> channels;
        _UDPManager udpm;
        void Reset()
        {
            channels = new List<UChannel>()
        {
            new UChannel("myChannelName")
        };
        }
        // Start is called before the first frame update
        void Start()
        {
            udpm = new _UDPManager();
            udpm.DefaultTarget = this;

            //Add listeners on the instance of UDPManager
            udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, (UDPManagerEvent e) =>
            {
                listeners.OnBound?.Invoke(e);
            });
            udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, (UDPManagerEvent e) =>
            {
                listeners.OnDataCanceled?.Invoke(e);
            });
            udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, (UDPManagerEvent e) =>
            {
                listeners.OnDataDelivered?.Invoke(e);
            });
            udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RECEIVED, (UDPManagerEvent e) =>
            {
                listeners.OnDataReceived?.Invoke(e);
            });
            udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, (UDPManagerEvent e) =>
            {
                listeners.OnDataRetried?.Invoke(e);
            });
            udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_SENT, (UDPManagerEvent e) =>
            {
                listeners.OnDataSent?.Invoke(e);
            });

            channels.ForEach((UChannel uc) =>
            {
                udpm.AddChannel(uc.Name, uc.GuarantiesDelivery, uc.MaintainOrder, uc.RetryTime, uc.CancelTime);
            });

            udpm.Bind(port);
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
            udpm.AddEventListener<T>(name, callBack);
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
            udpm.AddEventListener<T>(name, callBack);
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
            udpm.RemoveEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Dispatches an event, call all the callback methods registered with <see cref="AddEventListener{T}(object, Action{T})"/> for this event (depending on its name) and pass this event object as parameter to these methods
        /// </summary>
        /// <param name="e">An Event or inherited object that will be pass as parameter to the callback methods registered to this event (depending on its name)</param>
        public void DispatchEvent(Event e)
        {
            udpm.DispatchTargetedEvent(e, this);
        }
        /// <summary>
        /// Removes all event listeners registered to the specified <paramref name="eventName"/>. If not provided or null, then all event listeners of all type will be removed on this instance.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveAllEventListeners(object name = null)
        {
            udpm.RemoveAllEventListeners(name);
        }
        /// <summary>
        /// Returns true if an event listener of the specified type has been added with <see cref="AddEventListener{T}(object, Action{T})"/>
        /// </summary>
        /// <param name="name">A string that represents the name of the event</param>
        /// <returns></returns>
        public bool HasEventListener(object name)
        {
            return (udpm.HasEventListener(name));
        }
        /// <summary>
        /// Bind to a local port </summary>
        /// <remarks>Only call this method if you did not provide a local port on the constructor parameter(or if you provide -1)</remarks>
        /// <c>If the instance is already bound, the method will call Reset before bounding again on the specified localPort</c>
        /// <param name='localPort'>The local port to bind to, you can specify a port from 1 to 65535, 0  will bind to the first available port. Default is 0.</param><c>!Any other value will throw an exception!</c>
        public void Bind(int localPort)
        {
            udpm.Bind(localPort);
        }
        /// <summary>
        /// Add a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to create. It must be unique on this instance, if a channel with the same name has already been added the method will throw an exception!<remarks>You can check if the name is already used by calling <see cref="GetChannelByName"/></remarks></param>
        /// <param name='guarantiesDelivery'>If true the messages sent though this channel will wait for a receipt from the target that will guaranty the delivery. It will wait during the time specified on <paramref name="retryTime"/> until what it will retry sending the message, etc... If false the message is sent once without guranty of delivery. Default is false.<remarks>The guaranty of the delivery works only if the target uses the same library (C#,AS3 or JS) to communicate over UDP!</remarks></param>
        /// <param name='maintainOrder'>If true it will wait for a message to be delivered before sending the next one.<remarks> Only works if <paramref name="guarantiesDelivery"/> is true</remarks></param>
        /// <param name='retryTime'>The number of milliseconds the channel will wait before retrying sending the message if not delivered. Default is 30.</param>
        /// <param name='cancelTime'>The number of milliseconds the channel will wait before canceling the message if not delivered. Default is 500.</param>
        public void AddChannel(string channelName, bool guarantiesDelivery = false, bool maintainOrder = false, float retryTime = 30, float cancelTime = 500)
        {
            udpm.AddChannel(channelName, guarantiesDelivery, maintainOrder, retryTime, cancelTime);
        }
        /// <summary>
        /// Removes a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to remove.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        public void RemoveChannel(string channelName)
        {
            udpm.RemoveChannel(channelName);
        }
        /// <summary>
        /// Get a registered UDPChannel at the specified index</summary>
        public UDPChannel GetChannelAt(int index)
        {
            return (udpm.GetChannelAt(index));
        }
        /// <summary>
        /// Get a registered UDPChannel by specifying his name. Returns null if no channel with that has been found.</summary>
        public UDPChannel GetChannelByName(string channelName)
        {
            return (udpm.GetChannelByName(channelName));
        }
        /// <summary>
        /// Send data through an UDPChannel to a distant user and returns an <see cref="UDPDataInfo"/> object.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A <see cref="JavaScriptObject"/> that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public UDPDataInfo Send(string channelName, object udpData, string remoteAddress, int remotePort)
        {
            return (udpm.Send(channelName, udpData, remoteAddress, remotePort));
        }
        /// <summary>
        /// Send data "classicaly" to a distant user (means no UDPManager features are usable)</summary>
        /// <param name="udpData">A <see cref="JavaScriptObject"/> that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public void SendOutOfChannels(object udpData, string remoteAddress, int remotePort)
        {
            udpm.SendOutOfChannels(udpData, remoteAddress, remotePort);

        }
        /// <summary>
        /// Add a "white" IP address to the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddWhiteAddress(string address)
        {
            udpm.AddBlackAddress(address);
        }
        /// <summary>
        /// Add a "black" IP address to the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddBlackAddress(string address)
        {
            udpm.AddBlackAddress(address);
        }
        /// <summary>
        /// Remove a "white" IP address from the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port). It must be registered on this instance, if that address can't be found on the list the method will throw an exception!<remarks>You can check if the name is registered by calling <see cref="GetWhiteAddressAt(int)"/> and <see cref="WhiteListLength"/></remarks></param>
        public void RemoveWhiteAddress(string address)
        {
            udpm.RemoveWhiteAddress(address);
        }
        /// <summary>
        /// Remove a "black" IP address from the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port).</param>
        /// <c>It must be registered on this instance, if that address can't be found on the list the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetBlackAddressAt(int)"/> and <see cref="BlackListLength"/></remarks>
        public void RemoveBlackAddress(string address)
        {
            udpm.RemoveBlackAddress(address);
        }
        /// <summary>
        /// Get the white address at the specified index on the whitelist</summary>
        public string GetWhiteAddressAt(int index)
        {
            return (udpm.GetWhiteAddressAt(index));
        }
        /// <summary>
        /// Get the black address at the specified index on the blacklist</summary>
        public string GetBlackAddressAt(int index)
        {
            return (udpm.GetBlackAddressAt(index));
        }
        /// <summary>
        /// Unbind from the localport</summary>
        /// <param name='removeChannels'>Remove all the added UDPChannels, true by default
        /// </param>
        public void Close(bool removeChannels = true)
        {
            udpm.Close(removeChannels);
        }
        /// <summary>
        /// Utilitary static method to reset all instances of UDPManager on the program</summary>
        public static void ResetAllUDPManagers(bool removeChannels = true)
        {
            _UDPManager.ResetAllUDPManagers(removeChannels);
        }
        /// <summary>
        /// Gets the LAN broadcast address
        /// </summary>
        public static List<IPAddress> GetBroadcastAddresses()
        {
            return (_UDPManager.GetBroadcastAddresses());
        }
        /// <summary>
        /// True if the instance is bound to a port</summary>
        public bool Bound
        {
            get
            {
                return (udpm.Bound);
            }
        }
        /// <summary>
        /// Returns the local port on which the UDPManager is bound. Returns 0 if the UDPManager is not bound yet.
        /// </summary>
        public int BoundPort
        {
            get
            {
                return (udpm.BoundPort);
            }
        }
        /// <summary>
        /// The number of UDPChannel registered on the instance</summary>
        public int NumChannels
        {
            get
            {
                return udpm.NumChannels;
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the whitelist should be the only ones to be treated or not</summary><seealso cref="AddWhiteAddress(string)"/>
        public bool WhiteListEnabled
        {
            get
            {
                return udpm.WhiteListEnabled;
            }
            set
            {
                udpm.WhiteListEnabled = value;
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the blacklist should be ignored or not</summary><seealso cref="AddBlackAddress(string)"/>
        public bool BlackListEnabled
        {
            get
            {
                return udpm.BlackListEnabled;
            }
            set
            {
                udpm.BlackListEnabled = value;
            }
        }
        /// <summary>
        /// The length of the whitelist</summary>
        public int WhiteListLength
        {
            get
            {
                return udpm.WhiteListLength;
            }
        }
        /// <summary>
        /// The length of the blacklist</summary>
        public int BlackListLength
        {
            get
            {
                return udpm.BlackListLength;
            }
        }
        private void OnDestroy()
        {
            udpm.RemoveAllEventListeners();
            udpm.Close();
        }
    }
}