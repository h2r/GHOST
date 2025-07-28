using MessageTypes = RosSharp.RosBridgeClient.MessageTypes;

public class ThreadedKillMotor : ThreadedUnityPublisher<MessageTypes.Std.Bool>
{
    private MessageTypes.Std.Bool message;

    protected override void Start()
    {
        base.Start();
        message = new();
    }

    public void KillSpot()
    {
        message.data = true;
        KillSpot(message);
    }
}