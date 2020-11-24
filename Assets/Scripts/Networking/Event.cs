using System;

namespace Networking
{
    public enum Event
    {
        Join,
        JoinBroadcast,
        Input,
        Snapshot
    }

    public static class EventSerializer
    {
        private static readonly int EventMaxValue = Enum.GetValues(typeof(Event)).Length - 1;
    
        public static void SerializeIntoBuffer(BitBuffer buffer, Event eventType)
        {
            buffer.PutBits((int) eventType, 0, EventMaxValue);
        }
    
        public static Event DeserializeFromBuffer(BitBuffer buffer)
        {
            return (Event) buffer.GetBits(0, EventMaxValue);
        }
    }
}