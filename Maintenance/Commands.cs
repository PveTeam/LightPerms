using Maintenance.Managers;
using Torch.API.Managers;
using Torch.Commands;

namespace Maintenance;

[Category("maintenance")]
public class Commands : CommandModule
{
    private static readonly TimeSpan MaxTime = TimeSpan.FromDays(28);
    
    private MaintenanceManager MaintenanceManager =>
        Context.Torch.CurrentSession.Managers.GetManager<MaintenanceManager>();
    
    private MaintenanceScheduleManager MaintenanceScheduler =>
        Context.Torch.CurrentSession.Managers.GetManager<MaintenanceScheduleManager>();
    
    private LanguageManager LanguageManager =>
        Context.Torch.Managers.GetManager<LanguageManager>();
    
    [Command("on", "Set the status of the maintenance mode to enabled.")]
    public void On()
    {
        if (MaintenanceManager.MaintenanceEnabled)
        {
            Context.Respond(LanguageManager[LangKeys.AlreadyEnabled]);
            return;
        }
        
        MaintenanceManager.MaintenanceEnabled = true;
        
        Context.Respond(LanguageManager[LangKeys.MaintenanceActivated]);
    }
    
    [Command("off", "Set the status of the maintenance mode to disabled.")]
    public void Off()
    {
        if (!MaintenanceManager.MaintenanceEnabled)
        {
            Context.Respond(LanguageManager[LangKeys.AlreadyDisabled]);
            return;
        }
        
        MaintenanceManager.MaintenanceEnabled = false;
        MaintenanceScheduler.CancelTimer(null);
        
        Context.Respond(LanguageManager[LangKeys.MaintenanceDeactivated]);
    }

    [Command("starttimer", "Will enable maintenance mode after the time is up.")]
    public void StartTimer(string time)
    {
        var startTime = double.TryParse(time, out var seconds) ? TimeSpan.FromSeconds(seconds) : TimeSpan.Parse(time);

        if (startTime > MaxTime)
        {
            Context.Respond(LanguageManager[LangKeys.TimerTooLong]);
            return;
        }

        if (MaintenanceManager.MaintenanceEnabled)
        {
            Context.Respond(LanguageManager[LangKeys.AlreadyEnabled]);
            return;
        }
        
        if (MaintenanceScheduler.CurrentSchedule.StartTime.HasValue)
        {
            Context.Respond(LanguageManager[LangKeys.TimerAlreadyRunning]);
            return;
        }
        
        MaintenanceScheduler.ScheduleTimer(TimerType.Start, startTime);
        
        Context.Respond(LanguageManager.Format(LangKeys.StartTimerStarted, MaintenanceScheduler.CurrentSchedule));
    }

    [Command("endtimer",
        "Will enable maintenance mode for the given time in minutes. After the time is up, it'll be disabled again.")]
    public void EndTimer(string duration)
    {
        var endTime = double.TryParse(duration, out var seconds) ? TimeSpan.FromSeconds(seconds) : TimeSpan.Parse(duration);
        
        if (endTime > MaxTime)
        {
            Context.Respond(LanguageManager[LangKeys.TimerTooLong]);
            return;
        }
        
        if (MaintenanceScheduler.CurrentSchedule.EndTime.HasValue)
        {
            Context.Respond(LanguageManager[LangKeys.TimerAlreadyRunning]);
            return;
        }

        MaintenanceManager.MaintenanceEnabled = true;
        MaintenanceScheduler.ScheduleTimer(TimerType.End, endTime);
        
        Context.Respond(LanguageManager.Format(LangKeys.EndTimerStarted, MaintenanceScheduler.CurrentSchedule));
    }

    [Command("schedule", "Will enable maintenance mode after the given time, then disable it according to the second parameter.")]
    public void Schedule(string time, string duration)
    {
        var startTime = double.TryParse(time, out var startSeconds) ? TimeSpan.FromSeconds(startSeconds) : TimeSpan.Parse(time);
        var endTime = double.TryParse(duration, out var endSeconds) ? TimeSpan.FromSeconds(endSeconds) : TimeSpan.Parse(duration);
        
        if (startTime > MaxTime || endTime > MaxTime)
        {
            Context.Respond(LanguageManager[LangKeys.TimerTooLong]);
            return;
        }

        if (MaintenanceScheduler.CurrentSchedule != MaintenanceSchedule.Default)
        {
            Context.Respond(LanguageManager[LangKeys.TimerAlreadyRunning]);
            return;
        }

        if (MaintenanceManager.MaintenanceEnabled)
        {
            Context.Respond(LanguageManager[LangKeys.AlreadyEnabled]);
            return;
        }
        
        MaintenanceScheduler.ScheduleMaintenance(startTime, endTime);
        
        Context.Respond(LanguageManager.Format(LangKeys.ScheduleTimerBroadcast, MaintenanceScheduler.CurrentSchedule));
    }

    [Command("aborttimer", "Cancels a running start-/endtimer")]
    public void AbortTimer()
    {
        if (MaintenanceScheduler.CurrentSchedule == MaintenanceSchedule.Default)
        {
            Context.Respond(LanguageManager[LangKeys.TimerNotRunning]);
            return;
        }
        
        MaintenanceScheduler.CancelTimer(null);
        
        Context.Respond(LanguageManager[LangKeys.TimerCancelled]);
    }
}