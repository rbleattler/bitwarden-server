﻿using Bit.Api.AdminConsole.Models.Request;
using Bit.Api.Models.Response;
using Bit.Core.AdminConsole.Enums;
using Bit.Core.AdminConsole.Models.Api.Response;
using Bit.Core.AdminConsole.Repositories;
using Bit.Core.AdminConsole.Services;
using Bit.Core.Auth.Models.Business.Tokenables;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Tokens;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.AdminConsole.Controllers;

[Route("organizations/{orgId}/policies")]
[Authorize("Application")]
public class PoliciesController : Controller
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyService _policyService;
    private readonly IOrganizationService _organizationService;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IUserService _userService;
    private readonly ICurrentContext _currentContext;
    private readonly GlobalSettings _globalSettings;
    private readonly IDataProtector _organizationServiceDataProtector;
    private readonly IDataProtectorTokenFactory<OrgUserInviteTokenable> _orgUserInviteTokenDataFactory;

    public PoliciesController(
        IPolicyRepository policyRepository,
        IPolicyService policyService,
        IOrganizationService organizationService,
        IOrganizationUserRepository organizationUserRepository,
        IUserService userService,
        ICurrentContext currentContext,
        GlobalSettings globalSettings,
        IDataProtectionProvider dataProtectionProvider,
        IDataProtectorTokenFactory<OrgUserInviteTokenable> orgUserInviteTokenDataFactory)
    {
        _policyRepository = policyRepository;
        _policyService = policyService;
        _organizationService = organizationService;
        _organizationUserRepository = organizationUserRepository;
        _userService = userService;
        _currentContext = currentContext;
        _globalSettings = globalSettings;
        _organizationServiceDataProtector = dataProtectionProvider.CreateProtector(
            "OrganizationServiceDataProtector");

        _orgUserInviteTokenDataFactory = orgUserInviteTokenDataFactory;
    }

    [HttpGet("{type}")]
    public async Task<PolicyResponseModel> Get(string orgId, int type)
    {
        var orgIdGuid = new Guid(orgId);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }
        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(orgIdGuid, (PolicyType)type);
        if (policy == null)
        {
            throw new NotFoundException();
        }

        return new PolicyResponseModel(policy);
    }

    [HttpGet("")]
    public async Task<ListResponseModel<PolicyResponseModel>> Get(string orgId)
    {
        var orgIdGuid = new Guid(orgId);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var policies = await _policyRepository.GetManyByOrganizationIdAsync(orgIdGuid);
        var responses = policies.Select(p => new PolicyResponseModel(p));
        return new ListResponseModel<PolicyResponseModel>(responses);
    }

    [AllowAnonymous]
    [HttpGet("token")]
    public async Task<ListResponseModel<PolicyResponseModel>> GetByToken(Guid orgId, [FromQuery] string email,
        [FromQuery] string token, [FromQuery] Guid organizationUserId)
    {
        // TODO: PM-4142 - remove old token validation logic once 3 releases of backwards compatibility are complete
        var newTokenValid = OrgUserInviteTokenable.ValidateOrgUserInviteStringToken(
            _orgUserInviteTokenDataFactory, token, organizationUserId, email);

        var tokenValid = newTokenValid || CoreHelpers.UserInviteTokenIsValid(
            _organizationServiceDataProtector, token, email, organizationUserId, _globalSettings
        );

        if (!tokenValid)
        {
            throw new NotFoundException();
        }

        var orgUser = await _organizationUserRepository.GetByIdAsync(organizationUserId);
        if (orgUser == null || orgUser.OrganizationId != orgId)
        {
            throw new NotFoundException();
        }

        var policies = await _policyRepository.GetManyByOrganizationIdAsync(orgId);
        var responses = policies.Where(p => p.Enabled).Select(p => new PolicyResponseModel(p));
        return new ListResponseModel<PolicyResponseModel>(responses);
    }

    // TODO: PM-4097 - remove GetByInvitedUser once all clients are updated to use the GetMasterPasswordPolicy endpoint below
    [Obsolete("Deprecated API", false)]
    [AllowAnonymous]
    [HttpGet("invited-user")]
    public async Task<ListResponseModel<PolicyResponseModel>> GetByInvitedUser(Guid orgId, [FromQuery] Guid userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        var orgUsersByUserId = await _organizationUserRepository.GetManyByUserAsync(user.Id);
        var orgUser = orgUsersByUserId.SingleOrDefault(u => u.OrganizationId == orgId);
        if (orgUser == null)
        {
            throw new NotFoundException();
        }
        if (orgUser.Status != OrganizationUserStatusType.Invited)
        {
            throw new UnauthorizedAccessException();
        }

        var policies = await _policyRepository.GetManyByOrganizationIdAsync(orgId);
        var responses = policies.Where(p => p.Enabled).Select(p => new PolicyResponseModel(p));
        return new ListResponseModel<PolicyResponseModel>(responses);
    }

    [HttpGet("master-password")]
    public async Task<PolicyResponseModel> GetMasterPasswordPolicy(Guid orgId)
    {
        var userId = _userService.GetProperUserId(User).Value;

        var orgUser = await _organizationUserRepository.GetByOrganizationAsync(orgId, userId);

        if (orgUser == null)
        {
            throw new NotFoundException();
        }

        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(orgId, PolicyType.MasterPassword);

        if (policy == null || !policy.Enabled)
        {
            throw new NotFoundException();
        }

        return new PolicyResponseModel(policy);
    }

    [HttpPut("{type}")]
    public async Task<PolicyResponseModel> Put(string orgId, int type, [FromBody] PolicyRequestModel model)
    {
        var orgIdGuid = new Guid(orgId);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }
        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(new Guid(orgId), (PolicyType)type);
        if (policy == null)
        {
            policy = model.ToPolicy(orgIdGuid);
        }
        else
        {
            policy = model.ToPolicy(policy);
        }

        var userId = _userService.GetProperUserId(User);
        await _policyService.SaveAsync(policy, _userService, _organizationService, userId);
        return new PolicyResponseModel(policy);
    }
}
