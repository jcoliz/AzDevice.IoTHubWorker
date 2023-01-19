// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using System.Net;
using System.Text.Json.Serialization;

namespace AzDevice.Models;

public class PropertyChangeAck
{
    /// <summary>
    /// The unserialized property value.
    /// </summary>
    [JsonPropertyName("value")]
    public object? PropertyValue { get; set; } = null;

    /// <summary>
    /// The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.
    /// </summary>
    [JsonPropertyName("ac")]
    public HttpStatusCode AckCode { get; set; }

    /// <summary>
    /// The acknowledgement version, as supplied in the property update request.
    /// </summary>
    [JsonPropertyName("av")]
    public long AckVersion { get; set; }

    /// <summary>
    /// The acknowledgement description, an optional, human-readable message about the result of the property update.
    /// </summary>
    [JsonPropertyName("ad")]
    public string? AckDescription { get; set; }
}