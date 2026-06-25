using System;
using Unity.Netcode;
using UnityEngine;

public class RobotCommandGateway : NetworkBehaviour
{
    [Tooltip("Match this order to ScoutModeManager.spots and UI spot selection.")]
    public SpotMode[] spots;

    [Tooltip("Allow clients to send robot commands before robot ownership is implemented.")]
    public bool allowUnclaimedControl = true;

    private NetworkVariable<int> redSpotOwner = new(-1);
    private NetworkVariable<int> blueSpotOwner = new(-1);

    public bool TryGetSpotIndex(SpotMode spot, out int spotIndex)
    {
        spotIndex = -1;
        if (spot == null || spots == null)
            return false;

        for (int i = 0; i < spots.Length; i++)
        {
            if (spots[i] == spot)
            {
                spotIndex = i;
                return true;
            }
        }

        return false;
    }

    public void RequestDrive(SpotMode spot, Vector2 direction)
    {
        if (TryGetSpotIndex(spot, out int spotIndex))
            RequestDrive(spotIndex, direction);
    }

    public void RequestDrive(int spotIndex, Vector2 direction)
    {
        if (IsServer)
            ExecuteDrive(spotIndex, direction, NetworkManager.Singleton.LocalClientId);
        else
            DriveServerRpc(spotIndex, direction);
    }

    public void RequestRotate(SpotMode spot, float direction)
    {
        if (TryGetSpotIndex(spot, out int spotIndex))
            RequestRotate(spotIndex, direction);
    }

    public void RequestRotate(int spotIndex, float direction)
    {
        if (IsServer)
            ExecuteRotate(spotIndex, direction, NetworkManager.Singleton.LocalClientId);
        else
            RotateServerRpc(spotIndex, direction);
    }

    public void RequestSetHeight(SpotMode spot, float height)
    {
        if (TryGetSpotIndex(spot, out int spotIndex))
            RequestSetHeight(spotIndex, height);
    }

    public void RequestSetHeight(int spotIndex, float height)
    {
        if (IsServer)
            ExecuteSetHeight(spotIndex, height, NetworkManager.Singleton.LocalClientId);
        else
            SetHeightServerRpc(spotIndex, height);
    }

    public void RequestAdjustHeight(SpotMode spot, float deltaHeight)
    {
        if (TryGetSpotIndex(spot, out int spotIndex))
            RequestAdjustHeight(spotIndex, deltaHeight);
    }

    public void RequestAdjustHeight(int spotIndex, float deltaHeight)
    {
        if (IsServer)
            ExecuteAdjustHeight(spotIndex, deltaHeight, NetworkManager.Singleton.LocalClientId);
        else
            AdjustHeightServerRpc(spotIndex, deltaHeight);
    }

    public void RequestSetGripperWorldPose(SpotMode spot, Vector3 position, Quaternion rotation)
    {
        if (TryGetSpotIndex(spot, out int spotIndex))
            RequestSetGripperWorldPose(spotIndex, position, rotation);
    }

    public void RequestSetGripperWorldPose(int spotIndex, Vector3 position, Quaternion rotation)
    {
        if (IsServer)
            ExecuteSetGripperWorldPose(spotIndex, position, rotation, NetworkManager.Singleton.LocalClientId);
        else
            SetGripperWorldPoseServerRpc(spotIndex, position, rotation);
    }

    public void RequestSetGripperOpen(SpotMode spot, bool isOpen)
    {
        if (TryGetSpotIndex(spot, out int spotIndex))
            RequestSetGripperOpen(spotIndex, isOpen);
    }

    public void RequestSetGripperOpen(int spotIndex, bool isOpen)
    {
        if (IsServer)
            ExecuteSetGripperOpen(spotIndex, isOpen, NetworkManager.Singleton.LocalClientId);
        else
            SetGripperOpenServerRpc(spotIndex, isOpen);
    }

    public void RequestStowArm(SpotMode spot)
    {
        if (TryGetSpotIndex(spot, out int spotIndex))
            RequestStowArm(spotIndex);
    }

    public void RequestStowArm(int spotIndex)
    {
        if (IsServer)
            ExecuteStowArm(spotIndex, NetworkManager.Singleton.LocalClientId);
        else
            StowArmServerRpc(spotIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DriveServerRpc(int spotIndex, Vector2 direction, ServerRpcParams rpcParams = default)
    {
        ExecuteDrive(spotIndex, direction, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RotateServerRpc(int spotIndex, float direction, ServerRpcParams rpcParams = default)
    {
        ExecuteRotate(spotIndex, direction, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHeightServerRpc(int spotIndex, float height, ServerRpcParams rpcParams = default)
    {
        ExecuteSetHeight(spotIndex, height, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AdjustHeightServerRpc(int spotIndex, float deltaHeight, ServerRpcParams rpcParams = default)
    {
        ExecuteAdjustHeight(spotIndex, deltaHeight, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGripperWorldPoseServerRpc(int spotIndex, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        ExecuteSetGripperWorldPose(spotIndex, position, rotation, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGripperOpenServerRpc(int spotIndex, bool isOpen, ServerRpcParams rpcParams = default)
    {
        ExecuteSetGripperOpen(spotIndex, isOpen, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StowArmServerRpc(int spotIndex, ServerRpcParams rpcParams = default)
    {
        ExecuteStowArm(spotIndex, rpcParams.Receive.SenderClientId);
    }

    private void ExecuteDrive(int spotIndex, Vector2 direction, ulong senderClientId)
    {
        if (!CanClientControlSpot(senderClientId, spotIndex) || !TryGetSpot(spotIndex, out SpotMode spot))
            return;

        spot.Drive(direction);
    }

    private void ExecuteRotate(int spotIndex, float direction, ulong senderClientId)
    {
        if (!CanClientControlSpot(senderClientId, spotIndex) || !TryGetSpot(spotIndex, out SpotMode spot))
            return;

        spot.Rotate(direction);
    }

    private void ExecuteSetHeight(int spotIndex, float height, ulong senderClientId)
    {
        if (!CanClientControlSpot(senderClientId, spotIndex) || !TryGetSpot(spotIndex, out SpotMode spot))
            return;

        spot.SetHeight(height);
    }

    private void ExecuteAdjustHeight(int spotIndex, float deltaHeight, ulong senderClientId)
    {
        if (!CanClientControlSpot(senderClientId, spotIndex) || !TryGetSpot(spotIndex, out SpotMode spot))
            return;

        spot.AdjustHeight(deltaHeight);
    }

    private void ExecuteSetGripperWorldPose(int spotIndex, Vector3 position, Quaternion rotation, ulong senderClientId)
    {
        if (!CanClientControlSpot(senderClientId, spotIndex) || !TryGetSpot(spotIndex, out SpotMode spot))
            return;

        spot.SetGripperWorldPose(position, rotation);
    }

    private void ExecuteSetGripperOpen(int spotIndex, bool isOpen, ulong senderClientId)
    {
        if (!CanClientControlSpot(senderClientId, spotIndex) || !TryGetSpot(spotIndex, out SpotMode spot))
            return;

        spot.SetGripperOpen(isOpen);
    }

    private void ExecuteStowArm(int spotIndex, ulong senderClientId)
    {
        if (!CanClientControlSpot(senderClientId, spotIndex) || !TryGetSpot(spotIndex, out SpotMode spot))
            return;

        spot.StowArm();
    }

    private bool TryGetSpot(int spotIndex, out SpotMode spot)
    {
        spot = null;
        if (spots == null || spotIndex < 0 || spotIndex >= spots.Length)
            return false;

        spot = spots[spotIndex];
        return spot != null;
    }

    private bool CanClientControlSpot(ulong clientId, int spotIndex)
    {
        if (!TryGetSpot(spotIndex, out _))
            return false;

        int owner = GetOwnerForSpot(spotIndex);
        if (owner < 0)
            return allowUnclaimedControl;

        return owner == unchecked((int)clientId);
    }

    private int GetOwnerForSpot(int spotIndex)
    {
        if (spotIndex == 0)
            return redSpotOwner.Value;
        if (spotIndex == 1)
            return blueSpotOwner.Value;

        return -1;
    }

    public void SetLocalSpotOwner(int spotIndex)
    {
        if (IsServer)
            SetSpotOwner(spotIndex, NetworkManager.Singleton.LocalClientId);
        else
            SetSpotOwnerServerRpc(spotIndex);
    }

    public void ClearLocalSpotOwner(int spotIndex)
    {
        if (IsServer)
            ClearSpotOwner(spotIndex, NetworkManager.Singleton.LocalClientId);
        else
            ClearSpotOwnerServerRpc(spotIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetSpotOwnerServerRpc(int spotIndex, ServerRpcParams rpcParams = default)
    {
        SetSpotOwner(spotIndex, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClearSpotOwnerServerRpc(int spotIndex, ServerRpcParams rpcParams = default)
    {
        ClearSpotOwner(spotIndex, rpcParams.Receive.SenderClientId);
    }

    private void SetSpotOwner(int spotIndex, ulong clientId)
    {
        if (!TryGetSpot(spotIndex, out _))
            return;

        int currentOwner = GetOwnerForSpot(spotIndex);
        if (currentOwner >= 0 && currentOwner != unchecked((int)clientId))
            return;

        SetOwnerForSpot(spotIndex, unchecked((int)clientId));
    }

    private void ClearSpotOwner(int spotIndex, ulong clientId)
    {
        if (!TryGetSpot(spotIndex, out _))
            return;

        int currentOwner = GetOwnerForSpot(spotIndex);
        if (currentOwner == unchecked((int)clientId) || clientId == NetworkManager.ServerClientId)
            SetOwnerForSpot(spotIndex, -1);
    }

    private void SetOwnerForSpot(int spotIndex, int ownerClientId)
    {
        if (spotIndex == 0)
            redSpotOwner.Value = ownerClientId;
        else if (spotIndex == 1)
            blueSpotOwner.Value = ownerClientId;
    }
}
