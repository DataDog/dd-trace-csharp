using System;
using System.Collections.Generic;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Contains integration-specific settings.
    /// </summary>
    public class IntegrationSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationSettings"/> class.
        /// </summary>
        /// <param name="integrationName">The integration name.</param>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        public IntegrationSettings(string integrationName, IConfigurationSource source)
        {
            IntegrationName = integrationName ?? throw new ArgumentNullException(nameof(integrationName));

            if (source == null)
            {
                return;
            }

            // wrap IConfigurationSource with fallbacks if not done already
            var fallbacks = source as FallbacksConfigurationSource ?? new FallbacksConfigurationSource(source);

            Enabled = fallbacks.GetBool(ConfigurationKeys.Integrations.Enabled, integrationName);

            AnalyticsEnabled = fallbacks.GetBool(ConfigurationKeys.Integrations.AnalyticsEnabled, integrationName);

            AnalyticsSampleRate = fallbacks.GetDouble(ConfigurationKeys.Integrations.AnalyticsSampleRate, integrationName) ?? 1.0;
        }

        /// <summary>
        /// Gets the name of the integration. Used to retrieve integration-specific settings.
        /// </summary>
        public string IntegrationName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// this integration is enabled.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// Analytics are enabled for this integration.
        /// </summary>
        public bool? AnalyticsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value between 0 and 1 (inclusive)
        /// that determines the sampling rate for this integration.
        /// </summary>
        public double AnalyticsSampleRate { get; set; }
    }
}
