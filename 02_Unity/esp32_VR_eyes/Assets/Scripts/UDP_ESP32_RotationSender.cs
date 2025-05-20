using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UDP_ESP32_RotationSender : MonoBehaviour
{
    UdpClient client;
    [SerializeField] string esp32IP = "10.102.128.182"; 
    [SerializeField] int port = 4210;
    [SerializeField] Transform headTransform; 

    bool running = true;

    async void Start()
    {
        client = new UdpClient();

        if (headTransform == null)
        {
            Debug.LogError("Head Transform not assigned.");
            enabled = false;
            return;
        }

        await SendLoop();
    }

    async Task SendLoop()
    {
        while (running)
        {
            Vector3 euler = headTransform.rotation.eulerAngles;
            string message = $"RotX:{euler.x:F2},RotY:{euler.y:F2},RotZ:{euler.z:F2}";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            client.Send(bytes, bytes.Length, esp32IP, port);
            Debug.Log("Sent: " + message);

            await Task.Delay(10); //ms
        }
    }

    private void OnApplicationQuit()
    {
        running = false;
        client?.Close();
    }
}
