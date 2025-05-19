using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UDP_ESP32_NumberSender : MonoBehaviour
{
    UdpClient client;
    [SerializeField] string esp32_IP = "10.102.128.182"; // <-- Replace with your actual ESP32 IP
    int port = 4210;
    int counter = 0;
    bool running = true;

    async void Start()
    {
        client = new UdpClient();
        await SendLoop();
    }

    async Task SendLoop()
    {
        while (running)
        {
            string message = counter.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            client.Send(bytes, bytes.Length, esp32IP, port);
            Debug.Log("Sent: " + message);
            counter++;
            await Task.Delay(500); // Wait 500 ms
        }
    }

    private void OnApplicationQuit()
    {
        running = false;
        client?.Close();
    }
}
