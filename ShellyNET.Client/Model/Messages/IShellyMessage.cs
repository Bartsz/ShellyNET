namespace ShellyNET.Client.Model.Messages;

/// <summary>Represents a non-hot-path Shelly message delivered to the configuration message channel.</summary>
public interface IShellyMessage
{
    /// <summary>Gets the discriminant used for exhaustive message handling.</summary>
    ShellyMessageType MessageType { get; }
}
