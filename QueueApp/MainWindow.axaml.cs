using Avalonia.Controls;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;
using System;
using System.Threading.Tasks;
using Avalonia.Threading;

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
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeVideoPlayer();
        InitializeClock();
        UpdateQueueDisplay();
    }
    
    private async void InitializeVideoPlayer()
    {
        try
        {
            // Initialize LibVLC Core
            Core.Initialize();
            
            // Wait for window to be fully loaded
            await Task.Delay(500);
            
            // Create LibVLC with minimal options
            libVLC = new LibVLC(new string[] 
            {
                "--intf=dummy",
                "--no-osd",
                "--no-video-title-show",
                "--quiet"
            });
            
            mediaPlayer = new MediaPlayer(libVLC);
            
            // Event handling
            mediaPlayer.ESAdded += (sender, e) =>
            {
                Console.WriteLine($"ES Added: {e.Type} - {e.Id}");
                if (e.Type == TrackType.Video)
                {
                    Console.WriteLine("Video track detected, ensuring embedded playback");
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (videoView.MediaPlayer != mediaPlayer)
                        {
                            videoView.MediaPlayer = mediaPlayer;
                            Console.WriteLine("MediaPlayer reassigned to VideoView");
                        }
                    });
                }
            };
            
            mediaPlayer.Playing += (sender, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (videoPlaceholder != null)
                        videoPlaceholder.IsVisible = false;
                });
            };
            
            mediaPlayer.Stopped += (sender, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (videoPlaceholder != null)
                        videoPlaceholder.IsVisible = true;
                });
            };
            
            // Set MediaPlayer to VideoView
            videoView.MediaPlayer = mediaPlayer;
            isVideoViewReady = true;
            
            // Try to load and play video
            var videoPath = System.IO.Path.GetFullPath("sample.mp4");
            if (System.IO.File.Exists(videoPath))
            {
                var media = new Media(libVLC, videoPath, FromType.FromPath);
                mediaPlayer.Play(media);
                Console.WriteLine($"Playing video: {videoPath}");
            }
            else
            {
                Console.WriteLine($"Video file not found: {videoPath}");
                // Keep placeholder visible
                if (videoPlaceholder != null)
                    videoPlaceholder.IsVisible = true;
            }
            
            Console.WriteLine("Video player initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing video player: {ex.Message}");
            // Keep placeholder visible on error
            if (videoPlaceholder != null)
                videoPlaceholder.IsVisible = true;
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
        UpdateClock(); // Update immediately
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
        // Update queue numbers
        if (currentQueueNumber1 != null)
            currentQueueNumber1.Text = currentQueue1;
        if (currentQueueNumber2 != null)
            currentQueueNumber2.Text = currentQueue2;
        
        // Update status
        if (status1 != null)
            status1.Text = teller1Active ? "● AKTIF" : "● OFFLINE";
        if (status2 != null)
            status2.Text = teller2Active ? "● AKTIF" : "● OFFLINE";
    }
    
    // Method to update queue from external source
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
    
    // Method to change video source
    public void ChangeVideo(string videoPath)
    {
        if (mediaPlayer != null && libVLC != null && System.IO.File.Exists(videoPath))
        {
            try
            {
                var media = new Media(libVLC, videoPath, FromType.FromPath);
                mediaPlayer.Play(media);
                Console.WriteLine($"Changed video to: {videoPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing video: {ex.Message}");
            }
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        try
        {
            clockTimer?.Stop();
            mediaPlayer?.Stop();
            mediaPlayer?.Dispose();
            libVLC?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
        base.OnClosed(e);
    }
}