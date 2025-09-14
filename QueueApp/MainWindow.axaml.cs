using Avalonia.Controls;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;
using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Net;
using System.Text;
using System.Threading;

namespace QueueApp;

public partial class MainWindow : Window
{
    private LibVLC? libVLC;
    private MediaPlayer? mediaPlayer;
    private bool isVideoViewReady = false;
    private DispatcherTimer? clockTimer;

    // Queue data
    private string currentQueue1 = "A001";
    private string currentQueue2 = "B005";
    private bool teller1Active = true;
    private bool teller2Active = true;

    // HTTP server
    private HttpListener? httpListener;
    private CancellationTokenSource? cts;

    public MainWindow()
    {
        InitializeComponent();
        InitializeClock();
        UpdateQueueDisplay();

        // Jalankan web server kecil di background
        StartHttpServer();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await InitializeVideoPlayer();
    }

    private async Task InitializeVideoPlayer()
    {
        try
        {
            Core.Initialize();
            await Task.Delay(500);

            libVLC = new LibVLC(new string[]
            {
                "--intf=dummy",
                "--no-osd",
                "--no-video-title-show",
                "--quiet"
            });

            mediaPlayer = new MediaPlayer(libVLC);
            videoView.MediaPlayer = mediaPlayer;
            isVideoViewReady = true;

            var videoPath = System.IO.Path.GetFullPath("sample1.mp4");
            if (System.IO.File.Exists(videoPath))
            {
                var media = new Media(libVLC, videoPath, FromType.FromPath);
                mediaPlayer.Play(media);
                Console.WriteLine($"Playing video: {videoPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing video player: {ex.Message}");
        }
    }

    private void InitializeClock()
    {
        clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        clockTimer.Tick += (sender, e) => UpdateClock();
        clockTimer.Start();
        UpdateClock();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        if (currentTime != null)
            currentTime.Text = now.ToString("HH:mm:ss");
        if (currentDate != null)
            currentDate.Text = now.ToString("dddd, dd MMMM yyyy");
    }

    private void UpdateQueueDisplay()
    {
        if (currentQueueNumber1 != null)
            currentQueueNumber1.Text = currentQueue1;
        if (currentQueueNumber2 != null)
            currentQueueNumber2.Text = currentQueue2;

        if (status1 != null)
            status1.Text = teller1Active ? "● AKTIF" : "● OFFLINE";
        if (status2 != null)
            status2.Text = teller2Active ? "● AKTIF" : "● OFFLINE";
    }

    public void UpdateQueue(int tellerNumber, string queueNumber, bool isActive = true)
    {
        Dispatcher.UIThread.Post(() =>
        {
            switch (tellerNumber)
            {
                case 1:
                    currentQueue1 = queueNumber;
                    teller1Active = isActive;
                    break;
                case 2:
                    currentQueue2 = queueNumber;
                    teller2Active = isActive;
                    break;
            }
            UpdateQueueDisplay();
        });
    }

    // ================================
    // MINI HTTP SERVER
    // ================================
    private void StartHttpServer()
    {
        cts = new CancellationTokenSource();
        httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:5003/");
        httpListener.Start();

        Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await httpListener.GetContextAsync();
                    HandleRequest(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HTTP Error: {ex.Message}");
                }
            }
        }, cts.Token);
    }

    private void HandleRequest(HttpListenerContext context)
    {
        string responseText = "OK";
        var requestUrl = context.Request.Url?.AbsolutePath.ToLower();

        if (requestUrl == "/play")
        {
            if (mediaPlayer != null && !mediaPlayer.IsPlaying)
            {
                mediaPlayer.Play();
                responseText = "Video resumed";
            }
        }
        else if (requestUrl == "/pause")
        {
            if (mediaPlayer != null && mediaPlayer.IsPlaying)
            {
                mediaPlayer.Pause();
                responseText = "Video paused";
            }
        }
        else if (requestUrl == "/closewindow")
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.Close();
            });
            responseText = "Window closed";
        }
        else if (requestUrl?.StartsWith("/addqueue") == true)
        {
            // Format: /addqueue?teller=1&number=A010
            var query = context.Request.QueryString;
            int teller = int.TryParse(query["teller"], out var t) ? t : 1;
            string number = query["number"] ?? "X000";
            UpdateQueue(teller, number, true);
            responseText = $"Queue updated teller {teller} -> {number}";
        }
        else
        {
            responseText = "Invalid endpoint. Use /play /pause /closewindow /addqueue?teller=1&number=A010";
        }

        var buffer = Encoding.UTF8.GetBytes(responseText);
        context.Response.ContentType = "text/plain";
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            clockTimer?.Stop();
            mediaPlayer?.Stop();
            mediaPlayer?.Dispose();
            libVLC?.Dispose();

            // Stop server
            cts?.Cancel();
            httpListener?.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
        base.OnClosed(e);
    }
}
