using kevincastejon;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Event = kevincastejon.Event;
using _UDPServer = kevincastejon.UDPServer;

namespace kevincastejon.unity
{
    [Serializable]
    public class UUDPServerEvent : UnityEvent<UDPServerEvent> { }
    [Serializable]
    public class UDPServerListeners
    {
        public UUDPManagerEvent OnBound;
        public UUDPServerEvent OnClientConnected;
        public UUDPServerEvent OnClientPong;
        public UUDPServerEvent OnClientSentData;
        public UUDPServerEvent OnClientTimedOut;
    }
    public class UDPServer : MonoBehaviour
    {
        public int port;
        public UDPServerListeners listeners;
        public List<UChannel> channels;
        _UDPServer udps;
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
            udps = new _UDPServer();
            udps.DefaultTarget = this;
            //Add listeners on the instance of UDPServer
            udps.On<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, (UDPManagerEvent e) =>
            {
                listeners.OnBound?.Invoke(e);
            });
            udps.On<UDPServerEvent>(UDPServerEvent.Names.CLIENT_CONNECTED, (UDPServerEvent e) =>
            {
                listeners.OnClientConnected?.Invoke(e);
            });
            udps.On<UDPServerEvent>(UDPServerEvent.Names.CLIENT_PONG, (UDPServerEvent e) =>
            {
                listeners.OnClientPong?.Invoke(e);
            });
            udps.On<UDPServerEvent>(UDPServerEvent.Names.CLIENT_SENT_DATA, (UDPServerEvent e) =>
            {
                listeners.OnClientSentData?.Invoke(e);
            });
            udps.On<UDPServerEvent>(UDPServerEvent.Names.CLIENT_TIMED_OUT, (UDPServerEvent e) =>
            {
                listeners.OnClientTimedOut?.Invoke(e);
            });

            channels.ForEach((UChannel uc) =>
            {
                udps.AddChannel(uc.Name, uc.GuarantiesDelivery, uc.MaintainOrder, uc.RetryTime, uc.CancelTime);
            });

            udps.Start(port);
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
            udps.AddEventListener<T>(name, callBack);
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
            udps.RemoveEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Dispatches an event, call all the callback methods registered with <see cref="AddEventListener{T}(object, Action{T})"/> for this event (depending on its name) and pass this event object as parameter to these methods
        /// </summary>
        /// <param name="e">An Event or inherited object that will be pass as parameter to the callback methods registered to this event (depending on its name)</param>
        public void DispatchEvent(Event e)
        {
            udps.DispatchTargetedEvent(e, this);
        }
        /// <summary>
        /// Removes all event listeners registered to the specified <paramref name="eventName"/>. If not provided or null, then all event listeners of all type will be removed on this instance.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveAllEventListeners(object name = null)
        {
            udps.RemoveAllEventListeners(name);
        }
        /// <summary>
        /// Returns true if an event listener of the specified type has been added with <see cref="AddEventListener{T}(object, Action{T})"/>
        /// </summary>
        /// <param name="name">A string that represents the name of the event</param>
        /// <returns></returns>
        public bool HasEventListener(object name)
        {
            return (udps.HasEventListener(name));
        }
        /// <summary>
        /// Start the server. Bind to the local port provided and start listening for connections</summary>
        /// <remarks>Only call this method if you did not provide a local port on the constructor parameter(or if you provide -1)</remarks>
        /// <c>If the server is already started (=Bound), the method will call Reset before starting again on the specified localPort</c>
        /// <param name='localPort'>The local port to bind to, you can specify a port from 1 to 65535, 0  will bind to the first available port. Default is 0.</param><c>!Any other value will throw an exception!</c>
        public void Start(int localPort)
        {
            udps.Start(localPort);
        }
        /// <summary>
        /// Add a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to create.</param>
        /// <c>It must be unique on this instance, if a channel with the same name has already been added the method will throw an exception!</c>
        /// <remarks>You can check if the name is already used by calling <see cref="GetChannelByName"/></remarks>
        /// <param name='guarantiesDelivery'>If true the messages sent though this channel will wait for a receipt from the target that will guaranty the delivery. It will wait during the time specified on <paramref name="retryTime"/> until what it will retry sending the message, etc... If false the message is sent once without guranty of delivery. Default is false.<remarks>The guaranty of the delivery works only if the target uses the same library (C#,AS3 or JS) to communicate over UDP!</remarks></param>
        /// <param name='maintainOrder'>If true it will wait for a message to be delivered before sending the next one.<remarks> Only works if <paramref name="guarantiesDelivery"/> is true</remarks></param>
        /// <param name='retryTime'>The number of milliseconds the channel will wait before retrying sending the message if not delivered. Default is 30.</param>
        /// <param name='cancelTime'>The number of milliseconds the channel will wait before canceling the message if not delivered. Default is 500.</param>
        public void AddChannel(string channelName, bool guarantiesDelivery = false, bool maintainOrder = false, float retryTime = 30, float cancelTime = 500)
        {
            udps.AddChannel(channelName, guarantiesDelivery, maintainOrder, retryTime, cancelTime);
        }
        /// <summary>
        /// Removes a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to remove.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        public void RemoveChannel(string channelName)
        {
            udps.RemoveChannel(channelName);
        }
        /// <summary>
        /// Get a registered UDPChannel at the specified index</summary>
        public UDPChannel GetChannelAt(int num)
        {
            return (udps.GetChannelAt(num));
        }
        /// <summary>
        /// Get a registered UDPChannel by specifying his name</summary>
        public UDPChannel GetChannelByName(string channelName)
        {
            return (udps.GetChannelByName(channelName));
        }
        /// <summary>
        /// Get a connected UDPPeer at the specified index</summary>
        /// <remarks>You can list all UDPPeers connected with <see cref="NumClients"/></remarks>
        public UDPPeer GetClientAt(int index)
        {
            return (udps.GetClientAt(index));
        }
        /// <summary>
        /// Send data through an UDPChannel to all the clients and returns an array of <see cref="UDPDataInfo"/> objects.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A literal object that contains the data to send</param>
        public List<UDPDataInfo> SendToAll(string channelName, object udpData)
        {       //sendToAllClients
            return (udps.SendToAll(channelName, udpData));
        }
        /// <summary>
        /// Send data through an UDPChannel to a distant user and returns an <see cref="UDPDataInfo"/> object.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A literal object that contains the data to send</param>
        /// <param name="peer">A connected UDPPeer</param>
        public UDPDataInfo SendToClient(string channelName, object udpData, UDPPeer peer)
        {
            return (udps.SendToClient(channelName, udpData, peer));
        }
        /// <summary>
        /// Send data "classicaly" to a distant user (means no UDPManager features are usable)</summary>
        /// <param name="udpData">A literal object that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public void SendOutOfChannels(object udpData, string remoteAddress, int remotePort)
        {
            udps.SendOutOfChannels(udpData, remoteAddress, remotePort);
        }
        /// <summary>
        /// Add a "white" IP address to the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddWhiteAddress(string address)
        {
            udps.AddWhiteAddress(address);
        }
        /// <summary>
        /// Add a "black" IP address to the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddBlackAddress(string address)
        {
            udps.AddBlackAddress(address);
        }
        /// <summary>
        /// Remove a "white" IP address from the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port). It must be registered on this instance, if that address can't be found on the list the method will throw an exception!<remarks>You can check if the name is registered by calling <see cref="GetWhiteAddressAt(int)"/> and <see cref="WhiteListLength"/></remarks></param>
        public void RemoveWhiteAddress(string address)
        {
            udps.RemoveWhiteAddress(address);
        }
        /// <summary>
        /// Remove a "black" IP address from the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port).</param>
        /// <c>It must be registered on this instance, if that address can't be found on the list the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetBlackAddressAt(int)"/> and <see cref="BlackListLength"/></remarks>
        public void RemoveBlackAddress(string address)
        {
            udps.RemoveBlackAddress(address);
        }
        /// <summary>
        /// Get the white address at the specified index on the whitelist</summary>
        public string GetWhiteAddressAt(int index)
        {
            return (udps.GetWhiteAddressAt(index));
        }
        /// <summary>
        /// Get the black address at the specified index on the blacklist</summary>
        public string GetBlackAddressAt(int index)
        {
            return (udps.GetBlackAddressAt(index));
        }
        /// <summary>
        /// Disconnect the connected UDPPeer</summary>
        public void KickClient(UDPPeer peer)
        {
            udps.KickClient(peer);
        }
        /// <summary>
        /// Disconnect the connected UDPClient and add its IP to the blacklist</summary>
        /// <remarks>The <paramref name="blackListEnabled"/> has to be true for the bannishment to be effective</remarks>
        public void BanClient(UDPPeer peer)
        {
            udps.BanClient(peer);
        }
        /// <summary>
        /// Resets the UDPServer. Means unbind, and remove the UDPChannels if you specify the parameter as true</summary>
        /// <param name='removeChannels'></param>
        public void Close(bool removeChannels = true)
        {
            udps.Close(removeChannels);
        }
        /// <summary>
        /// True if the instance is bound to a port</summary>
        public bool Bound
        {
            get
            {
                return (udps.Bound);
            }
        }
        /// <summary>
        /// Returns the local port on which the UDPServer is started. Returns 0 if the UDPServer is not started yet.
        /// </summary>
        public int BoundPort
        {
            get
            {
                return (udps.BoundPort);
            }
        }
        /// <summary>
        /// The number of UDPChannel registered on the instance</summary>
        public int NumChannels
        {
            get
            {
                return (udps.NumChannels);
            }
        }
        /// <summary>
        /// The number of connected UDPPeers</summary>
        public int NumClients
        {
            get
            {
                return (udps.NumClients);
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the whitelist should be the only ones to be treated or not</summary><seealso cref="AddWhiteAddress(string)"/>
        public bool WhiteListEnabled
        {
            get
            {
                return (udps.WhiteListEnabled);
            }
            set
            {
                udps.WhiteListEnabled = value;
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the blacklist should be ignored or not</summary><seealso cref="AddBlackAddress(string)"/>
        public bool BlackListEnabled
        {
            get
            {
                return (udps.BlackListEnabled);
            }
            set
            {
                udps.BlackListEnabled = value;
            }
        }
        /// <summary>
        /// The length of the whitelist</summary>
        public int WhiteListLength
        {
            get
            {
                return (udps.WhiteListLength);
            }
        }
        /// <summary>
        /// The length of the blacklist</summary>
        public int BlackListLength
        {
            get
            {
                return (udps.BlackListLength);
            }
        }
        private void OnDestroy()
        {
            udps.RemoveAllEventListeners();
            udps.Close();
        }
    }
}