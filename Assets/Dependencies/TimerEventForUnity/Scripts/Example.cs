using kevincastejon;
using UnityEngine;
using Timer = kevincastejon.unity.Timer;

namespace TimerEventExample
{
    public class Example : MonoBehaviour
    {
        public Timer Timer;
        // Start is called before the first frame update
        void Start()
        {

        }

        public void OnTime(TimerEvent e)
        {
            Debug.Log(e.Name + " event has been dispatched by "+e.Target);
        }
        public void OnComplete(TimerEvent e)
        {
            Debug.Log(e.Name + " event has been dispatched by " + e.Target);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}