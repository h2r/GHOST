using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
public class ServerController : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinServerButton;
    [SerializeField] private Button startServerButton;
    [SerializeField] private TMP_Text serverButtonText;
    [SerializeField] private TMP_Text joinCodeText;
    public NetworkTransport transport;
    // Update is called once per frame
    private void Start()
    {
        startServerButton.onClick.AddListener(OnStartServerRequested);
        joinServerButton.onClick.AddListener(JoinServer);
    }
    private async void JoinServer()
    {
        string joinCode=joinCodeInput.text;

        ConnectClientJoinCode(joinCode);
    }
    private async void ConnectClientJoinCode(string joinCode)
    {
        if (joinCode == null)
        {
            Debug.Log("no join code inputted");
            return;
        }
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        }
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        // 4. Configure UnityTransport with the fetched Relay data
        // Use "dtls" for secure encryption or "udp" for unencrypted transport
        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
        UnityTransport transport1 = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport1.SetRelayServerData(relayServerData);
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client successfully connected to the Relay host.");
        }
        else
        {
            Debug.LogError("Failed to start the NetworkManager client.");
        }

    }
    private async void OnStartServerRequested()
    {
        joinCodeText.SetText("Starting Server...");
        string joinCode=await StartRelayServer();
        joinCodeText.SetText($"Join Code: {joinCode}");
        startServerButton.onClick.RemoveAllListeners();
        startServerButton.onClick.AddListener(KillServer);
        serverButtonText.SetText("Kill Server");

    }
    private void KillServer()
    {
        NetworkManager.Singleton.Shutdown();
        startServerButton.onClick.RemoveAllListeners();
        startServerButton.onClick.AddListener(OnStartServerRequested);
        joinCodeText.SetText("Join Code: ");
        serverButtonText.SetText("Start Server");
    }
    private async Task<string> StartRelayServer()
    {

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        }
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);

        // 4. Retrieve the unique Join Code for clients to connect with
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Define connection parameters ("udp" protocol is typically standard)
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "udp");
        transport.SetRelayServerData(relayServerData);

        // 6. Start the Game Session as a Host (Acts as both server and local client)
        NetworkManager.Singleton.StartServer();
        return (joinCode);
    }
}
