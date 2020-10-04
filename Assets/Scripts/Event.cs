using System;

public enum Event
{
    Join,
    JoinBroadcast,
    Input
}

public static class EventSerializer
{
    public static void SerializeIntoBuffer(BitBuffer buffer, Event eventType)
    {
        buffer.PutBits((int) eventType, 0, Enum.GetValues(typeof(Event)).Length);
    }
    
    public static Event DeserializeFromBuffer(BitBuffer buffer)
    {
        return (Event) buffer.GetBits(0, 3);
    }
}