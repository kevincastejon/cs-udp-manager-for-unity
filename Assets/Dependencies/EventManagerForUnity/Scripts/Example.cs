using kevincastejon.unity;
using UnityEngine;
using Event = kevincastejon.Event;

namespace EventManagerExample
{
    public class Example : MonoBehaviour
    {
        public EventDispatcher Dispatcher;
        private bool Dispatched = false;
        // Start is called before the first frame update
        void Start()
        {

        }

        public void MyCallback(Event e)
        {
            Debug.Log(e.Name + " event has been dispatched by" + e.Target);
        }

        // Update is called once per frame
        void Update()
        {
            if (!Dispatched)
            {
                Dispatcher.DispatchEvent(new Event("someEventName"));
                Dispatched = true;
            }
        }
    }
}