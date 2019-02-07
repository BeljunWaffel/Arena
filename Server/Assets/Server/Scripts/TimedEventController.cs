using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Server.Scripts
{
    class TimedEventController : MonoBehaviour
    {
        public List<TimedEvent> TimedEvents;
        private AgentListener _agentListener;
        private List<TimedEvent> _eventsToRemove;

        private void Awake()
        {
            TimedEvents = new List<TimedEvent>();
            _eventsToRemove = new List<TimedEvent>();
            _agentListener = GetComponent<AgentListener>();
        }

        private void Update()
        {
            foreach (var timedEvent in TimedEvents)
            {
                timedEvent.Timer += Time.deltaTime;
                if (timedEvent.Timer > timedEvent.TimeToReach)
                {
                    Debug.Log($"Event {timedEvent.MessageType} sent event after {timedEvent.Timer} seconds");
                    _agentListener.SendEventToOtherClients(timedEvent.MessageType, timedEvent.Message, timedEvent.IgnoreId);
                    _eventsToRemove.Add(timedEvent);
                }
            }

            TimedEvents = TimedEvents.Except(_eventsToRemove).ToList();
        }        
    }

    public class TimedEvent
    {
        public float TimeToReach;
        public short MessageType;
        public MessageBase Message;
        public string IgnoreId;

        public float Timer = 0f;

        public TimedEvent(float timeToReach, short messageType, MessageBase message, string ignoreId = null)
        {
            TimeToReach = timeToReach;
            MessageType = messageType;
            Message = message;
            IgnoreId = ignoreId;
        }
    }
}
