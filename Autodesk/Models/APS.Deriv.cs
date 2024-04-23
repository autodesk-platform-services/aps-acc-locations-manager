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

using Autodesk.ModelDerivative;
using Autodesk.ModelDerivative.Model;

namespace Autodesk.Aps.Models
{
    public partial class APS
    {
        public async Task<Manifest> GetManifest(string urn, Tokens tokens)
        {
            var modelDerivativeClient = new ModelDerivativeClient(_sdkManager);
            var manifest = await modelDerivativeClient.GetManifestAsync(urn, accessToken: tokens.InternalToken);
            return manifest;
        }

        public async Task<string> GetDerivativeDownloadUrl(string derivativeUrn, string urn, Tokens tokens)
        {
            var modelDerivativeClient = new ModelDerivativeClient(_sdkManager);
            var response = await modelDerivativeClient.DerivativesApi.GetDerivativeUrlAsync(derivativeUrn, urn, accessToken: tokens.InternalToken);

            var cloudFrontPolicyName = "CloudFront-Policy";
            var cloudFrontKeyPairIdName = "CloudFront-Key-Pair-Id";
            var cloudFrontSignatureName = "CloudFront-Signature";

            var cloudFrontCookies = response.HttpResponse.Headers.GetValues("Set-Cookie");

            var cloudFrontPolicy = cloudFrontCookies.Where(value => value.Contains(cloudFrontPolicyName)).FirstOrDefault()?.Trim().Substring(cloudFrontPolicyName.Length + 1).Split(";").First();
            var cloudFrontKeyPairId = cloudFrontCookies.Where(value => value.Contains(cloudFrontKeyPairIdName)).FirstOrDefault()?.Trim().Substring(cloudFrontKeyPairIdName.Length + 1).Split(";").First();
            var cloudFrontSignature = cloudFrontCookies.Where(value => value.Contains(cloudFrontSignatureName)).FirstOrDefault()?.Trim().Substring(cloudFrontSignatureName.Length + 1).Split(";").First();

            var result = response.Content;
            var downloadURL = result.Url.ToString();
            var queryString =  "?Key-Pair-Id=" + cloudFrontKeyPairId + "&Signature=" + cloudFrontSignature + "&Policy=" + cloudFrontPolicy;
            downloadURL += queryString;

            return downloadURL;
        }
    }
}