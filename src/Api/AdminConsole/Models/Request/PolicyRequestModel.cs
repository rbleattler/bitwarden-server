﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Bit.Core.AdminConsole.Entities;
using Bit.Core.AdminConsole.Enums;

namespace Bit.Api.AdminConsole.Models.Request;

public class PolicyRequestModel
{
    [Required]
    public PolicyType? Type { get; set; }
    [Required]
    public bool? Enabled { get; set; }
    public Dictionary<string, object> Data { get; set; }

    public Policy ToPolicy(Guid orgId)
    {
        return ToPolicy(new Policy
        {
            Type = Type.Value,
            OrganizationId = orgId
        });
    }

    public Policy ToPolicy(Policy existingPolicy)
    {
        existingPolicy.Enabled = Enabled.GetValueOrDefault();
        existingPolicy.Data = Data != null ? JsonSerializer.Serialize(Data) : null;
        return existingPolicy;
    }
}
