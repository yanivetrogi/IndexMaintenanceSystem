using SqlServerIndexMaintenanceSystem.Data;
using SqlServerIndexMaintenanceSystem.Services;
using SqlServerIndexMaintenanceSystem.Models.Ims;
using Index = SqlServerIndexMaintenanceSystem.Models.Ims.Index;

namespace SqlServerIndexMaintenanceSystem.Workers;

public class Rescheduler : BackgroundService
{
    private readonly ILogger<Rescheduler> _logger;
    private readonly IConfiguration _configuration;
    private readonly ImsConnectionFactory _imsDbConectionFactory;
    private readonly SynchronizationService _syncService;
    private readonly int _sleepTimeSeconds;

    public Rescheduler(ILogger<Rescheduler> logger,
        IConfiguration configuration,
        ImsConnectionFactory imsDbConectionFactory,
        SynchronizationService syncService)
    {
        _logger = logger;
        _configuration = configuration;
        _imsDbConectionFactory = imsDbConectionFactory;
        _syncService = syncService;

        _sleepTimeSeconds = _configuration.GetValue("ExecutionIntervalSeconds", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!await _syncService.WaitUntilMigrationFinished())
        {
            _logger.LogError("Terminating rescheduler due to migration failure");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            Task[] tasks = {
                RescheduleOutdatedNextChecks(cancellationToken)
                    .ContinueWith(async task => {
                        await task;
                        _syncService.MarkInitialReschedulingAsFinished();
                    }),
                Task.Delay(_sleepTimeSeconds * 1000, cancellationToken)
            };

            Task.WaitAll(tasks, cancellationToken);
        }
    }

    private async Task RescheduleOutdatedNextChecks(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Rescheduling outdated checks");
        try
        {
            using var dbConnection = _imsDbConectionFactory();
            dbConnection.Open();
            var obsoleteChecks = await dbConnection.GetObsoleteChecksAsync(_sleepTimeSeconds);

            foreach (var (server, database, index, schedule, plannedExecution) in obsoleteChecks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                var nextCheck = await dbConnection.PlanNextCheckAsync(schedule, server, database, index);

                LogPlannedCheck(server, database, index, schedule, nextCheck);
            }

            dbConnection.Close();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while rescheduling outdated checks");
        }
    }

    private void LogPlannedCheck(Server server, Database? database, Index? index, Schedule? schedule, NextCheck? plannedCheck)
    {
        var scheduleFailed = plannedCheck == null;
        var schedulePlanned = plannedCheck?.NextExecutionDateTime != null;

        var action = scheduleFailed
            ? "failed"
            : (schedulePlanned
                ? $"is planned for {plannedCheck!.NextExecutionDateTime:G}"
                : "will never happen");

        var target = $"{server}";

        if (database?.DatabaseId != null)
        {
            target = $"{database} of {target}";
            if (index?.IndexId != null)
            {
                target = $"{index} of {target}";
            }
        }

        var reason = schedule != null
            ? $"schedule [{schedule.ScheduleId}]\"{schedule.Name}\""
            : "run_immediately flag";

        var text = $"Next check for {target} {action} using {reason}";

        if (scheduleFailed)
        {
            _logger.LogError(text);
        }
        else if (schedulePlanned)
        {
            _logger.LogTrace(text);
        }
        else
        {
            _logger.LogWarning(text);
        }
    }
}