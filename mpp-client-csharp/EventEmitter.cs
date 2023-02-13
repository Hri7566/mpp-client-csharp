using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mpp_client_csharp
{
    public class EventEmitter
    {
        private Dictionary<string, List<Action<dynamic>>> events;

        public EventEmitter()
        {
            events = new Dictionary<string, List<Action<dynamic>>>();
        }

        public void On(string evt, Action<dynamic> callback)
        {
            if (!events.ContainsKey(evt))
            {
                events[evt] = new List<Action<dynamic>>();
            }

            events[evt].Add(callback);
        }

        public void Off(string evt, Action<dynamic> callback)
        {
            if (!events.ContainsKey(evt)) return;
            events[evt].Remove(callback);
        }

        public void Emit(string evt, params object[] args)
        {
            if (!events.ContainsKey(evt)) return;
            foreach (Action<dynamic> callback in events[evt])
            {
                callback(args);
            }
        }
    }
}
