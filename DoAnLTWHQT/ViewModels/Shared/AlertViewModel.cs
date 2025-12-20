using System.Collections.Generic;

namespace Ltwhqt.ViewModels.Shared
{
    public class AlertViewModel
    {
        public string Type { get; set; } = "info";

        public string Message { get; set; } = string.Empty;

        public bool Dismissible { get; set; } = true;
    }

    public class SelectOptionViewModel
    {
        public string Value { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool Selected { get; set; }
    }

    public class StatisticCardViewModel
    {
        public string Label { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string SubLabel { get; set; } = string.Empty;

        public string Trend { get; set; } = string.Empty;

        public string Icon { get; set; } = "bi-graph-up";

        public string Context { get; set; } = "primary";

        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
