// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Auth;

// TODO: tests
// TODO: handle errors
// TODO: use binding
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    public interface IConfig
    {
        // Web service listening port
        int Port { get; }

        ILoggingConfig LoggingConfig { get; }

        // Service layer configuration
        IServicesConfig ServicesConfig { get; }

        // Client authentication and authorization configuration
        IClientAuthConfig ClientAuthConfig { get; }

        // Simulation speed configuration
        IRateLimitingConfig RateLimitingConfig { get; }

        // Deployment details
        IDeploymentConfig DeploymentConfig { get; }

        // Simulation multi-threading settings
        ISimulationConcurrencyConfig SimulationConcurrencyConfig { get; }

        // Clustering and partitioning settings
        IClusteringConfig ClusteringConfig { get; }
    }

    public class Config : IConfig
    {
        private const string APPLICATION_KEY = "DeviceSimulationService:";

        private const string PORT_KEY = APPLICATION_KEY + "webservice_port";
        private const string DEVICE_MODELS_FOLDER_KEY = APPLICATION_KEY + "device_models_folder";
        private const string DEVICE_MODELS_SCRIPTS_FOLDER_KEY = APPLICATION_KEY + "device_models_scripts_folder";
        private const string IOTHUB_DATA_FOLDER_KEY = APPLICATION_KEY + "iothub_data_folder";
        private const string IOTHUB_CONNSTRING_KEY = APPLICATION_KEY + "iothub_connstring";
        private const string IOTHUB_IMPORT_STORAGE_CONNSTRING_KEY = APPLICATION_KEY + "iothub_import_storage_account";
        private const string IOTHUB_SDK_DEVICE_CLIENT_TIMEOUT_KEY = APPLICATION_KEY + "iothub_sdk_device_client_timeout";
        private const string TWIN_READ_WRITE_ENABLED_KEY = APPLICATION_KEY + "twin_read_write_enabled";

        private const string IOTHUB_LIMITS_KEY = APPLICATION_KEY + "RateLimits:";
        private const string CONNECTIONS_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "device_connections_per_second";
        private const string REGISTRYOPS_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "registry_operations_per_minute";
        private const string DEVICE_MESSAGES_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "device_to_cloud_messages_per_second";
        private const string DEVICE_MESSAGES_DAILY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "device_to_cloud_messages_per_day";
        private const string TWIN_READS_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "twin_reads_per_second";
        private const string TWIN_WRITES_FREQUENCY_LIMIT_KEY = IOTHUB_LIMITS_KEY + "twin_writes_per_second";

        private const string CONCURRENCY_KEY = APPLICATION_KEY + "SimulationConcurrency:";
        private const string CONCURRENCY_TELEMETRY_THREADS_KEY = CONCURRENCY_KEY + "telemetry_threads";
        private const string CONCURRENCY_MAX_PENDING_CONNECTIONS_KEY = CONCURRENCY_KEY + "max_pending_connections";
        private const string CONCURRENCY_MAX_PENDING_TELEMETRY_KEY = CONCURRENCY_KEY + "max_pending_telemetry_messages";
        private const string CONCURRENCY_MAX_PENDING_TWIN_WRITES_KEY = CONCURRENCY_KEY + "max_pending_twin_writes";
        private const string CONCURRENCY_MIN_DEVICE_TELEMETRY_LOOP_DURATION_KEY = CONCURRENCY_KEY + "min_device_telemetry_loop_duration";
        private const string CONCURRENCY_MIN_DEVICE_STATE_LOOP_DURATION_KEY = CONCURRENCY_KEY + "min_device_state_loop_duration";
        private const string CONCURRENCY_MIN_DEVICE_CONNECTION_LOOP_DURATION_KEY = CONCURRENCY_KEY + "min_device_connection_loop_duration";
        private const string CONCURRENCY_MIN_DEVICE_PROPERTIES_LOOP_DURATION_KEY = CONCURRENCY_KEY + "min_device_properties_loop_duration";

        private const string CLUSTERING_KEY = APPLICATION_KEY + "Clustering:";
        private const string CLUSTERING_CHECK_INTERVAL_KEY = CLUSTERING_KEY + "check_interval";
        private const string CLUSTERING_NODE_RECORD_MAX_AGE_KEY = CLUSTERING_KEY + "node_record_max_age";
        private const string CLUSTERING_MASTER_LOCK_MAX_AGE_KEY = CLUSTERING_KEY + "master_lock_duration";
        private const string CLUSTERING_MAX_PARTITION_SIZE_KEY = CLUSTERING_KEY + "max_partition_size";

        private const string STORAGE_ADAPTER_KEY = "StorageAdapterService:";
        private const string STORAGE_ADAPTER_API_URL_KEY = STORAGE_ADAPTER_KEY + "webservice_url";
        private const string STORAGE_ADAPTER_API_TIMEOUT_KEY = STORAGE_ADAPTER_KEY + "webservice_timeout";

        private const string AZURE_MANAGEMENT_ADAPTER_KEY = "AzureManagementService:";
        private const string AZURE_MANAGEMENT_ADAPTER_API_URL_KEY = AZURE_MANAGEMENT_ADAPTER_KEY + "webservice_url";
        private const string AZURE_MANAGEMENT_ADAPTER_API_TIMEOUT_KEY = AZURE_MANAGEMENT_ADAPTER_KEY + "webservice_timeout";
        private const string AZURE_MANAGEMENT_ADAPTER_API_VERSION = AZURE_MANAGEMENT_ADAPTER_KEY + "api_version";

        private const string MAIN_STORAGE_KEY = APPLICATION_KEY + "Storage:Main:";
        private const string NODES_STORAGE_KEY = APPLICATION_KEY + "Storage:Nodes:";
        private const string SIMULATIONS_STORAGE_KEY = APPLICATION_KEY + "Storage:Simulations:";
        private const string DEVICES_STORAGE_KEY = APPLICATION_KEY + "Storage:Devices:";
        private const string PARTITIONS_STORAGE_KEY = APPLICATION_KEY + "Storage:Partitions:";

        private const string STORAGE_TYPE_KEY = "type";
        private const string STORAGE_MAX_PENDING_OPERATIONS = APPLICATION_KEY + "max_pending_storage_tasks";
        private const string DOCUMENTDB_CONNECTION_STRING_KEY = "documentdb_connstring";
        private const string DOCUMENTDB_DATABASE_KEY = "documentdb_database";
        private const string DOCUMENTDB_COLLECTION_KEY = "documentdb_collection";
        private const string DOCUMENTDB_THROUGHPUT_KEY = "documentdb_collection_throughput";

        private const string LOGGING_KEY = APPLICATION_KEY + "Logging:";
        private const string LOGGING_LOGLEVEL_KEY = LOGGING_KEY + "LogLevel";
        private const string LOGGING_INCLUDEPROCESSID_KEY = LOGGING_KEY + "IncludeProcessId";
        private const string LOGGING_DATEFORMAT_KEY = LOGGING_KEY + "DateFormat";
        private const string LOGGING_BLACKLIST_SOURCES_KEY = LOGGING_KEY + "BlackListSources";
        private const string LOGGING_WHITELIST_SOURCES_KEY = LOGGING_KEY + "WhiteListSources";
        private const string LOGGING_EXTRADIAGNOSTICS_KEY = LOGGING_KEY + "ExtraDiagnostics";
        private const string LOGGING_EXTRADIAGNOSTICSPATH_KEY = LOGGING_KEY + "ExtraDiagnosticsPath";
        private const string LOGGING_DIAGNOSTICS_URL_KEY = LOGGING_KEY + "diagnostics_endpoint_url";

        private const string CLIENT_AUTH_KEY = APPLICATION_KEY + "ClientAuth:";
        private const string CORS_WHITELIST_KEY = CLIENT_AUTH_KEY + "cors_whitelist";
        private const string AUTH_TYPE_KEY = CLIENT_AUTH_KEY + "auth_type";
        private const string AUTH_REQUIRED_KEY = CLIENT_AUTH_KEY + "auth_required";

        private const string JWT_KEY = APPLICATION_KEY + "ClientAuth:JWT:";
        private const string JWT_ALGOS_KEY = JWT_KEY + "allowed_algorithms";
        private const string JWT_ISSUER_KEY = JWT_KEY + "issuer";
        private const string JWT_AUDIENCE_KEY = JWT_KEY + "audience";
        private const string JWT_CLOCK_SKEW_KEY = JWT_KEY + "clock_skew_seconds";

        private const string DEPLOYMENT_KEY = APPLICATION_KEY + "Deployment:";
        private const string AZURE_SUBSCRIPTION_DOMAIN = DEPLOYMENT_KEY + "azure_subscription_domain";
        private const string AZURE_SUBSCRIPTION_ID = DEPLOYMENT_KEY + "azure_subscription_id";
        private const string AZURE_RESOURCE_GROUP = DEPLOYMENT_KEY + "azure_resource_group";
        private const string AZURE_IOTHUB_NAME = DEPLOYMENT_KEY + "azure_iothub_name";

        private const string AZURE_ACTIVE_DIRECTORY_KEY = APPLICATION_KEY + "AzureActiveDirectory:";
        private const string AAD_TENANT_ID = AZURE_ACTIVE_DIRECTORY_KEY + "tenant_id";
        private const string AAD_APP_ID = AZURE_ACTIVE_DIRECTORY_KEY + "app_id";
        private const string AAD_APP_SECRET = AZURE_ACTIVE_DIRECTORY_KEY + "app_secret";
        private const string AAD_ACCESS_TOKEN_URL = AZURE_ACTIVE_DIRECTORY_KEY + "access_token_url";

        public int Port { get; }
        public ILoggingConfig LoggingConfig { get; set; }
        public IClientAuthConfig ClientAuthConfig { get; }
        public IServicesConfig ServicesConfig { get; }
        public IRateLimitingConfig RateLimitingConfig { get; set; }
        public IDeploymentConfig DeploymentConfig { get; set; }
        public ISimulationConcurrencyConfig SimulationConcurrencyConfig { get; set; }
        public IClusteringConfig ClusteringConfig { get; }

        public Config(IConfigData configData)
        {
            this.Port = configData.GetInt(PORT_KEY);
            this.LoggingConfig = GetLogConfig(configData);
            this.ServicesConfig = GetServicesConfig(configData);
            this.ClientAuthConfig = GetClientAuthConfig(configData);
            this.RateLimitingConfig = GetRateLimitingConfig(configData);
            this.DeploymentConfig = GetDeploymentConfig(configData);
            this.SimulationConcurrencyConfig = GetConcurrencyConfig(configData);
            this.ClusteringConfig = GetClusteringConfig(configData);
        }

        private static ILoggingConfig GetLogConfig(IConfigData configData)
        {
            var data = configData.GetString(LOGGING_BLACKLIST_SOURCES_KEY);
            var values = data.Replace(";", ",").Replace(":", ".").Split(",");
            var blacklist = new HashSet<string>();
            foreach (var k in values) blacklist.Add(k);

            data = configData.GetString(LOGGING_WHITELIST_SOURCES_KEY);
            values = data.Replace(";", ",").Replace(":", ".").Split(",");
            var whitelist = new HashSet<string>();
            foreach (var k in values) blacklist.Add(k);

            Enum.TryParse(configData.GetString(LOGGING_LOGLEVEL_KEY, Services.Diagnostics.LoggingConfig.DEFAULT_LOGLEVEL.ToString()), true, out LogLevel logLevel);
            var result = new LoggingConfig
            {
                LogLevel = logLevel,
                BlackList = blacklist,
                WhiteList = whitelist,
                DateFormat = configData.GetString(LOGGING_DATEFORMAT_KEY, Services.Diagnostics.LoggingConfig.DEFAULT_DATE_FORMAT),
                LogProcessId = configData.GetBool(LOGGING_INCLUDEPROCESSID_KEY, true),
                ExtraDiagnostics = configData.GetBool(LOGGING_EXTRADIAGNOSTICS_KEY, false),
                ExtraDiagnosticsPath = configData.GetString(LOGGING_EXTRADIAGNOSTICSPATH_KEY)
            };

            return result;
        }

        private static IClientAuthConfig GetClientAuthConfig(IConfigData configData)
        {
            return new ClientAuthConfig
            {
                // By default CORS is disabled
                CorsWhitelist = configData.GetString(CORS_WHITELIST_KEY, string.Empty),
                // By default Auth is required
                AuthRequired = configData.GetBool(AUTH_REQUIRED_KEY, true),
                // By default auth type is JWT
                AuthType = configData.GetString(AUTH_TYPE_KEY, "JWT"),
                // By default the only trusted algorithms are RS256, RS384, RS512
                JwtAllowedAlgos = configData.GetString(JWT_ALGOS_KEY, "RS256,RS384,RS512").Split(','),
                JwtIssuer = configData.GetString(JWT_ISSUER_KEY, String.Empty),
                JwtAudience = configData.GetString(JWT_AUDIENCE_KEY, String.Empty),
                // By default the allowed clock skew is 2 minutes
                JwtClockSkew = TimeSpan.FromSeconds(configData.GetInt(JWT_CLOCK_SKEW_KEY, 120)),
            };
        }

        private static IServicesConfig GetServicesConfig(IConfigData configData)
        {
            var connstring = configData.GetString(IOTHUB_CONNSTRING_KEY);
            if (connstring.ToLowerInvariant().Contains("connection string"))
            {
                // In order to connect to Azure IoT Hub, the service requires a connection
                // string. The value can be found in the Azure Portal. For more information see
                // https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-getstarted
                // to find the connection string value.
                // The connection string can be stored in the 'appsettings.ini' configuration
                // file, or in the PCS_IOTHUB_CONNSTRING environment variable. When
                // working with VisualStudio, the environment variable can be set in the
                // WebService project settings, under the "Debug" tab.

                ShowIoTHubConnStringInstructions();

                throw new Exception("The service configuration is incomplete. " +
                                    "Please provide your Azure IoT Hub connection string. " +
                                    "For more information, see the environment variables " +
                                    "used in project properties and the 'iothub_connstring' " +
                                    "value in the 'appsettings.ini' configuration file.");
            }

            var hubImportStorageAccount = configData.GetString(IOTHUB_IMPORT_STORAGE_CONNSTRING_KEY);
            if (hubImportStorageAccount.ToLowerInvariant().Contains("connection string"))
            {
                throw new Exception("The service configuration is incomplete. " +
                                    "Please provide your Azure Storage Account connection string. " +
                                    "For more information, see the environment variables " +
                                    "used in project properties and the 'iothub_connstring' " +
                                    "value in the 'appsettings.ini' configuration file.");
            }

            return new ServicesConfig
            {
                DeviceModelsFolder = MapRelativePath(configData.GetString(DEVICE_MODELS_FOLDER_KEY)),
                DeviceModelsScriptsFolder = MapRelativePath(configData.GetString(DEVICE_MODELS_SCRIPTS_FOLDER_KEY)),
                IoTHubDataFolder = MapRelativePath(configData.GetString(IOTHUB_DATA_FOLDER_KEY)),
                IoTHubConnString = connstring,
                IoTHubImportStorageAccount = hubImportStorageAccount,
                IoTHubSdkDeviceClientTimeout = configData.GetOptionalUInt(IOTHUB_SDK_DEVICE_CLIENT_TIMEOUT_KEY),
                StorageAdapterApiUrl = configData.GetString(STORAGE_ADAPTER_API_URL_KEY),
                StorageAdapterApiTimeout = configData.GetInt(STORAGE_ADAPTER_API_TIMEOUT_KEY),
                AzureManagementAdapterApiUrl = configData.GetString(AZURE_MANAGEMENT_ADAPTER_API_URL_KEY),
                AzureManagementAdapterApiTimeout = configData.GetInt(AZURE_MANAGEMENT_ADAPTER_API_TIMEOUT_KEY),
                AzureManagementAdapterApiVersion = configData.GetString(AZURE_MANAGEMENT_ADAPTER_API_VERSION),
                TwinReadWriteEnabled = configData.GetBool(TWIN_READ_WRITE_ENABLED_KEY, true),
                MainStorage = GetStorageConfig(configData, MAIN_STORAGE_KEY),
                NodesStorage = GetStorageConfig(configData, NODES_STORAGE_KEY),
                SimulationsStorage = GetStorageConfig(configData, SIMULATIONS_STORAGE_KEY),
                DevicesStorage = GetStorageConfig(configData, DEVICES_STORAGE_KEY),
                PartitionsStorage = GetStorageConfig(configData, PARTITIONS_STORAGE_KEY),
                DiagnosticsEndpointUrl = configData.GetString(LOGGING_DIAGNOSTICS_URL_KEY)
            };
        }

        private static StorageConfig GetStorageConfig(IConfigData configData, string prefix)
        {
            var defaults = new StorageConfig();
            return new StorageConfig
            {
                StorageType = configData.GetString(prefix + STORAGE_TYPE_KEY, defaults.StorageType),
                MaxPendingOperations = configData.GetInt(STORAGE_MAX_PENDING_OPERATIONS, defaults.MaxPendingOperations),
                DocumentDbConnString = configData.GetString(prefix + DOCUMENTDB_CONNECTION_STRING_KEY),
                DocumentDbDatabase = configData.GetString(prefix + DOCUMENTDB_DATABASE_KEY),
                DocumentDbCollection = configData.GetString(prefix + DOCUMENTDB_COLLECTION_KEY),
                DocumentDbThroughput = configData.GetInt(prefix + DOCUMENTDB_THROUGHPUT_KEY, defaults.DocumentDbThroughput)
            };
        }

        private static void ShowIoTHubConnStringInstructions()
        {
            WriteLine(ConsoleColor.Yellow, ConsoleColor.Red, "Azure IoT Hub connection string not configured");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "");
            WriteLine(ConsoleColor.Yellow, ConsoleColor.Black, "If you are running the service in Docker or a VM:");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "* The service configuration is stored in appsettings.ini.");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "* If the file references an environment variable, check the environment configuration.");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "");
            WriteLine(ConsoleColor.Yellow, ConsoleColor.Black, "If you are running the service from an IDE (VS, Rider, etc.):");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "* The service configuration is stored in appsettings.ini. If the file references an environment variable:");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "  - Visual Studio: check the WebService project settings (or check WebService/Properties/launchSettings.json)");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "  - Visual Studio for Mac: check the WebService project settings (or check WebService/WebService.csproj)");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "  - Visual Studio Code: check .vscode/launch.json");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "  - IntelliJ Rider: check your Run Configuration (or files under .idea folder)");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "  - Check also your development environment, where you might have environment variables set");
            WriteLine(ConsoleColor.White, ConsoleColor.Black, "");
        }

        private static void WriteLine(ConsoleColor fg, ConsoleColor bg, string text)
        {
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
            Console.WriteLine(" {0,-120}", text);
            Console.ResetColor();
        }

        private static IRateLimitingConfig GetRateLimitingConfig(IConfigData configData)
        {
            return new RateLimitingConfig
            {
                ConnectionsPerSecond = configData.GetInt(CONNECTIONS_FREQUENCY_LIMIT_KEY, 50),
                RegistryOperationsPerMinute = configData.GetInt(REGISTRYOPS_FREQUENCY_LIMIT_KEY, 50),
                DeviceMessagesPerSecond = configData.GetInt(DEVICE_MESSAGES_FREQUENCY_LIMIT_KEY, 50),
                DeviceMessagesPerDay = configData.GetLong(DEVICE_MESSAGES_DAILY_LIMIT_KEY, 8000),
                TwinReadsPerSecond = configData.GetInt(TWIN_READS_FREQUENCY_LIMIT_KEY, 5),
                TwinWritesPerSecond = configData.GetInt(TWIN_WRITES_FREQUENCY_LIMIT_KEY, 5)
            };
        }

        private static IDeploymentConfig GetDeploymentConfig(IConfigData configData)
        {
            return new DeploymentConfig
            {
                AzureSubscriptionDomain = configData.GetString(AZURE_SUBSCRIPTION_DOMAIN, "undefined.onmicrosoft.com"),
                AzureSubscriptionId = configData.GetString(AZURE_SUBSCRIPTION_ID, Guid.Empty.ToString()),
                AzureResourceGroup = configData.GetString(AZURE_RESOURCE_GROUP, "undefined"),
                AzureIothubName = configData.GetString(AZURE_IOTHUB_NAME, "undefined"),
                AadTenantId = configData.GetString(AAD_TENANT_ID, "undefined"),
                AadAppId = configData.GetString(AAD_APP_ID, "undefined"),
                AadAppSecret = configData.GetString(AAD_APP_SECRET, "undefined"),
                AadTokenUrl = configData.GetString(AAD_ACCESS_TOKEN_URL, "undefinedd")
            };
        }

        private static ISimulationConcurrencyConfig GetConcurrencyConfig(IConfigData configData)
        {
            var defaults = new SimulationConcurrencyConfig();
            return new SimulationConcurrencyConfig
            {
                TelemetryThreads = configData.GetInt(CONCURRENCY_TELEMETRY_THREADS_KEY, defaults.TelemetryThreads),
                MaxPendingConnections = configData.GetInt(CONCURRENCY_MAX_PENDING_CONNECTIONS_KEY, defaults.MaxPendingConnections),
                MaxPendingTelemetry = configData.GetInt(CONCURRENCY_MAX_PENDING_TELEMETRY_KEY, defaults.MaxPendingTelemetry),
                MaxPendingTwinWrites = configData.GetInt(CONCURRENCY_MAX_PENDING_TWIN_WRITES_KEY, defaults.MaxPendingTwinWrites),
                MinDeviceStateLoopDuration = configData.GetInt(CONCURRENCY_MIN_DEVICE_STATE_LOOP_DURATION_KEY, defaults.MinDeviceStateLoopDuration),
                MinDeviceConnectionLoopDuration = configData.GetInt(CONCURRENCY_MIN_DEVICE_CONNECTION_LOOP_DURATION_KEY, defaults.MinDeviceConnectionLoopDuration),
                MinDeviceTelemetryLoopDuration = configData.GetInt(CONCURRENCY_MIN_DEVICE_TELEMETRY_LOOP_DURATION_KEY, defaults.MinDeviceTelemetryLoopDuration),
                MinDevicePropertiesLoopDuration = configData.GetInt(CONCURRENCY_MIN_DEVICE_PROPERTIES_LOOP_DURATION_KEY, defaults.MinDevicePropertiesLoopDuration)
            };
        }

        private static IClusteringConfig GetClusteringConfig(IConfigData configData)
        {
            var defaults = new ClusteringConfig();
            return new ClusteringConfig
            {
                CheckIntervalMsecs = configData.GetInt(CLUSTERING_CHECK_INTERVAL_KEY, defaults.CheckIntervalMsecs),
                NodeRecordMaxAgeMsecs = configData.GetInt(CLUSTERING_NODE_RECORD_MAX_AGE_KEY, defaults.NodeRecordMaxAgeMsecs),
                MasterLockDurationMsecs = configData.GetInt(CLUSTERING_MASTER_LOCK_MAX_AGE_KEY, defaults.MasterLockDurationMsecs),
                MaxPartitionSize = configData.GetInt(CLUSTERING_MAX_PARTITION_SIZE_KEY, defaults.MaxPartitionSize)
            };
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }
    }
}
