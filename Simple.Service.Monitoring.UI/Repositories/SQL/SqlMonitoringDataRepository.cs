using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

                // Create table if it doesn't exist (works for both SQL Server and Azure SQL)
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
                    healthCheckData.CreationDate = DateTime.UtcNow;
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
                        LastUpdated = DateTime.UtcNow
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
                
                foreach (var data in dataList)
                {
                    // Ensure CreationDate is set
                    if (data.CreationDate == default)
                    {
                        data.CreationDate = DateTime.UtcNow;
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

                    // If latest check exists and has same status, just update LastUpdated
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
                            LastUpdated = DateTime.UtcNow
                        }, transaction);

                        _logger.LogDebug("Updated LastUpdated for health check {Name} on {MachineName} with status {Status}", 
                            data.Name, data.MachineName, data.Status);
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
                _logger.LogError(ex, "Error adding health checks data in bulk");
                throw;
            }
        }

        // Existing methods unchanged...
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

        // Map from database DTO to domain object
        private static HealthCheckData MapDtoToHealthCheckData(HealthCheckDataDto dto)
        {
            return new HealthCheckData
            {
                Name = dto.Name,
                MachineName = dto.MachineName,
                ServiceType = dto.ServiceType,
                Status = (HealthStatus)dto.Status,
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
