using MessageTypes = RosSharp.RosBridgeClient.MessageTypes;

public class ThreadedStowArm : ThreadedUnityPublisher<MessageTypes.Std.Bool>
{
    private MessageTypes.Std.Bool message;

    protected override void Start()
    {
        base.Start();
        message = new();
    }

    // Call this function from other scripts to stow the arm
    // WARNING: if recently sent an incomplete command, it will not Stow
    // eg: if you close the gripper with an object in the gripper (gripper doesn't close all of the way)
    // the arm won't stow because it's still trying to finish closing the gripper
    public void Stow()
    {
        message.data = true;
        LoopPublish(message);
    }
}