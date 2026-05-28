namespace Carrezance.Support.App.Models;

public sealed class SoftwareDetectionResult
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = "NotDetected";
    public string DetectionSource { get; init; } = string.Empty;
    public string DetectionPath { get; init; } = string.Empty;
    public string Confidence { get; init; } = "High";

    public string DisplayStatus => Status switch
    {
        "Detected" => "Détecté",
        "NotAnalyzed" => "Non analysé",
        "Unavailable" => "Non disponible",
        _ => "Non détecté"
    };
}
