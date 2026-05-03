using UnityEngine;

public class CampusRuntimeValidator : MonoBehaviour
{
    public bool HasExportedGraph(LocationRegistry locationRegistry, out string message)
    {
        if (locationRegistry == null || !locationRegistry.IsLoaded || locationRegistry.Count == 0)
        {
            message = "No campus graph loaded. Create and export the floor map first.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public bool HasScannedQrStart(QRLocationManager qrLocationManager, out string message)
    {
        if (qrLocationManager == null || !qrLocationManager.HasLocation)
        {
            message = "Scan a campus QR code first.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public bool ResolveDestination(
        LocationRegistry locationRegistry,
        string destinationId,
        out LocationData destination,
        out string message)
    {
        destination = null;

        if (string.IsNullOrWhiteSpace(destinationId))
        {
            message = "Choose a destination first.";
            return false;
        }

        if (!HasExportedGraph(locationRegistry, out message))
            return false;

        destination = locationRegistry.GetLocation(destinationId);
        if (destination == null)
        {
            message = "Destination is not available in the current graph.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public bool CanRequestPath(
        LocationRegistry locationRegistry,
        QRLocationManager qrLocationManager,
        string destinationId,
        out LocationData destination,
        out string message)
    {
        destination = null;

        if (!HasExportedGraph(locationRegistry, out message))
            return false;

        if (!HasScannedQrStart(qrLocationManager, out message))
            return false;

        return ResolveDestination(locationRegistry, destinationId, out destination, out message);
    }
}
