using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text;
using NAudio.CoreAudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System.Threading;

public static class Programs
{
    public static void Start()
    {
        new Thread(() =>
        {
            try
            {
                string filePath = "buttons.json";
                int port = 5001;

                HttpListener listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Prefixes.Add($"http://000.000.0.00:{port}/");
                listener.Start();

                Console.WriteLine($"The server is working: http://localhost:{port}/");

                while (true)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var response = context.Response;

                    Console.WriteLine("Request path: " + request.Url.AbsolutePath);

                    try
                    {
                        string path = request.Url.AbsolutePath;

                        if ((path == "/" || path.Equals("/command", StringComparison.OrdinalIgnoreCase)) &&
                            !string.IsNullOrEmpty(request.QueryString["command"]))
                        {
                            string command = request.QueryString["command"];
                            Console.WriteLine($"Incoming command: {command}");

                            string result = ExecuteCommand(command);
                            SendTextResponse(response, result, 200);
                        }
                        else if (path.Equals("/buttons.json", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!File.Exists(filePath))
                            {
                                SendTextResponse(response, "buttons.json the file was not found.", 404);
                            }
                            else
                            {
                                byte[] buffer = File.ReadAllBytes(filePath);
                                response.ContentType = "application/json";
                                response.ContentLength64 = buffer.Length;
                                response.OutputStream.Write(buffer, 0, buffer.Length);
                                response.OutputStream.Flush();
                            }
                        }
                        else
                        {
                            SendTextResponse(response, "Invalid request.", 400);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        SendTextResponse(response, "Server error", 500);
                    }
                    finally
                    {
                        response.OutputStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("The server could not be started: " + ex.Message);
            }

        }).Start();
    }

    private static void SendTextResponse(HttpListenerResponse response, string message, int statusCode)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.StatusCode = statusCode;
        response.ContentType = "text/plain";
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Flush();
    }

    private static string ExecuteCommand(string command)
    {
        switch (command.ToLower())
        {
            case "volumeup":
                var up = new CoreAudioController().DefaultPlaybackDevice;
                up.Volume = Math.Min(up.Volume + 5, 100);
                return "Volume Up.";

            case "volumedown":
                var down = new CoreAudioController().DefaultPlaybackDevice;
                down.Volume = Math.Max(down.Volume - 5, 0);
                return "Volume Down.";

            case "closeapp":
                RunNirCmd("killprocess notepad.exe");
                return "Close Up.";

            default:
                return "Unknown command.";
        }
    }

    private static void RunNirCmd(string args)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "nircmd.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
    }
}


