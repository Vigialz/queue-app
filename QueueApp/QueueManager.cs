using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace QueueApp;

public class QueueItem
{
    public string QueueNumber { get; set; } = "";
    public DateTime CreatedTime { get; set; }
    public QueueStatus Status { get; set; }
    public int TellerNumber { get; set; }
}

public enum QueueStatus
{
    Waiting,
    BeingServed,
    Completed,
    Skipped
}

public class QueueManager
{
    private List<QueueItem> queueItems = new();
    private Dictionary<int, QueueItem?> activeTellers = new();
    private MainWindow? mainWindow;
    private Timer? autoAdvanceTimer;
    
    // Queue prefixes
    private readonly Dictionary<int, string> tellerPrefixes = new()
    {
        { 1, "A" },
        { 2, "B" }
    };
    
    private readonly Dictionary<int, int> currentNumbers = new()
    {
        { 1, 1 },
        { 2, 1 }
    };
    
    public event EventHandler<QueueUpdateEventArgs>? QueueUpdated;
    
    public QueueManager(MainWindow window)
    {
        mainWindow = window;
        InitializeTellers();
        StartAutoAdvance();
    }
    
    private void InitializeTellers()
    {
        // Initialize tellers
        activeTellers[1] = null;
        activeTellers[2] = null;
        
        // Add some initial queue items for demo
        AddQueueItem(1);
        AddQueueItem(1);
        AddQueueItem(2);
        AddQueueItem(2);
        AddQueueItem(1);
        
        // Serve first items
        ServeNextQueue(1);
        ServeNextQueue(2);
    }
    
    public void AddQueueItem(int tellerNumber)
    {
        if (!tellerPrefixes.ContainsKey(tellerNumber))
            return;
            
        var queueNumber = $"{tellerPrefixes[tellerNumber]}{currentNumbers[tellerNumber]:000}";
        currentNumbers[tellerNumber]++;
        
        var queueItem = new QueueItem
        {
            QueueNumber = queueNumber,
            CreatedTime = DateTime.Now,
            Status = QueueStatus.Waiting,
            TellerNumber = tellerNumber
        };
        
        queueItems.Add(queueItem);
        Console.WriteLine($"Added queue item: {queueNumber} for Teller {tellerNumber}");
    }
    
    public void ServeNextQueue(int tellerNumber)
    {
        // Find next waiting item for this teller
        var nextItem = queueItems.Find(q => 
            q.TellerNumber == tellerNumber && 
            q.Status == QueueStatus.Waiting);
            
        if (nextItem != null)
        {
            // Mark current item as completed
            if (activeTellers[tellerNumber] != null)
            {
                activeTellers[tellerNumber]!.Status = QueueStatus.Completed;
            }
            
            // Serve next item
            nextItem.Status = QueueStatus.BeingServed;
            activeTellers[tellerNumber] = nextItem;
            
            // Update UI
            mainWindow?.UpdateQueue(tellerNumber, nextItem.QueueNumber, true);
            
            Console.WriteLine($"Now serving: {nextItem.QueueNumber} at Teller {tellerNumber}");
            
            // Notify subscribers
            QueueUpdated?.Invoke(this, new QueueUpdateEventArgs 
            { 
                TellerNumber = tellerNumber, 
                QueueNumber = nextItem.QueueNumber 
            });
        }
        else
        {
            // No more items, mark teller as idle
            activeTellers[tellerNumber] = null;
            mainWindow?.UpdateQueue(tellerNumber, "----", false);
            Console.WriteLine($"Teller {tellerNumber} is now idle");
        }
    }
    
    private void StartAutoAdvance()
    {
        autoAdvanceTimer = new Timer(15000); // Auto advance every 15 seconds
        autoAdvanceTimer.Elapsed += (sender, e) =>
        {
            // Randomly advance queues for demo
            var random = new Random();
            var teller = random.Next(1, 3);
            
            // Sometimes add new queue items
            if (random.NextDouble() < 0.3) // 30% chance
            {
                AddQueueItem(random.Next(1, 3));
            }
            
            // Advance queue
            ServeNextQueue(teller);
        };
        autoAdvanceTimer.Start();
    }
    
    public List<QueueItem> GetWaitingQueues(int tellerNumber)
    {
        return queueItems.FindAll(q => 
            q.TellerNumber == tellerNumber && 
            q.Status == QueueStatus.Waiting);
    }
    
    public QueueItem? GetCurrentQueue(int tellerNumber)
    {
        return activeTellers.GetValueOrDefault(tellerNumber);
    }
    
    public void StopAutoAdvance()
    {
        autoAdvanceTimer?.Stop();
        autoAdvanceTimer?.Dispose();
    }
    
    // Manual control methods
    public void CallNextQueue(int tellerNumber)
    {
        ServeNextQueue(tellerNumber);
    }
    
    public void SkipCurrentQueue(int tellerNumber)
    {
        if (activeTellers[tellerNumber] != null)
        {
            activeTellers[tellerNumber]!.Status = QueueStatus.Skipped;
            ServeNextQueue(tellerNumber);
        }
    }
    
    public void CompleteCurrentQueue(int tellerNumber)
    {
        if (activeTellers[tellerNumber] != null)
        {
            activeTellers[tellerNumber]!.Status = QueueStatus.Completed;
            ServeNextQueue(tellerNumber);
        }
    }
}

public class QueueUpdateEventArgs : EventArgs
{
    public int TellerNumber { get; set; }
    public string QueueNumber { get; set; } = "";
}