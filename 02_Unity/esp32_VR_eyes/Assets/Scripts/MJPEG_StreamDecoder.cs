/*
 * I needed a simple MJPEG Stream Decoder and I couldn't find one that worked for me.
 * 
 * It reads a response stream and when there's a new frame it updates the render texture. 
 * That's it. No authenication or options.
 * It's something stupid simple for reading a video stream from an equally stupid simple Arduino. 
 * 
 * I fixed most of the large memory leaks, but there's at least one small one left.
 */

using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MJPEGS_StreamDecoder : MonoBehaviour
{
    [SerializeField] bool tryOnStart = false;
    [SerializeField] string defaultStreamURL = "http://127.0.0.1/stream";

    [SerializeField] RenderTexture renderTexture;

    float RETRY_DELAY = 5f;
    int MAX_RETRIES = 3;
    int retryCount = 0;

    byte[] nextFrame = null;

    Thread worker;
    int threadID = 0;

    static System.Random randu;
    List<BufferedStream> trackedBuffers = new List<BufferedStream>();

    void Start()
    {
        randu = new System.Random(Random.Range(0, 65536));
        Debug.Log("STREAM: MJPEGStreamDecoder started.");
        if (tryOnStart)
        {
            Debug.Log("STREAM: Auto-starting stream with default URL.");
            StartStream(defaultStreamURL);
        }
    }

    private void Update()
    {
        if (nextFrame != null)
        {
            Debug.Log("STREAM: New frame received, sending to texture.");
            SendFrame(nextFrame);
            nextFrame = null;
        }
    }

    private void OnDestroy()
    {
        Debug.Log("STREAM: OnDestroy - Cleaning up buffers.");
        foreach (var b in trackedBuffers)
        {
            if (b != null)
                b.Close();
        }
    }

    public void StartStream(string url)
    {
        Debug.Log($"STREAM: Starting MJPEG stream from URL: {url}");
        retryCount = 0;
        StopAllCoroutines();

        foreach (var b in trackedBuffers)
        {
            Debug.Log("STREAM: Closing tracked buffer before restart.");
            b.Close();
        }

        worker = new Thread(() => ReadMJPEGStreamWorker(threadID = randu.Next(65536), url));
        worker.Start();
        Debug.Log("STREAM: Worker thread started.");
    }

    void ReadMJPEGStreamWorker(int id, string url)
    {
        Debug.Log($"STREAM: [{id}] Connecting to stream: {url}");

        var webRequest = WebRequest.Create(url);
        webRequest.Method = "GET";
        List<byte> frameBuffer = new List<byte>();

        int lastByte = 0x00;
        bool addToBuffer = false;

        BufferedStream buffer = null;
        try
        {
            Stream stream = webRequest.GetResponse().GetResponseStream();
            buffer = new BufferedStream(stream);
            trackedBuffers.Add(buffer);
            Debug.Log("STREAM: Stream successfully opened.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"STREAM: Error opening stream: {ex}");
        }

        int newByte;
        while (buffer != null)
        {
            if (threadID != id)
            {
                Debug.Log("STREAM: Thread ID mismatch. Exiting thread.");
                return;
            }

            if (!buffer.CanRead)
            {
                Debug.LogError("STREAM: Cannot read from buffer. Exiting.");
                break;
            }

            newByte = -1;

            try
            {
                newByte = buffer.ReadByte();
            }
            catch
            {
                Debug.LogWarning("STREAM: Exception during buffer read. Restarting.");
                break;
            }

            if (newByte < 0)
            {
                Debug.LogWarning("STREAM: End of stream or read error.");
                continue;
            }

            if (addToBuffer)
                frameBuffer.Add((byte)newByte);

            if (lastByte == 0xFF)
            {
                if (!addToBuffer)
                {
                    if (IsStartOfImage(newByte))
                    {
                        Debug.Log("STREAM: Start of image detected.");
                        addToBuffer = true;
                        frameBuffer.Add((byte)lastByte);
                        frameBuffer.Add((byte)newByte);
                    }
                }
                else
                {
                    if (newByte == 0xD9)
                    {
                        Debug.Log($"STREAM: End of image detected. Frame size: {frameBuffer.Count} bytes.");
                        frameBuffer.Add((byte)newByte);
                        addToBuffer = false;
                        nextFrame = frameBuffer.ToArray();
                        frameBuffer.Clear();
                    }
                }
            }

            lastByte = newByte;
        }

        if (retryCount < MAX_RETRIES)
        {
            retryCount++;
            Debug.Log($"STREAM: [{id}] Retrying connection... Attempt {retryCount}");
            foreach (var b in trackedBuffers)
            {
                Debug.Log("STREAM: Disposing tracked buffer on retry.");
                b.Dispose();
            }
            trackedBuffers.Clear();
            worker = new Thread(() => ReadMJPEGStreamWorker(threadID = randu.Next(65536), url));
            worker.Start();
        }
        else
        {
            Debug.LogError("STREAM: Max retry limit reached. Giving up.");
        }
    }

    bool IsStartOfImage(int command)
    {
        switch (command)
        {
            case 0x8D:
                Debug.Log("STREAM: Command SOI (0x8D)");
                return true;
            case 0xC0:
                Debug.Log("STREAM: Command SOF0 (0xC0)");
                return true;
            case 0xC2:
                Debug.Log("STREAM: Command SOF2 (0xC2)");
                return true;
            case 0xC4:
                Debug.Log("STREAM: Command DHT (0xC4)");
                break;
            case 0xD8:
                Debug.Log("STREAM: Command DQT (0xD8)");
                return true;
            case 0xDD:
                Debug.Log("STREAM: Command DRI (0xDD)");
                break;
            case 0xDA:
                Debug.Log("STREAM: Command SOS (0xDA)");
                break;
            case 0xFE:
                Debug.Log("STREAM: Command COM (0xFE)");
                break;
            case 0xD9:
                Debug.Log("STREAM: Command EOI (0xD9)");
                break;
        }
        return false;
    }

    void SendFrame(byte[] bytes)
    {
        Debug.Log($"STREAM: Sending frame to texture. Size: {bytes.Length} bytes.");
        Texture2D texture2D = new Texture2D(2, 2);
        texture2D.LoadImage(bytes);

        if (texture2D.width == 2)
        {
            Debug.LogWarning("STREAM: Frame load failed. Texture size is 2x2.");
            return;
        }

        Graphics.Blit(texture2D, renderTexture);
        Destroy(texture2D);
        Debug.Log("STREAM: Frame blitted and texture destroyed.");
    }
}
