﻿using Bit.Core.Enums;

namespace Bit.Core.Models.StaticStore.Plans;

public record FreePlan : Models.StaticStore.Plan
{
    public FreePlan()
    {
        Type = PlanType.Free;
        Product = ProductType.Free;
        Name = "Free";
        NameLocalizationKey = "planNameFree";
        DescriptionLocalizationKey = "planDescFree";

        UpgradeSortOrder = -1; // Always the lowest plan, cannot be upgraded to
        DisplaySortOrder = -1;

        PasswordManager = new FreePasswordManagerFeatures();
        SecretsManager = new FreeSecretsManagerFeatures();
    }

    private record FreeSecretsManagerFeatures : SecretsManagerPlanFeatures
    {
        public FreeSecretsManagerFeatures()
        {
            BaseSeats = 2;
            BaseServiceAccount = 3;
            MaxProjects = 3;
            MaxSeats = 2;
            MaxServiceAccounts = 3;

            AllowSeatAutoscale = false;
        }
    }

    private record FreePasswordManagerFeatures : PasswordManagerPlanFeatures
    {
        public FreePasswordManagerFeatures()
        {
            BaseSeats = 2;
            MaxCollections = 2;
            MaxSeats = 2;

            AllowSeatAutoscale = false;
        }
    }
}
