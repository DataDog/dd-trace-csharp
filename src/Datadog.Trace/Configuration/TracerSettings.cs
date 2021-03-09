using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.Serilog;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Contains Tracer settings.
    /// </summary>
    public class TracerSettings
    {
        /// <summary>
        /// The default host value for <see cref="AgentUri"/>.
        /// </summary>
        public const string DefaultAgentHost = "localhost";

        /// <summary>
        /// The default port value for <see cref="AgentUri"/>.
        /// </summary>
        public const int DefaultAgentPort = 8126;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracerSettings"/> class with default values.
        /// </summary>
        public TracerSettings()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TracerSettings"/> class
        /// using the specified <see cref="IConfigurationSource"/> to initialize values.
        /// </summary>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        public TracerSettings(IConfigurationSource source)
        {
            Environment = source?.GetString(ConfigurationKeys.Environment);

            ServiceName = source?.GetString(ConfigurationKeys.ServiceName) ??
                          // backwards compatibility for names used in the past
                          source?.GetString("DD_SERVICE_NAME");

            ServiceVersion = source?.GetString(ConfigurationKeys.ServiceVersion);

            TraceEnabled = source?.GetBool(ConfigurationKeys.TraceEnabled) ??
                           // default value
                           true;

            if (AzureAppServices.Metadata.IsRelevant && AzureAppServices.Metadata.IsUnsafeToTrace)
            {
                TraceEnabled = false;
            }

            var disabledIntegrationNames = source?.GetString(ConfigurationKeys.DisabledIntegrations)
                                                 ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ??
                                           Enumerable.Empty<string>();

            DisabledIntegrationNames = new HashSet<string>(disabledIntegrationNames, StringComparer.OrdinalIgnoreCase);

            var adonetExcludedTypes = source?.GetString(ConfigurationKeys.AdoNetExcludedTypes)
                                                 ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ??
                                           Enumerable.Empty<string>();

            AdoNetExcludedTypes = new HashSet<string>(adonetExcludedTypes, StringComparer.OrdinalIgnoreCase);

            Integrations = new IntegrationSettingsCollection(source);

            var agentHost = source?.GetString(ConfigurationKeys.AgentHost) ??
                            // backwards compatibility for names used in the past
                            source?.GetString("DD_TRACE_AGENT_HOSTNAME") ??
                            source?.GetString("DATADOG_TRACE_AGENT_HOSTNAME") ??
                            // default value
                            DefaultAgentHost;

            var agentPort = source?.GetInt32(ConfigurationKeys.AgentPort) ??
                            // backwards compatibility for names used in the past
                            source?.GetInt32("DATADOG_TRACE_AGENT_PORT") ??
                            // default value
                            DefaultAgentPort;

            var agentUri = source?.GetString(ConfigurationKeys.AgentUri) ??
                           // default value
                           $"http://{agentHost}:{agentPort}";

            AgentUri = new Uri(agentUri);

            TracesPipeName = source?.GetString(ConfigurationKeys.TracesPipeName);

            TracesPipeTimeoutMs = source?.GetInt32(ConfigurationKeys.TracesPipeTimeoutMs)
#if DEBUG
            ?? 20_000;
#else
            ?? 100;
#endif

            TracesTransport = source?.GetString(ConfigurationKeys.TracesTransport);

            if (string.Equals(AgentUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Replace localhost with 127.0.0.1 to avoid DNS resolution.
                // When ipv6 is enabled, localhost is first resolved to ::1, which fails
                // because the trace agent is only bound to ipv4.
                // This causes delays when sending traces.
                var builder = new UriBuilder(agentUri) { Host = "127.0.0.1" };
                AgentUri = builder.Uri;
            }

            AnalyticsEnabled = source?.GetBool(ConfigurationKeys.GlobalAnalyticsEnabled) ??
                               // default value
                               false;

            LogsInjectionEnabled = source?.GetBool(ConfigurationKeys.LogsInjectionEnabled) ??
                                   // default value
                                   false;

            MaxTracesSubmittedPerSecond = source?.GetInt32(ConfigurationKeys.MaxTracesSubmittedPerSecond) ??
                                          // default value
                                          100;

            GlobalTags = source?.GetDictionary(ConfigurationKeys.GlobalTags) ??
                         // backwards compatibility for names used in the past
                         source?.GetDictionary("DD_TRACE_GLOBAL_TAGS") ??
                         // default value (empty)
                         new ConcurrentDictionary<string, string>();

            // Filter out tags with empty keys or empty values, and trim whitespace
            GlobalTags = GlobalTags.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                                   .ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim());

            HeaderTags = source?.GetDictionary(ConfigurationKeys.HeaderTags) ??
                         // default value (empty)
                         new ConcurrentDictionary<string, string>();

            // Filter out tags with empty keys or empty values, and trim whitespace
            HeaderTags = HeaderTags.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                                   .ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim());

            var serviceNameMappings = source?.GetDictionary(ConfigurationKeys.ServiceNameMappings)
                                      ?.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                                      ?.ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim());

            ServiceNameMappings = new ServiceNames(serviceNameMappings);

            DogStatsdPort = source?.GetInt32(ConfigurationKeys.DogStatsdPort) ??
                            // default value
                            8125;

            TracerMetricsEnabled = source?.GetBool(ConfigurationKeys.TracerMetricsEnabled) ??
                                   // default value
                                   false;

            RuntimeMetricsEnabled = source?.GetBool(ConfigurationKeys.RuntimeMetricsEnabled) ??
                                    false;

            CustomSamplingRules = source?.GetString(ConfigurationKeys.CustomSamplingRules);

            GlobalSamplingRate = source?.GetDouble(ConfigurationKeys.GlobalSamplingRate);

            StartupDiagnosticLogEnabled = source?.GetBool(ConfigurationKeys.StartupDiagnosticLogEnabled) ??
                                          // default value
                                          true;

            var httpServerErrorStatusCodes = source?.GetString(ConfigurationKeys.HttpServerErrorStatusCodes) ??
                                           // Default value
                                           "500-599";
            HttpServerErrorStatusCodes = ParseHttpCodesToArray(httpServerErrorStatusCodes);

            var httpClientErrorStatusCodes = source?.GetString(ConfigurationKeys.HttpClientErrorStatusCodes) ??
                                        // Default value
                                        "400-499";
            HttpClientErrorStatusCodes = ParseHttpCodesToArray(httpClientErrorStatusCodes);

            TraceBufferSize = source?.GetInt32(ConfigurationKeys.BufferSize)
                ?? 1024 * 1024 * 10; // 10MB

            TraceBatchInterval = source?.GetInt32(ConfigurationKeys.SerializationBatchInterval)
                        ?? 100;

            AspnetRouteTemplateResourceNamesEnabled = IsFeatureFlagEnabled(ConfigurationKeys.FeatureFlags.AspnetRouteTemplateResourceNamesEnabled);
        }

        /// <summary>
        /// Gets or sets the default environment name applied to all spans.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.Environment"/>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the service name applied to top-level spans and used to build derived service names.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.ServiceName"/>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the version tag applied to all spans.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.ServiceVersion"/>
        public string ServiceVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TraceEnabled"/>
        public bool TraceEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether debug is enabled for a tracer.
        /// This property is obsolete. Manage the debug setting through GlobalSettings.
        /// </summary>
        /// <seealso cref="GlobalSettings.DebugEnabled"/>
        [Obsolete]
        public bool DebugEnabled { get; set; }

        /// <summary>
        /// Gets or sets the names of disabled integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.DisabledIntegrations"/>
        public HashSet<string> DisabledIntegrationNames { get; set; }

        /// <summary>
        /// Gets or sets the AdoNet types to exclude from automatic instrumentation.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.AdoNetExcludedTypes"/>
        public HashSet<string> AdoNetExcludedTypes { get; set; }

        /// <summary>
        /// Gets or sets the Uri where the Tracer can connect to the Agent.
        /// Default is <c>"http://localhost:8126"</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.AgentUri"/>
        /// <seealso cref="ConfigurationKeys.AgentHost"/>
        /// <seealso cref="ConfigurationKeys.AgentPort"/>
        public Uri AgentUri { get; set; }

        /// <summary>
        /// Gets or sets the key used to determine the transport for sending traces.
        /// Default is <c>null</c>, which will use the default path decided in <see cref="Agent.Api"/>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesTransport"/>
        public string TracesTransport { get; set; }

        /// <summary>
        /// Gets or sets the windows pipe name where the Tracer can connect to the Agent.
        /// Default is <c>null</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesPipeName"/>
        public string TracesPipeName { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds for the windows named pipe requests.
        /// Default is <c>100</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesPipeTimeoutMs"/>
        public int TracesPipeTimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the windows pipe name where the Tracer can send stats.
        /// Default is <c>null</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.MetricsPipeName"/>
        public string MetricsPipeName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether default Analytics are enabled.
        /// Settings this value is a shortcut for setting
        /// <see cref="Configuration.IntegrationSettings.AnalyticsEnabled"/> on some predetermined integrations.
        /// See the documentation for more details.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.GlobalAnalyticsEnabled"/>
        public bool AnalyticsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether correlation identifiers are
        /// automatically injected into the logging context.
        /// Default is <c>false</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.LogsInjectionEnabled"/>
        public bool LogsInjectionEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the maximum number of traces set to AutoKeep (p1) per second.
        /// Default is <c>100</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.MaxTracesSubmittedPerSecond"/>
        public int MaxTracesSubmittedPerSecond { get; set; }

        /// <summary>
        /// Gets or sets a value indicating custom sampling rules.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.CustomSamplingRules"/>
        public string CustomSamplingRules { get; set; }

        /// <summary>
        /// Gets or sets a value indicating a global rate for sampling.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.GlobalSamplingRate"/>
        public double? GlobalSamplingRate { get; set; }

        /// <summary>
        /// Gets a collection of <see cref="Integrations"/> keyed by integration name.
        /// </summary>
        public IntegrationSettingsCollection Integrations { get; }

        /// <summary>
        /// Gets or sets the global tags, which are applied to all <see cref="Span"/>s.
        /// </summary>
        public IDictionary<string, string> GlobalTags { get; set; }

        /// <summary>
        /// Gets or sets the map of header keys to tag names, which are applied to the root <see cref="Span"/> of incoming requests.
        /// </summary>
        public IDictionary<string, string> HeaderTags { get; set; }

        /// <summary>
        /// Gets or sets the port where the DogStatsd server is listening for connections.
        /// Default is <c>8125</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.DogStatsdPort"/>
        public int DogStatsdPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether internal metrics
        /// are enabled and sent to DogStatsd.
        /// </summary>
        public bool TracerMetricsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether runtime metrics
        /// are enabled and sent to DogStatsd.
        /// </summary>
        public bool RuntimeMetricsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the use
        /// of System.Diagnostics.DiagnosticSource is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <remark>
        /// This value cannot be set in code. Instead,
        /// set it using the <c>DD_TRACE_DIAGNOSTIC_SOURCE_ENABLED</c>
        /// environment variable or in configuration files.
        /// </remark>
        public bool DiagnosticSourceEnabled
        {
            get => GlobalSettings.Source.DiagnosticSourceEnabled;
            set { }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the diagnostic log at startup is enabled
        /// </summary>
        public bool StartupDiagnosticLogEnabled { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code that should be marked as errors for server integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpServerErrorStatusCodes"/>
        internal bool[] HttpServerErrorStatusCodes { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code that should be marked as errors for client integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientErrorStatusCodes"/>
        internal bool[] HttpClientErrorStatusCodes { get; set; }

        /// <summary>
        /// Gets configuration values for changing service names based on configuration
        /// </summary>
        internal ServiceNames ServiceNameMappings { get; }

        /// <summary>
        /// Gets or sets a value indicating the size in bytes of the trace buffer
        /// </summary>
        internal int TraceBufferSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the batch interval for the serialization queue, in milliseconds
        /// </summary>
        internal int TraceBatchInterval { get; set; }

        /// <summary>
        /// Gets a value indicating whether the feature flag to enable the updated ASP.NET resource names is enabled
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientErrorStatusCodes"/>
        internal bool AspnetRouteTemplateResourceNamesEnabled { get; }

        /// <summary>
        /// Create a <see cref="TracerSettings"/> populated from the default sources
        /// returned by <see cref="CreateDefaultConfigurationSource"/>.
        /// </summary>
        /// <returns>A <see cref="TracerSettings"/> populated from the default sources.</returns>
        public static TracerSettings FromDefaultSources()
        {
            var source = CreateDefaultConfigurationSource();
            return new TracerSettings(source);
        }

        /// <summary>
        /// Creates a <see cref="IConfigurationSource"/> by combining environment variables,
        /// AppSettings where available, and a local datadog.json file, if present.
        /// </summary>
        /// <returns>A new <see cref="IConfigurationSource"/> instance.</returns>
        public static CompositeConfigurationSource CreateDefaultConfigurationSource()
        {
            return GlobalSettings.CreateDefaultConfigurationSource();
        }

        /// <summary>
        /// Sets the HTTP status code that should be marked as errors for client integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientErrorStatusCodes"/>
        /// <param name="statusCodes">Status codes that should be marked as errors</param>
        public void SetHttpClientErrorStatusCodes(IEnumerable<int> statusCodes)
        {
            HttpClientErrorStatusCodes = ParseHttpCodesToArray(string.Join(",", statusCodes));
        }

        /// <summary>
        /// Sets the HTTP status code that should be marked as errors for server integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpServerErrorStatusCodes"/>
        /// <param name="statusCodes">Status codes that should be marked as errors</param>
        public void SetHttpServerErrorStatusCodes(IEnumerable<int> statusCodes)
        {
            HttpServerErrorStatusCodes = ParseHttpCodesToArray(string.Join(",", statusCodes));
        }

        /// <summary>
        /// Sets the mappings to use for service names within a <see cref="Span"/>
        /// </summary>
        /// <param name="mappings">Mappings to use from original service name (e.g. <code>sql-server</code> or <code>graphql</code>)
        /// as the <see cref="KeyValuePair{TKey, TValue}.Key"/>) to replacement service names as <see cref="KeyValuePair{TKey, TValue}.Value"/>).</param>
        public void SetServiceNameMappings(IEnumerable<KeyValuePair<string, string>> mappings)
        {
            ServiceNameMappings.SetServiceNameMappings(mappings);
        }

        /// <summary>
        /// Populate the internal structures. Modifying the settings past this point is not supported
        /// </summary>
        internal void Freeze()
        {
            Integrations.SetDisabledIntegrations(DisabledIntegrationNames);
        }

        internal bool IsErrorStatusCode(int statusCode, bool serverStatusCode)
        {
            var source = serverStatusCode ? HttpServerErrorStatusCodes : HttpClientErrorStatusCodes;

            if (source == null)
            {
                return false;
            }

            if (statusCode >= source.Length)
            {
                return false;
            }

            return source[statusCode];
        }

        internal bool IsIntegrationEnabled(IntegrationInfo integration, bool defaultValue = true)
        {
            if (TraceEnabled && !DomainMetadata.ShouldAvoidAppDomain())
            {
                return Integrations[integration].Enabled ?? defaultValue;
            }

            return false;
        }

        internal bool IsIntegrationEnabled(string integrationName)
        {
            if (TraceEnabled && !DomainMetadata.ShouldAvoidAppDomain())
            {
                bool? enabled = Integrations[integrationName].Enabled;
                return enabled != false;
            }

            return false;
        }

        internal double? GetIntegrationAnalyticsSampleRate(IntegrationInfo integration, bool enabledWithGlobalSetting)
        {
            var integrationSettings = Integrations[integration];
            var analyticsEnabled = integrationSettings.AnalyticsEnabled ?? (enabledWithGlobalSetting && AnalyticsEnabled);
            return analyticsEnabled ? integrationSettings.AnalyticsSampleRate : (double?)null;
        }

        internal bool IsNetStandardFeatureFlagEnabled()
        {
            return IsFeatureFlagEnabled(ConfigurationKeys.FeatureFlags.NetStandardEnabled);
        }

        internal bool IsFeatureFlagEnabled(string featureFlag)
        {
            var value = EnvironmentHelpers.GetEnvironmentVariable(featureFlag, string.Empty);

            return value == "1" || value == "true";
        }

        internal bool[] ParseHttpCodesToArray(string httpStatusErrorCodes)
        {
            bool[] httpErrorCodesArray = new bool[600];

            void TrySetValue(int index)
            {
                if (index >= 0 && index < httpErrorCodesArray.Length)
                {
                    httpErrorCodesArray[index] = true;
                }
            }

            string[] configurationsArray = httpStatusErrorCodes.Replace(" ", string.Empty).Split(',');

            foreach (string statusConfiguration in configurationsArray)
            {
                int startStatus;

                // Checks that the value about to be used follows the `401-404` structure or single 3 digit number i.e. `401` else log the warning
                if (!Regex.IsMatch(statusConfiguration, @"^\d{3}-\d{3}$|^\d{3}$"))
                {
                    Log.Warning("Wrong format '{0}' for DD_HTTP_SERVER/CLIENT_ERROR_STATUSES configuration.", statusConfiguration);
                }

                // If statusConfiguration equals a single value i.e. `401` parse the value and save to the array
                else if (int.TryParse(statusConfiguration, out startStatus))
                {
                    TrySetValue(startStatus);
                }
                else
                {
                    string[] statusCodeLimitsRange = statusConfiguration.Split('-');

                    startStatus = int.Parse(statusCodeLimitsRange[0]);
                    int endStatus = int.Parse(statusCodeLimitsRange[1]);

                    if (endStatus < startStatus)
                    {
                        startStatus = endStatus;
                        endStatus = int.Parse(statusCodeLimitsRange[0]);
                    }

                    for (int statusCode = startStatus; statusCode <= endStatus; statusCode++)
                    {
                        TrySetValue(statusCode);
                    }
                }
            }

            return httpErrorCodesArray;
        }

        internal string GetServiceName(Tracer tracer, string serviceName)
        {
            return ServiceNameMappings.GetServiceName(tracer.DefaultServiceName, serviceName);
        }
    }
}
