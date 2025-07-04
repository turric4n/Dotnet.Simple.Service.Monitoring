using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.UI.Dtos;
using Simple.Service.Monitoring.UI.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simple.Service.Monitoring.UI.Models;

namespace Simple.Service.Monitoring.UI.Repositories.SQL
{
    public class SqlMonitoringDataRepository : IMonitoringDataRepository, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlMonitoringDataRepository> _logger;
        private bool _initialized = false;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        public SqlMonitoringDataRepository(
            IOptions<MonitoringUiOptions> options,
            ILogger<SqlMonitoringDataRepository> logger)
        {
            _connectionString = options.Value.SqlConnectionString
                ?? throw new ArgumentNullException(nameof(options.Value.SqlConnectionString),
                    "SQL connection string must be provided in options.");

            _logger = logger;

            _logger.LogInformation("SQL repository initialized using {DatabaseType}");
        }

        private async Task EnsureDatabaseInitializedAsync()
        {
            if (_initialized) return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_initialized) return;

                _logger.LogInformation("Initializing SQL database schema for health monitoring");

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HealthChecks')
                BEGIN
                    CREATE TABLE HealthChecks (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(255) NOT NULL,
                        MachineName NVARCHAR(255) NOT NULL,
                        ServiceType NVARCHAR(255) NULL,
                        Status INT NOT NULL,
                        Duration NVARCHAR(255) NULL,
                        Description NVARCHAR(MAX) NULL,
                        CheckError NVARCHAR(MAX) NULL,
                        LastUpdated DATETIME2 NOT NULL,
                        CreationDate DATETIME2 NOT NULL
                    );

                    CREATE INDEX IX_HealthChecks_Name ON HealthChecks(Name);
                    CREATE INDEX IX_HealthChecks_MachineName ON HealthChecks(MachineName);
                    CREATE INDEX IX_HealthChecks_LastUpdated ON HealthChecks(LastUpdated);
                    CREATE INDEX IX_HealthChecks_NameMachine ON HealthChecks(Name, MachineName);
                    CREATE INDEX IX_HealthChecks_Status ON HealthChecks(Status);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HealthCheckTimeSeriesPoints')
                BEGIN
                    CREATE TABLE HealthCheckTimeSeriesPoints (
                        Id NVARCHAR(50) PRIMARY KEY,
                        Name NVARCHAR(255) NOT NULL,
                        MachineName NVARCHAR(255) NOT NULL,
                        ServiceKey NVARCHAR(511) NOT NULL,
                        Timestamp DATETIME2 NOT NULL,
                        Status INT NOT NULL,
                        StatusReason NVARCHAR(MAX) NULL
                    );

                    CREATE INDEX IX_TimeSeriesPoints_Name ON HealthCheckTimeSeriesPoints(Name);
                    CREATE INDEX IX_TimeSeriesPoints_MachineName ON HealthCheckTimeSeriesPoints(MachineName);
                    CREATE INDEX IX_TimeSeriesPoints_Timestamp ON HealthCheckTimeSeriesPoints(Timestamp);
                    CREATE INDEX IX_TimeSeriesPoints_ServiceKey ON HealthCheckTimeSeriesPoints(ServiceKey);
                    CREATE INDEX IX_TimeSeriesPoints_NameMachine ON HealthCheckTimeSeriesPoints(Name, MachineName);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HealthCheckTimeRanges')
                BEGIN
                    CREATE TABLE HealthCheckTimeRanges (
                        Id NVARCHAR(50) PRIMARY KEY,
                        Name NVARCHAR(255) NOT NULL,
                        MachineName NVARCHAR(255) NOT NULL,
                        StartTime DATETIME2 NOT NULL,
                        EndTime DATETIME2 NULL,
                        UpdateTime DATETIME2 NOT NULL,
                        Status INT NOT NULL,
                        StatusReason NVARCHAR(MAX) NULL,
                        ServiceKey NVARCHAR(511) NOT NULL
                    );

                    CREATE INDEX IX_TimeRanges_Name ON HealthCheckTimeRanges(Name);
                    CREATE INDEX IX_TimeRanges_MachineName ON HealthCheckTimeRanges(MachineName);
                    CREATE INDEX IX_TimeRanges_StartTime ON HealthCheckTimeRanges(StartTime);
                    CREATE INDEX IX_TimeRanges_EndTime ON HealthCheckTimeRanges(EndTime);
                    CREATE INDEX IX_TimeRanges_UpdateTime ON HealthCheckTimeRanges(UpdateTime);
                    CREATE INDEX IX_TimeRanges_ServiceKey ON HealthCheckTimeRanges(ServiceKey);
                    CREATE INDEX IX_TimeRanges_Status ON HealthCheckTimeRanges(Status);
                    CREATE INDEX IX_TimeRanges_NameMachine ON HealthCheckTimeRanges(Name, MachineName);
                END
                ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'UpdateTime' AND Object_ID = Object_ID(N'HealthCheckTimeRanges'))
                BEGIN
                    -- Add UpdateTime column to existing table if it doesn't exist
                    ALTER TABLE HealthCheckTimeRanges ADD UpdateTime DATETIME2 NOT NULL DEFAULT GETDATE();
                END";

                await connection.ExecuteAsync(sql);
                _initialized = true;
                _logger.LogInformation("SQL database schema initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SQL database schema");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task<HealthCheckData> GetLatestHealthCheckAsync(string name, string machineName)
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT TOP 1 * 
                    FROM HealthChecks 
                    WHERE Name = @Name AND MachineName = @MachineName
                    ORDER BY LastUpdated DESC";

                var result = await connection.QueryFirstOrDefaultAsync<HealthCheckDataDto>(sql, 
                    new { Name = name, MachineName = machineName });

                if (result == null)
                    return null;

                return MapDtoToHealthCheckData(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest health check for {Name} on {MachineName}", 
                    name, machineName);
                throw;
            }
        }

        public async Task AddHealthCheckDataAsync(HealthCheckData healthCheckData)
        {
            if (healthCheckData == null)
                throw new ArgumentNullException(nameof(healthCheckData));

            try
            {
                await EnsureDatabaseInitializedAsync();

                // Ensure CreationDate is set
                if (healthCheckData.CreationDate == default)
                {
                    healthCheckData.CreationDate = DateTime.Now;
                }

                // Get the latest health check for this name and machine name
                var latestCheck = await GetLatestHealthCheckAsync(healthCheckData.Name, healthCheckData.MachineName);

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // If the latest check exists and has the same status, just update LastUpdated
                if (latestCheck != null && latestCheck.Status == healthCheckData.Status)
                {
                    var updateSql = @"
                        UPDATE HealthChecks
                        SET LastUpdated = @LastUpdated, 
                            Duration = @Duration,
                            Description = @Description,
                            CheckError = @CheckError
                        WHERE Name = @Name AND MachineName = @MachineName AND Status = @Status
                        AND LastUpdated = (
                            SELECT MAX(LastUpdated) 
                            FROM HealthChecks 
                            WHERE Name = @Name AND MachineName = @MachineName
                        )";

                    await connection.ExecuteAsync(updateSql, new
                    {
                        healthCheckData.Name,
                        healthCheckData.MachineName,
                        Status = (int)healthCheckData.Status,
                        healthCheckData.Duration,
                        healthCheckData.Description,
                        healthCheckData.CheckError,
                        LastUpdated = DateTime.Now
                    });

                    _logger.LogDebug("Updated LastUpdated for health check {Name} on {MachineName} with status {Status}", 
                        healthCheckData.Name, healthCheckData.MachineName, healthCheckData.Status);
                }
                // Otherwise, insert a new record
                else
                {
                    var insertSql = @"
                        INSERT INTO HealthChecks (Name, MachineName, ServiceType, Status, Duration, Description, 
                                               CheckError, LastUpdated, CreationDate)
                        VALUES (@Name, @MachineName, @ServiceType, @Status, @Duration, @Description,
                                @CheckError, @LastUpdated, @CreationDate)";

                    await connection.ExecuteAsync(insertSql, new
                    {
                        healthCheckData.Name,
                        healthCheckData.MachineName,
                        healthCheckData.ServiceType,
                        Status = (int)healthCheckData.Status,
                        healthCheckData.Duration,
                        healthCheckData.Description,
                        healthCheckData.CheckError,
                        healthCheckData.LastUpdated,
                        healthCheckData.CreationDate
                    });

                    _logger.LogDebug("Added new health check for {Name} on {MachineName} with status {Status}", 
                        healthCheckData.Name, healthCheckData.MachineName, healthCheckData.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding health check data for {Name}", healthCheckData.Name);
                throw;
            }
        }

        public async Task AddHealthChecksDataAsync(IEnumerable<HealthCheckData> healthChecksData)
        {
            if (healthChecksData == null)
                throw new ArgumentNullException(nameof(healthChecksData));

            try
            {
                await EnsureDatabaseInitializedAsync();
                var dataList = healthChecksData.ToList();
                if (!dataList.Any())
                    return;

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Use a transaction for bulk operations
                await using var transaction = await connection.BeginTransactionAsync();
                
                try
                {
                    var staleThreshold = TimeSpan.FromMinutes(1); // Define stale threshold
                    var now = DateTime.Now;
                    
                    foreach (var data in dataList)
                    {
                        // Ensure CreationDate is set
                        if (data.CreationDate == default)
                        {
                            data.CreationDate = DateTime.Now;
                        }

                        // Get latest health check for this service with optimized query
                        var latestCheckSql = @"
                            SELECT TOP 1 * 
                            FROM HealthChecks 
                            WHERE Name = @Name AND MachineName = @MachineName
                            ORDER BY LastUpdated DESC";

                        var latestCheck = await connection.QueryFirstOrDefaultAsync<HealthCheckDataDto>(
                            latestCheckSql, 
                            new { data.Name, data.MachineName },
                            transaction);

                        // First, check for open time ranges
                        var timeRangeSql = @"
                            SELECT TOP 1 *
                            FROM HealthCheckTimeRanges
                            WHERE Name = @Name 
                            AND MachineName = @MachineName
                            AND EndTime IS NULL
                            ORDER BY StartTime DESC";
                            
                        var openRange = await connection.QueryFirstOrDefaultAsync<HealthCheckTimeRangeDto>(
                            timeRangeSql, 
                            new { data.Name, data.MachineName },
                            transaction);
                        
                        // Handle time range updates
                        if (openRange != null)
                        {
                            // Calculate if the range is stale
                            bool isStale = (now - openRange.UpdateTime) > staleThreshold;
                            
                            // If status changed or is stale, close the current range
                            if (openRange.Status != data.Status || isStale)
                            {
                                // If stale, we'll close with the last known update time + threshold
                                var closeTime = isStale
                                    ? openRange.UpdateTime.Add(staleThreshold)
                                    : now;
                                
                                var updateSql = @"
                                    UPDATE HealthCheckTimeRanges
                                    SET EndTime = @EndTime,
                                        UpdateTime = @UpdateTime
                                    WHERE Id = @Id";
                                    
                                await connection.ExecuteAsync(updateSql, new
                                {
                                    Id = openRange.Id,
                                    EndTime = closeTime,
                                    UpdateTime = now
                                }, transaction);
                                
                                // If it was stale, create an "Unknown" range between stale time and now
                                if (isStale)
                                {
                                    var unknownInsertSql = @"
                                        INSERT INTO HealthCheckTimeRanges 
                                        (Id, Name, MachineName, StartTime, EndTime, UpdateTime, Status, StatusReason, ServiceKey)
                                        VALUES 
                                        (@Id, @Name, @MachineName, @StartTime, @EndTime, @UpdateTime, @Status, @StatusReason, @ServiceKey)";
                                        
                                    var serviceKey = string.IsNullOrEmpty(data.MachineName) 
                                        ? data.Name 
                                        : $"{data.Name} ({data.MachineName})";
                                        
                                    await connection.ExecuteAsync(unknownInsertSql, new
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        data.Name,
                                        MachineName = data.MachineName ?? string.Empty,
                                        StartTime = closeTime,
                                        EndTime = now,
                                        UpdateTime = now,
                                        Status = (int)HealthStatus.Unknown,
                                        StatusReason = "Status became unknown due to inactivity",
                                        ServiceKey = serviceKey
                                    }, transaction);
                                }
                                
                                // Create a new range with current status
                                var insertSql = @"
                                    INSERT INTO HealthCheckTimeRanges 
                                    (Id, Name, MachineName, StartTime, EndTime, UpdateTime, Status, StatusReason, ServiceKey)
                                    VALUES 
                                    (@Id, @Name, @MachineName, @StartTime, @EndTime, @UpdateTime, @Status, @StatusReason, @ServiceKey)";
                                    
                                var serviceKey2 = string.IsNullOrEmpty(data.MachineName) 
                                    ? data.Name 
                                    : $"{data.Name} ({data.MachineName})";
                                    
                                await connection.ExecuteAsync(insertSql, new
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    data.Name,
                                    MachineName = data.MachineName ?? string.Empty,
                                    StartTime = now,
                                    EndTime = (DateTime?)null,
                                    UpdateTime = now,
                                    Status = (int)data.Status,
                                    StatusReason = data.Description,
                                    ServiceKey = serviceKey2
                                }, transaction);
                            }
                            else
                            {
                                // Status hasn't changed and not stale, just update UpdateTime
                                var updateSql = @"
                                    UPDATE HealthCheckTimeRanges
                                    SET UpdateTime = @UpdateTime
                                    WHERE Id = @Id";
                                    
                                await connection.ExecuteAsync(updateSql, new
                                {
                                    Id = openRange.Id,
                                    UpdateTime = now
                                }, transaction);
                            }
                        }
                        else
                        {
                            // No open range exists, create a new one
                            var insertSql = @"
                                INSERT INTO HealthCheckTimeRanges 
                                (Id, Name, MachineName, StartTime, EndTime, UpdateTime, Status, StatusReason, ServiceKey)
                                VALUES 
                                (@Id, @Name, @MachineName, @StartTime, @EndTime, @UpdateTime, @Status, @StatusReason, @ServiceKey)";
                                
                            var serviceKey = string.IsNullOrEmpty(data.MachineName) 
                                ? data.Name 
                                : $"{data.Name} ({data.MachineName})";
                                
                            await connection.ExecuteAsync(insertSql, new
                            {
                                Id = Guid.NewGuid().ToString(),
                                data.Name,
                                MachineName = data.MachineName ?? string.Empty,
                                StartTime = now,
                                EndTime = (DateTime?)null,
                                UpdateTime = now,
                                Status = (int)data.Status,
                                StatusReason = data.Description,
                                ServiceKey = serviceKey
                            }, transaction);
                        }

                        // Now handle the regular health check data
                        if (latestCheck != null && (HealthStatus)latestCheck.Status == data.Status)
                        {
                            var updateSql = @"
                                UPDATE HealthChecks
                                SET LastUpdated = @LastUpdated, 
                                    Duration = @Duration,
                                    Description = @Description,
                                    CheckError = @CheckError
                                WHERE Id = @Id";

                            await connection.ExecuteAsync(updateSql, new
                            {
                                Id = latestCheck.Id,
                                data.Duration,
                                data.Description,
                                data.CheckError,
                                LastUpdated = DateTime.Now
                            }, transaction);

                            _logger.LogDebug("Updated LastUpdated for health check {Name} on {MachineName} with status {Status}", 
                                data.Name, data.MachineName, data.Status);
                        }
                        else
                        {
                            var insertSql = @"
                                INSERT INTO HealthChecks (Name, MachineName, ServiceType, Status, Duration, Description, 
                                                       CheckError, LastUpdated, CreationDate)
                                VALUES (@Name, @MachineName, @ServiceType, @Status, @Duration, @Description,
                                        @CheckError, @LastUpdated, @CreationDate)";

                            await connection.ExecuteAsync(insertSql, new
                            {
                                data.Name,
                                data.MachineName,
                                data.ServiceType,
                                Status = (int)data.Status,
                                data.Duration,
                                data.Description,
                                data.CheckError,
                                data.LastUpdated,
                                data.CreationDate
                            }, transaction);

                            _logger.LogDebug("Added new health check for {Name} on {MachineName} with status {Status}", 
                                data.Name, data.MachineName, data.Status);
                        }
                    }

                    await transaction.CommitAsync();
                    
                    _logger.LogDebug("Processed {Count} health check data items in bulk", dataList.Count);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in transaction, rolling back");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding health checks data in bulk");
                throw;
            }
        }

        public async Task<List<HealthCheckData>> GetLatestHealthChecksAsync()
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();
                var sql = @"
                    WITH LatestChecks AS (
                        SELECT Name, MachineName, MAX(LastUpdated) as LatestUpdate
                        FROM HealthChecks
                        GROUP BY Name, MachineName
                    )
                    SELECT h.*
                    FROM HealthChecks h
                    INNER JOIN LatestChecks l ON h.Name = l.Name 
                        AND h.MachineName = l.MachineName
                        AND h.LastUpdated = l.LatestUpdate
                    ORDER BY h.Name, h.MachineName";

                var results = await connection.QueryAsync<HealthCheckDataDto>(sql);
                
                // Convert database DTOs to domain objects
                var healthChecks = results.Select(MapDtoToHealthCheckData).ToList();
                
                _logger.LogDebug("Retrieved {Count} latest health checks", healthChecks.Count);
                
                return healthChecks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest health checks");
                throw;
            }
        }

        public async Task<List<HealthCheckData>> GetHealthChecksByDateRangeAsync(DateTime from, DateTime to)
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();

                // Get the latest entries for each Name+MachineName within the date range
                var sql = @"
                    WITH LatestInRange AS (
                        SELECT Name, MachineName, MAX(LastUpdated) as LatestUpdate
                        FROM HealthChecks
                        WHERE LastUpdated BETWEEN @From AND @To
                        GROUP BY Name, MachineName
                    )
                    SELECT h.*
                    FROM HealthChecks h
                    INNER JOIN LatestInRange l ON h.Name = l.Name 
                        AND h.MachineName = l.MachineName
                        AND h.LastUpdated = l.LatestUpdate
                    ORDER BY h.Name, h.MachineName";

                var results = await connection.QueryAsync<HealthCheckDataDto>(sql, new { From = from, To = to });
                
                var healthChecks = results.Select(MapDtoToHealthCheckData).ToList();
                
                _logger.LogDebug("Retrieved {Count} health checks in date range {From} to {To}", 
                    healthChecks.Count, from, to);
                
                return healthChecks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health checks by date range: {From} to {To}", from, to);
                throw;
            }
        }

        public async Task<IEnumerable<IGrouping<(string Name, string MachineName), HealthCheckData>>> GetGroupedHealthChecksAsync()
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();

                // Retrieve all health checks
                var sql = "SELECT * FROM HealthChecks ORDER BY Name, MachineName, LastUpdated DESC";

                var results = await connection.QueryAsync<HealthCheckDataDto>(sql);
                
                // Convert and group the results
                var healthChecks = results.Select(MapDtoToHealthCheckData)
                    .GroupBy(hc => (hc.Name, hc.MachineName))
                    .ToList();
                
                _logger.LogDebug("Retrieved health checks grouped into {Count} groups", healthChecks.Count);
                
                return healthChecks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving grouped health checks");
                throw;
            }
        }

        public async Task<IEnumerable<HealthCheckData>> GetHealthChecksInTimeWindowAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();

                var sql = @"
                    SELECT * 
                    FROM HealthChecks 
                    WHERE LastUpdated BETWEEN @StartTime AND @EndTime
                    ORDER BY Name, MachineName, LastUpdated DESC";

                var results = await connection.QueryAsync<HealthCheckDataDto>(
                    sql, new { StartTime = startTime, EndTime = endTime });
                
                var healthChecks = results.Select(MapDtoToHealthCheckData).ToList();
                
                _logger.LogDebug("Retrieved {Count} health checks in time window {Start} to {End}", 
                    healthChecks.Count, startTime, endTime);
                
                return healthChecks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health checks in time window: {Start} to {End}", 
                    startTime, endTime);
                throw;
            }
        }

        public async Task AddTimeSeriesPointAsync(HealthCheckTimeSeriesPoint point)
        {
            if (point == null)
                throw new ArgumentNullException(nameof(point));

            try
            {
                await EnsureDatabaseInitializedAsync();

                // Generate ID if not provided
                if (string.IsNullOrEmpty(point.Id))
                {
                    point.Id = Guid.NewGuid().ToString();
                }

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO HealthCheckTimeSeriesPoints (Id, Name, MachineName, ServiceKey, Timestamp, Status, StatusReason)
                    VALUES (@Id, @Name, @MachineName, @ServiceKey, @Timestamp, @Status, @StatusReason)";

                await connection.ExecuteAsync(sql, new
                {
                    point.Id,
                    point.Name,
                    point.MachineName,
                    point.ServiceKey,
                    point.Timestamp,
                    Status = (int)point.Status,
                    point.StatusReason
                });

                _logger.LogDebug("Added time series point for {Name} on {MachineName} at {Timestamp}", 
                    point.Name, point.MachineName, point.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding time series point for {Name}", point.Name);
                throw;
            }
        }

        public async Task AddTimeSeriesPointsAsync(IEnumerable<HealthCheckTimeSeriesPoint> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            var pointsList = points.ToList();
            if (!pointsList.Any())
                return;

            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Use a transaction for bulk operations
                await using var transaction = await connection.BeginTransactionAsync();

                var sql = @"
                    INSERT INTO HealthCheckTimeSeriesPoints (Id, Name, MachineName, ServiceKey, Timestamp, Status, StatusReason)
                    VALUES (@Id, @Name, @MachineName, @ServiceKey, @Timestamp, @Status, @StatusReason)";

                foreach (var point in pointsList)
                {
                    // Generate ID if not provided
                    if (string.IsNullOrEmpty(point.Id))
                    {
                        point.Id = Guid.NewGuid().ToString();
                    }

                    await connection.ExecuteAsync(sql, new
                    {
                        point.Id,
                        point.Name,
                        point.MachineName,
                        point.ServiceKey,
                        point.Timestamp,
                        Status = (int)point.Status,
                        point.StatusReason
                    }, transaction);
                }

                await transaction.CommitAsync();
                
                _logger.LogDebug("Added {Count} time series points in bulk", pointsList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding time series points in bulk");
                throw;
            }
        }

        public async Task<List<HealthCheckTimeSeriesPoint>> GetTimeSeriesPointsAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * 
                    FROM HealthCheckTimeSeriesPoints 
                    WHERE Timestamp BETWEEN @StartTime AND @EndTime
                    ORDER BY Name, MachineName, Timestamp";

                var results = await connection.QueryAsync<HealthCheckTimeSeriesPointDto>(
                    sql, new { StartTime = startTime, EndTime = endTime });

                var points = results.Select(MapDtoToTimeSeriesPoint).ToList();
                
                _logger.LogDebug("Retrieved {Count} time series points in time window {Start} to {End}", 
                    points.Count, startTime, endTime);
                
                return points;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time series points in time window: {Start} to {End}", 
                    startTime, endTime);
                throw;
            }
        }

        public async Task<Dictionary<string, List<HealthCheckTimeSeriesPoint>>> GetGroupedTimeSeriesPointsAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * 
                    FROM HealthCheckTimeSeriesPoints 
                    WHERE Timestamp BETWEEN @StartTime AND @EndTime
                    ORDER BY ServiceKey, Timestamp";

                var results = await connection.QueryAsync<HealthCheckTimeSeriesPointDto>(
                    sql, new { StartTime = startTime, EndTime = endTime });

                var points = results.Select(MapDtoToTimeSeriesPoint).ToList();
                
                // Group by ServiceKey
                var groupedPoints = points.GroupBy(p => p.ServiceKey)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                _logger.LogDebug("Retrieved {TotalCount} time series points in {GroupCount} groups", 
                    points.Count, groupedPoints.Count);
                
                return groupedPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving grouped time series points in time window: {Start} to {End}", 
                    startTime, endTime);
                throw;
            }
        }

        public async Task AddHealthCheckStatusChangeAsync(string name, string machineName, DateTime timestamp, HealthStatus status, string statusReason)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // Begin transaction
                await using var transaction = await connection.BeginTransactionAsync();
                
                try
                {
                    // Calculate the service key
                    var serviceKey = string.IsNullOrEmpty(machineName) 
                        ? name 
                        : $"{name} ({machineName})";
                        
                    // First, check for any open time range for this service
                    var checkSql = @"
                        SELECT TOP 1 *
                        FROM HealthCheckTimeRanges
                        WHERE Name = @Name 
                        AND MachineName = @MachineName
                        AND EndTime IS NULL
                        ORDER BY StartTime DESC";
                        
                    var openRange = await connection.QueryFirstOrDefaultAsync<HealthCheckTimeRangeDto>(checkSql, 
                        new { Name = name, MachineName = machineName }, transaction);
                        
                    var now = DateTime.Now;
                        
                    // Close any open time range for this service
                    if (openRange != null)
                    {
                        // If the status is the same, just update UpdateTime
                        if (openRange.Status == status)
                        {
                            var updateSql = @"
                                UPDATE HealthCheckTimeRanges
                                SET UpdateTime = @UpdateTime
                                WHERE Id = @Id";
                                
                            await connection.ExecuteAsync(updateSql, new
                            {
                                Id = openRange.Id,
                                UpdateTime = now
                            }, transaction);
                            
                            _logger.LogDebug("Updated time range for {Name} on {MachineName}, same status: {Status}", 
                                name, machineName, status);
                                
                            // No need to create a new range, just updated the existing one
                            await transaction.CommitAsync();
                            return;
                        }
                        else
                        {
                            // Status changed, close the existing range
                            var updateSql = @"
                                UPDATE HealthCheckTimeRanges
                                SET EndTime = @Timestamp,
                                    UpdateTime = @UpdateTime
                                WHERE Id = @Id";
                                
                            await connection.ExecuteAsync(updateSql, new
                            {
                                Id = openRange.Id,
                                Timestamp = timestamp,
                                UpdateTime = now
                            }, transaction);
                        }
                    }
                    
                    // Create a new time range with the current status
                    var insertSql = @"
                        INSERT INTO HealthCheckTimeRanges (Id, Name, MachineName, StartTime, EndTime, UpdateTime, Status, StatusReason, ServiceKey)
                        VALUES (@Id, @Name, @MachineName, @StartTime, @EndTime, @UpdateTime, @Status, @StatusReason, @ServiceKey)";
                        
                    await connection.ExecuteAsync(insertSql, new
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        MachineName = machineName ?? string.Empty,
                        StartTime = timestamp,
                        EndTime = (DateTime?)null,
                        UpdateTime = now,
                        Status = (int)status,
                        StatusReason = statusReason,
                        ServiceKey = serviceKey
                    }, transaction);
                    
                    // Commit transaction
                    await transaction.CommitAsync();
                    
                    _logger.LogDebug("Added health check status change for {Name} on {MachineName}: {Status}", 
                        name, machineName, status);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding health check status change for {Name}", name);
                throw;
            }
        }

        public async Task<List<HealthCheckTimeRange>> GetHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * 
                    FROM HealthCheckTimeRanges 
                    WHERE (StartTime BETWEEN @StartTime AND @EndTime)
                       OR (EndTime IS NULL OR EndTime BETWEEN @StartTime AND @EndTime)
                       OR (StartTime <= @StartTime AND (EndTime IS NULL OR EndTime >= @EndTime))
                    ORDER BY Name, MachineName, StartTime";

                var results = await connection.QueryAsync<HealthCheckTimeRangeDto>(
                    sql, new { StartTime = startTime, EndTime = endTime });

                var ranges = results.Select(MapDtoToTimeRange).ToList();
                
                _logger.LogDebug("Retrieved {Count} time ranges in time window {Start} to {End}", 
                    ranges.Count, startTime, endTime);
                
                return ranges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time ranges in time window: {Start} to {End}", 
                    startTime, endTime);
                throw;
            }
        }

        public async Task<Dictionary<string, List<HealthCheckTimeRange>>> GetGroupedHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                await EnsureDatabaseInitializedAsync();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT * 
                    FROM HealthCheckTimeRanges 
                    WHERE (StartTime BETWEEN @StartTime AND @EndTime)
                       OR (EndTime IS NULL OR EndTime BETWEEN @StartTime AND @EndTime)
                       OR (StartTime <= @StartTime AND (EndTime IS NULL OR EndTime >= @EndTime))
                    ORDER BY ServiceKey, StartTime";

                var results = await connection.QueryAsync<HealthCheckTimeRangeDto>(
                    sql, new { StartTime = startTime, EndTime = endTime });

                var ranges = results.Select(MapDtoToTimeRange).ToList();
                
                // Group by ServiceKey
                var groupedRanges = ranges.GroupBy(r => r.ServiceKey)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                _logger.LogDebug("Retrieved {TotalCount} time ranges in {GroupCount} groups", 
                    ranges.Count, groupedRanges.Count);
                
                return groupedRanges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving grouped time ranges in time window: {Start} to {End}", 
                    startTime, endTime);
                throw;
            }
        }

        private class HealthCheckTimeSeriesPointDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string MachineName { get; set; }
            public string ServiceKey { get; set; }
            public DateTime Timestamp { get; set; }
            public int Status { get; set; }
            public string StatusReason { get; set; }
        }

        private static HealthCheckTimeSeriesPoint MapDtoToTimeSeriesPoint(HealthCheckTimeSeriesPointDto dto)
        {
            return new HealthCheckTimeSeriesPoint
            {
                Id = dto.Id,
                Name = dto.Name,
                MachineName = dto.MachineName,
                Timestamp = dto.Timestamp,
                Status = (HealthStatus)dto.Status,
                StatusReason = dto.StatusReason,
            };
        }

        private class HealthCheckTimeRangeDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string MachineName { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public DateTime UpdateTime { get; set; }
            public HealthStatus Status { get; set; }
            public string StatusReason { get; set; }
            public string ServiceKey { get; set; }
        }

        private static HealthCheckTimeRange MapDtoToTimeRange(HealthCheckTimeRangeDto dto)
        {
            return new HealthCheckTimeRange
            {
                Id = dto.Id,
                Name = dto.Name,
                MachineName = dto.MachineName,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                UpdateTime = dto.UpdateTime,
                Status = dto.Status,
                StatusReason = dto.StatusReason
            };
        }

        private static HealthCheckData MapDtoToHealthCheckData(HealthCheckDataDto dto)
        {
            return new HealthCheckData
            {
                Name = dto.Name,
                MachineName = dto.MachineName,
                ServiceType = dto.ServiceType,
                Status = dto.Status,
                Duration = dto.Duration,
                Description = dto.Description,
                CheckError = dto.CheckError,
                LastUpdated = dto.LastUpdated,
                CreationDate = dto.CreationDate
            };
        }

        public void Dispose()
        {
            _initializationLock?.Dispose();
        }
    }
}
