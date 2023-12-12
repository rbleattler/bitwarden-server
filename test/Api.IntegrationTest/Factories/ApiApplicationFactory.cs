﻿using Bit.Core.Auth.Models.Api.Request.Accounts;
using Bit.IntegrationTestCommon.Factories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;

namespace Bit.Api.IntegrationTest.Factories;

public class ApiApplicationFactory : WebApplicationFactoryBase<Startup>
{
    private readonly IdentityApplicationFactory _identityApplicationFactory;
    private const string _connectionString = "DataSource=:memory:";

    public ApiApplicationFactory()
    {
        SqliteConnection = new SqliteConnection(_connectionString);
        SqliteConnection.Open();

        _identityApplicationFactory = new IdentityApplicationFactory();
        _identityApplicationFactory.SqliteConnection = SqliteConnection;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            // Remove scheduled background jobs to prevent errors in parallel test execution
            var jobService = services.First(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(Jobs.JobsHostedService));
            services.Remove(jobService);

            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.BackchannelHttpHandler = _identityApplicationFactory.Server.CreateHandler();
            });
        });
    }

    /// <summary>
    /// Helper for registering and logging in to a new account
    /// </summary>
    public async Task<(string Token, string RefreshToken)> LoginWithNewAccount(string email = "integration-test@bitwarden.com", string masterPasswordHash = "master_password_hash")
    {
        await _identityApplicationFactory.RegisterAsync(new RegisterRequestModel
        {
            Email = email,
            MasterPasswordHash = masterPasswordHash,
        });

        return await _identityApplicationFactory.TokenFromPasswordAsync(email, masterPasswordHash);
    }

    /// <summary>
    /// Helper for logging in to an account
    /// </summary>
    public async Task<(string Token, string RefreshToken)> LoginAsync(string email = "integration-test@bitwarden.com", string masterPasswordHash = "master_password_hash")
    {
        return await _identityApplicationFactory.TokenFromPasswordAsync(email, masterPasswordHash);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.Dispose();
    }
}
