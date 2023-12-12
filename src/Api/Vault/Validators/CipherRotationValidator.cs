﻿using Bit.Api.Auth.Validators;
using Bit.Api.Vault.Models.Request;
using Bit.Core;
using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.Services;
using Bit.Core.Vault.Entities;
using Bit.Core.Vault.Repositories;

namespace Bit.Api.Vault.Validators;

public class CipherRotationValidator : IRotationValidator<IEnumerable<CipherWithIdRequestModel>, IEnumerable<Cipher>>
{
    private readonly ICipherRepository _cipherRepository;
    private readonly ICurrentContext _currentContext;
    private readonly IFeatureService _featureService;

    private bool UseFlexibleCollections =>
        _featureService.IsEnabled(FeatureFlagKeys.FlexibleCollections, _currentContext);

    public CipherRotationValidator(ICipherRepository cipherRepository, ICurrentContext currentContext,
        IFeatureService featureService)
    {
        _cipherRepository = cipherRepository;
        _currentContext = currentContext;
        _featureService = featureService;

    }

    public async Task<IEnumerable<Cipher>> ValidateAsync(User user, IEnumerable<CipherWithIdRequestModel> ciphers)
    {
        var result = new List<Cipher>();
        if (ciphers == null || !ciphers.Any())
        {
            return result;
        }

        var existingCiphers = await _cipherRepository.GetManyByUserIdAsync(user.Id, UseFlexibleCollections);
        if (existingCiphers == null || !existingCiphers.Any())
        {
            return result;
        }

        foreach (var existing in existingCiphers)
        {
            var cipher = ciphers.FirstOrDefault(c => c.Id == existing.Id);
            if (cipher == null)
            {
                throw new BadRequestException("All existing ciphers must be included in the rotation.");
            }
            result.Add(cipher.ToCipher(existing));
        }
        return result;
    }
}
