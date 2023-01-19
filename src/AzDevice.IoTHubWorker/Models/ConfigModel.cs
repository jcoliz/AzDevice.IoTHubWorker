// Copyright (C) 2023 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

using Tomlyn.Model;

namespace AzDevice.Models;

public class ConfigModel : ITomlMetadataProvider
{
    public Provisioning? Provisioning { get; set; }

    // storage for comments and whitespace
    TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}

public class Provisioning : ITomlMetadataProvider
{
    public string? Source { get; set; }

    public string? GlobalEndpoint { get; set; }

    public string? IdScope { get; set; }

    public Attestation? Attestation { get; set; }

    // storage for comments and whitespace
    TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}

public class Attestation : ITomlMetadataProvider
{
    public string? Method { get; set; }

    public string? RegistrationId { get; set; }

    public SymmetricKey? SymmetricKey { get; set; }

    // storage for comments and whitespace
    TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}

public class SymmetricKey : ITomlMetadataProvider
{
    public string? Value { get; set; }

    // storage for comments and whitespace
    TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}