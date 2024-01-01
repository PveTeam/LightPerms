using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using NLog;
using Torch.API.Managers;
using Torch.Managers;

namespace Maintenance.Managers;

public class MaintenanceScheduleManager(string storagePath) : IManager
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    [Manager.Dependency]
    private readonly MaintenanceManager _maintenanceManager = null!;
    
    [Manager.Dependency]
    private readonly ConfigManager _configManager = null!;
    
    [Manager.Dependency]
    private readonly LanguageManager _languageManager = null!;
    
    [Manager.Dependency]
    private readonly IChatManagerServer _chatManager = null!;

    private MaintenanceSchedule _currentSchedule = MaintenanceSchedule.Default;
    
    private readonly FileInfo _scheduleFile = new(Path.Combine(storagePath, ".schedule"));

    public MaintenanceSchedule CurrentSchedule
    {
        get => _currentSchedule;
        private set
        {
            if (value != _currentSchedule)
            {
                using var file = _scheduleFile.Create();
                JsonSerializer.Serialize(file, value);
            }
            
            _currentSchedule = value;
        }
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public void Attach()
    {
        Scheduler();
        
        if (!_scheduleFile.Exists) return;
        
        using (var file = _scheduleFile.OpenRead())
            _currentSchedule = JsonSerializer.Deserialize<MaintenanceSchedule>(file)!;
        
        _scheduleFile.Delete();

        if (_currentSchedule is not { StartTime: null, EndTime: not null }) return;
        
        if (!_configManager.Configuration.GetValue<bool>(ConfigKeys.ContinueEndTimerAfterRestart))
            CurrentSchedule = MaintenanceSchedule.Default;
        
        _maintenanceManager.MaintenanceEnabled = true;
    }

    private async void Scheduler()
    {
        var token = _cancellationTokenSource.Token;

        try
        {
            await SchedulerLoop(token);
        }
        catch (OperationCanceledException)
        {
            Log.Info("Maintenance scheduler loop was cancelled");
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Exception in maintenance scheduler loop. Shutting down the scheduler.");
        }
    }

    private async Task SchedulerLoop(CancellationToken token)
    {
        var timerSecondsSection = _configManager.Configuration.GetSection(ConfigKeys.TimerBroadcastForSeconds);
        var timerSeconds = timerSecondsSection.GetChildren().Select(b => b.Get<int>()).ToArray();

        using var disposable = timerSecondsSection.GetReloadToken()
            .RegisterChangeCallback(_ => timerSeconds = timerSecondsSection.GetChildren().Select(b => b.Get<int>()).ToArray(), null);
        
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(2000, token);

            switch (_maintenanceManager.MaintenanceEnabled)
            {
                case false when _currentSchedule.StartTime.HasValue:
                {
                    if (timerSeconds.Any(b => Math.Abs(_currentSchedule.Time.TotalSeconds - b) <= 1))
                    {
                        _chatManager.SendMessageAsOther("Maintenance", 
                            Format(_currentSchedule.EndTime.HasValue
                                ? LangKeys.ScheduleTimerBroadcast
                                : LangKeys.StartTimerBroadcast));
                    }

                    if (_currentSchedule.Time.TotalSeconds <= 1)
                    {
                        _chatManager.SendMessageAsOther("Maintenance", Format(LangKeys.MaintenanceActivated));
                        _maintenanceManager.MaintenanceEnabled = true;
                        CurrentSchedule = CurrentSchedule with { StartTime = null };
                    }

                    break;
                }
                case true when _currentSchedule.EndTime.HasValue:
                {
                    if (timerSeconds.Any(b => Math.Abs(_currentSchedule.Duration.TotalSeconds - b) <= 1))
                    {
                        _chatManager.SendMessageAsOther("Maintenance", Format(LangKeys.EndTimerBroadcast));
                    }

                    if (_currentSchedule.Duration.TotalSeconds <= 1)
                    {
                        _chatManager.SendMessageAsOther("Maintenance", Format(LangKeys.MaintenanceDeactivated));
                        _maintenanceManager.MaintenanceEnabled = false;
                        CurrentSchedule = CurrentSchedule with { EndTime = null };
                    }

                    break;
                }
            }
        }
        
        string Format(string key) => _languageManager.Format(key, _currentSchedule);
    }

    public void Detach()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
    
    public void ScheduleTimer(TimerType type, TimeSpan duration)
    {
        if (_maintenanceManager.MaintenanceEnabled && type == TimerType.Start)
            throw new InvalidOperationException("Maintenance is already enabled");
        
        if (type == TimerType.Start)
            CurrentSchedule = CurrentSchedule with { StartTime = DateTimeOffset.Now + duration };
        else
            CurrentSchedule = CurrentSchedule with { EndTime = DateTimeOffset.Now + duration };
    }
    
    public void ScheduleMaintenance(TimeSpan startTime, TimeSpan endTime)
    {
        if (_maintenanceManager.MaintenanceEnabled)
            throw new InvalidOperationException("Maintenance is already enabled");

        var startDateTime = DateTimeOffset.Now + startTime;
        
        CurrentSchedule = new(startDateTime, startDateTime + endTime);
    }

    public void CancelTimer(TimerType? type)
    {
        CurrentSchedule = type switch
        {
            TimerType.Start when !_currentSchedule.StartTime.HasValue => throw new InvalidOperationException(
                "No start timer running"),
            TimerType.Start => MaintenanceSchedule.Default,
            TimerType.End when !_currentSchedule.EndTime.HasValue => throw new InvalidOperationException(
                "No end timer running"),
            TimerType.End => CurrentSchedule with { EndTime = null },
            _ => MaintenanceSchedule.Default
        };
    }
}

public record MaintenanceSchedule(DateTimeOffset? StartTime, DateTimeOffset? EndTime)
{
    public static MaintenanceSchedule Default => new(null, null);

    [JsonIgnore]
    public TimeSpan Time => Round((StartTime ?? throw new InvalidOperationException("No start timer running"))
                                  - DateTimeOffset.Now);
    [JsonIgnore]
    public TimeSpan Duration => Round((EndTime ?? throw new InvalidOperationException("No end timer running"))
                                      - (StartTime ?? DateTimeOffset.Now));
    
    private static TimeSpan Round(TimeSpan timeSpan) => TimeSpan.FromSeconds(Math.Round(timeSpan.TotalSeconds, 0));
}

public enum TimerType : byte
{
    Start,
    End
}