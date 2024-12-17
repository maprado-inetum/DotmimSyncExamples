using Dotmim.Sync.Enumerations;
using Dotmim.Sync;
using Dotmim.Sync.SqlServer;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Linq;
using System;

internal class Program
{
    private const string SCOPE_NAME = "OnDemandJobsSync";

    private static async Task Main(string[] args)
    {
        try
        {
            await ProvisionAsync();
            Console.WriteLine($"[INF] Provision '{SCOPE_NAME}' succesfully");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERR] An error occurred: {ex}");
            Console.ResetColor();
        }
    }

    private static async Task ProvisionAsync()
    {
        // Server side
        SqlSyncChangeTrackingProvider configurationServerProvider = new("Data Source=.;Database=ServerDatabase;Integrated Security=true;MultipleActiveResultSets=True;Connection Timeout=300;Persist Security Info=true;TrustServerCertificate=true")
        {
            UseBulkOperations = true,
            SupportsMultipleActiveResultSets = true,
            BulkBatchMaxLinesCount = 50,
            IsolationLevel = IsolationLevel.ReadUncommitted // Uncommited isolation in the server side
        };

        // Client side
        SqlSyncChangeTrackingProvider configurationClientProvider = new("Data Source=.;Database=ClientDatabase;Integrated Security=true;MultipleActiveResultSets=True;Connection Timeout=300;Persist Security Info=true;TrustServerCertificate=true")
        {
            UseBulkOperations = true,
            SupportsMultipleActiveResultSets = true,
            BulkBatchMaxLinesCount = 50,
            IsolationLevel = IsolationLevel.ReadCommitted
        };

        string assemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        SyncOptions syncOptions = new()
        {
            BatchSize = 512,
            BatchDirectory = Path.Combine(assemblyDirectoryName, "Batches"),
            SnapshotsDirectory = Path.Combine(assemblyDirectoryName, "Snapshots"),
            CleanFolder = true,
            CleanMetadatas = false,
            UseVerboseErrors = true,
            ConflictResolutionPolicy = ConflictResolutionPolicy.ServerWins,
            DisableConstraintsOnApplyChanges = false,
            ScopeInfoTableName = "__sync_info",
            DbCommandTimeout = 300,
            ProgressLevel = SyncProgressLevel.Sql,
            TransactionMode = TransactionMode.PerBatch,
            ErrorResolutionPolicy = ErrorResolution.RetryOnNextSync
        };

        // Create a server orchestrator
        RemoteOrchestrator serverOrchestrator = new(configurationServerProvider, syncOptions);

        List<ScopeInfo> serverScopesInfoList = await serverOrchestrator.GetAllScopeInfosAsync();

        // Create a local orchestrator used to provision everything locally
        LocalOrchestrator clientOrchestrator = new(configurationClientProvider, syncOptions);

        List<ScopeInfo> clientScopesInfoList = await clientOrchestrator.GetAllScopeInfosAsync();

        // If no scope exists, provision server and client scope
        if (!serverScopesInfoList.Any(si => si.Name == SCOPE_NAME)
            ||
            !clientScopesInfoList.Any(si => si.Name == SCOPE_NAME)
            )
        {
            string tableName = $"js.OnDemandJobs";

            // Tables involved in the sync process:
            var tables = new string[]
            {
                    tableName
            };

            SyncSetup syncSetup = new(tables);

            // Filters
            SetupFilter onDemandJobsFilter = new(tableName);

            onDemandJobsFilter.AddCustomWhere(
                $"[base].[AssetAlias] = @InstallationAlias " +
                "OR " +
                "[side].[sync_row_is_tombstone] = 1");

            onDemandJobsFilter.AddParameter("InstallationAlias", DbType.String);

            syncSetup.Filters.Add(onDemandJobsFilter);

            syncSetup.Tables[tableName].SyncDirection = SyncDirection.Bidirectional;

            // Provision everything needed by the setup
            ScopeInfo sScopeInfo = await serverOrchestrator.ProvisionAsync(SCOPE_NAME, syncSetup, overwrite: true);

            await clientOrchestrator.ProvisionAsync(sScopeInfo, overwrite: true);
        }
    }
}