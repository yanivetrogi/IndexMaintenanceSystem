using Microsoft.Data.SqlClient;
using IndexMaintenanceSystem.Services;
using IndexMaintenanceSystem.Logger;
using IndexMaintenanceSystem.Workers;
using IndexMaintenanceSystem.ConnectionPool;
using System.Data;
using IndexMaintenanceSystem.Web;
using Dapper;

namespace IndexMaintenanceSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Windows Service
            builder.Host.UseWindowsService();

            // Configure Configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

            builder.Services.Configure<GlobalConfig>(builder.Configuration);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            
            builder.Services.AddScoped<DashboardService>();

            // Existing Service Registration
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            var mainDatabaseConnectionString = builder.Configuration.GetConnectionString("MainDatabase");
            var clientDatabaseTemplate = builder.Configuration.GetConnectionString("ClientDatabaseTemplate");
            
            if (mainDatabaseConnectionString == null) throw new ArgumentNullException("MainDatabase connection string is not set");
            if (clientDatabaseTemplate == null) throw new ArgumentNullException("ClientDatabaseTemplate connection string is not set");

            var credentialsFile = builder.Configuration.GetValue<string?>("CredentialsFilePath");
            if (!string.IsNullOrEmpty(credentialsFile))
            {
                if (!Path.IsPathRooted(credentialsFile))
                {
                    credentialsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, credentialsFile);
                }
            }

            builder.Services.AddSingleton<ImsConnectionFactory>(() => new SqlConnection(mainDatabaseConnectionString));
            builder.Services.AddSingleton(services => 
                new SqlConnectionPool<IDbConnection>((server, database, poolSize, integratedSecurity) => 
                    new SqlConnection(
                        new SqlConnectionStringFactory(
                            clientDatabaseTemplate,
                            new CredentialsManager.CredentialsStorage(credentialsFile ?? default),
                            services.GetService<ILogger<SqlConnectionStringFactory>>()!
                        ).CreateConnectionString(server, database, poolSize, integratedSecurity)
                    )
                )
            );

            builder.Services.AddSingleton<SynchronizationService>();
            builder.Services.AddSingleton<ServerPreparationService>();

            builder.Services.AddHostedService<ImsMigrator>();
            builder.Services.AddHostedService<Rescheduler>();
            builder.Services.AddHostedService<ServerProcessor>();
            builder.Services.AddHostedService<DatabaseDefragger>();
            builder.Services.AddHostedService<AlwaysonReverter>();

            var interval = builder.Configuration.GetValue("ExecutionIntervalSeconds", 30);
            builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(interval));

            // Configure Logging
            builder.Logging.ClearProviders();
            var loggingConfiguration = builder.Configuration.GetSection("Logging");
            builder.Logging.AddConfiguration(loggingConfiguration);
            builder.Logging.AddProvider(new FileLoggerProvider(loggingConfiguration));
            builder.Logging.AddConsole();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
            }

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            var logger = app.Services.GetService<ILogger<Program>>();
            var config = new GlobalConfig();
            app.Configuration.Bind(config);
            logger!.LogInformation(config!.ToString());

            var version = EventLogHelper.GetVersion();
            var startupMsg = $"Index Maintenance System started: Version {version} (Web Dashboard Enabled)";
            logger!.LogInformation(startupMsg);
            if (OperatingSystem.IsWindows()) EventLogHelper.LogInformation(startupMsg);

            try
            {
                app.Run();
                if (OperatingSystem.IsWindows()) EventLogHelper.LogInformation("Index Maintenance System stopped.");
            }
            catch (Exception ex)
            {
                if (OperatingSystem.IsWindows()) EventLogHelper.LogError("Index Maintenance System encountered a critical error and will shut down.", ex);
                logger!.LogCritical(ex, "Critical Exception in Program.Run");
                throw;
            }
        }
    }
}

