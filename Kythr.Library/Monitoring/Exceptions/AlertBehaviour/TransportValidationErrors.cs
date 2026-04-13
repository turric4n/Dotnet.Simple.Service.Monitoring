using System;

namespace Kythr.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class TeamsValidationError : Exception
    {
        public TeamsValidationError(string message) : base(message) { }
    }

    public class DiscordValidationError : Exception
    {
        public DiscordValidationError(string message) : base(message) { }
    }

    public class PagerDutyValidationError : Exception
    {
        public PagerDutyValidationError(string message) : base(message) { }
    }

    public class OpsgenieValidationError : Exception
    {
        public OpsgenieValidationError(string message) : base(message) { }
    }

    public class DatadogValidationError : Exception
    {
        public DatadogValidationError(string message) : base(message) { }
    }

    public class PrometheusValidationError : Exception
    {
        public PrometheusValidationError(string message) : base(message) { }
    }

    public class CloudWatchValidationError : Exception
    {
        public CloudWatchValidationError(string message) : base(message) { }
    }

    public class AppInsightsValidationError : Exception
    {
        public AppInsightsValidationError(string message) : base(message) { }
    }

    public class ElasticsearchValidationError : Exception
    {
        public ElasticsearchValidationError(string message) : base(message) { }
    }

    public class GoogleChatValidationError : Exception
    {
        public GoogleChatValidationError(string message) : base(message) { }
    }

    public class MattermostValidationError : Exception
    {
        public MattermostValidationError(string message) : base(message) { }
    }

    public class ConsoleValidationError : Exception
    {
        public ConsoleValidationError(string message) : base(message) { }
    }

    public class FileTransportValidationError : Exception
    {
        public FileTransportValidationError(string message) : base(message) { }
    }

    public class RmqTransportValidationError : Exception
    {
        public RmqTransportValidationError(string message) : base(message) { }
    }

    public class KafkaTransportValidationError : Exception
    {
        public KafkaTransportValidationError(string message) : base(message) { }
    }

    public class WebhookValidationError : Exception
    {
        public WebhookValidationError(string message) : base(message) { }
    }
}
