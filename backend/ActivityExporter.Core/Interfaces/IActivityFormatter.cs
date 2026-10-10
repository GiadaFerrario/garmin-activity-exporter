using ActivityExporter.Core.Models;

namespace ActivityExporter.Core.Interfaces;

public interface IActivityFormatter
{
    string Format(ActivitySummary activitySummary);
}