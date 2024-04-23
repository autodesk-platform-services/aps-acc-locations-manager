/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Developer Advocate and Support
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;

namespace Autodesk.Aps.Models
{
    public partial class APS
    {
        public string GetAuthorizationURL()
        {
            var authenticationClient = new AuthenticationClient(_sdkManager);
            return authenticationClient.Authorize(_clientId, ResponseType.Code, _callbackUri, InternalTokenScopes);
        }

        public async Task<string> GetTwoLeggedToken(List<Scopes> scopes)
        {
            var authenticationClient = new AuthenticationClient(_sdkManager);
            var twoLeggedAuth = await authenticationClient.GetTwoLeggedTokenAsync(_clientId, _clientSecret, scopes);
            return twoLeggedAuth.AccessToken;
        }

        public async Task<Tokens> GenerateTokens(string code)
        {
            var authenticationClient = new AuthenticationClient(_sdkManager);
            var internalAuth = await authenticationClient.GetThreeLeggedTokenAsync(_clientId, _clientSecret, code, _callbackUri);
            var publicAuth = await authenticationClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth.RefreshToken, PublicTokenScopes);
            return new Tokens
            {
                PublicToken = publicAuth.AccessToken,
                InternalToken = internalAuth.AccessToken,
                RefreshToken = publicAuth._RefreshToken,
                ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
            };
        }

        public async Task<Tokens> RefreshTokens(Tokens tokens)
        {
            var authenticationClient = new AuthenticationClient(_sdkManager);
            var internalAuth = await authenticationClient.GetRefreshTokenAsync(_clientId, _clientSecret, tokens.RefreshToken, InternalTokenScopes);
            var publicAuth = await authenticationClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth._RefreshToken, PublicTokenScopes);
            return new Tokens
            {
                PublicToken = publicAuth.AccessToken,
                InternalToken = internalAuth.AccessToken,
                RefreshToken = publicAuth._RefreshToken,
                ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
            };
        }

        public async Task<UserInfo> GetUserProfile(Tokens tokens)
        {
            var authenticationClient = new AuthenticationClient(_sdkManager);
            UserInfo userInfo = await authenticationClient.GetUserInfoAsync(tokens.InternalToken);
            return userInfo;
        }
    }
}