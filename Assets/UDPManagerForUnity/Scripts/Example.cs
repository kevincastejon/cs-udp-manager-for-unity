using kevincastejon;
using UnityEngine;
using UDPClient = kevincastejon.unity.UDPClient;
using UDPManager = kevincastejon.unity.UDPManager;
using UDPServer = kevincastejon.unity.UDPServer;

namespace UDPManagerExample
{
    public class Example : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnUDPServerEvent(UDPServerEvent e)
        {
            if (e.Name == UDPServerEvent.Names.CLIENT_CONNECTED.ToString())
            {
                Debug.Log("New client connected <" + e.UDPpeer.Address + ":" + e.UDPpeer.Port + ">  ID: " + e.UDPpeer.ID);
            }
            else if (e.Name == UDPServerEvent.Names.CLIENT_TIMED_OUT.ToString())
            {
                Debug.Log("Client disconnected <" + e.UDPpeer.Address + ":" + e.UDPpeer.Port + ">  ID: " + e.UDPpeer.ID);
            }
            else if (e.Name == UDPServerEvent.Names.CLIENT_SENT_DATA.ToString())
            {
                Vector3 vec = e.UDPdataInfo.Data.ToObject<Vector3>();
                vec.Set(1, 2, 3);
                Debug.Log("Received from <" + e.UDPpeer.Address + ":" + e.UDPpeer.Port + "> : " + vec + GameObject.FindGameObjectWithTag("MainCamera"));
            }
            else
            {
                //Debug.Log(e.Name);
            }
        }

        public void OnUDPClientEvent(UDPClientEvent e)
        {
            if (e.Name == UDPClientEvent.Names.CONNECTED_TO_SERVER.ToString())
            {
                Debug.Log("Connected to server <" + e.UDPpeer.Address + ":" + e.UDPpeer.Port + ">");
                UDPClient udps = e.Target as UDPClient;
                udps.SendToServer("myChannelName", new Vector3());
            }
            else if (e.Name == UDPClientEvent.Names.CONNECTION_FAILED.ToString())
            {
                Debug.Log("Connection failed to server");
            }
            else if (e.Name == UDPClientEvent.Names.SERVER_SENT_DATA.ToString())
            {
                Debug.Log("Received data from server: " + e.UDPdataInfo.Data);
            }
            else if (e.Name == UDPClientEvent.Names.SERVER_TIMED_OUT.ToString())
            {
                Debug.Log("Connection to server timed out");
            }
            else
            {
                //Debug.Log(e.Name);
            }
        }

        public void OnUDPManagerEvent(UDPManagerEvent e)
        {

            if (e.Name == UDPManagerEvent.Names.BOUND.ToString())
            {
                if (e.Target.GetType() == typeof(UDPManager))
                {
                    Debug.Log("Bound on port " + ((UDPManager)e.Target).BoundPort);
                }
                else if (e.Target.GetType() == typeof(UDPServer))
                {
                    Debug.Log("Bound on port " + ((UDPServer)e.Target).BoundPort);
                }
                else
                {
                    Debug.Log("Bound on port " + ((UDPClient)e.Target).BoundPort);
                }

            }
            else if (e.Name == UDPManagerEvent.Names.DATA_RECEIVED.ToString())
            {
                Debug.Log("Received :" + e.UDPdataInfo.Data);
            }
            else
            {
                Debug.Log(e.Name);
            }
        }

        private void OnDestroy()
        {
            
        }
    }
}