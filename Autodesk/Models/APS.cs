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
using Autodesk.SDKManager;
using Autodesk.Authentication.Model;
using System.Collections.Generic;

namespace Autodesk.Aps.Models
{
    public class Tokens
    {
        public string InternalToken;
        public string PublicToken;
        public string RefreshToken;
        public DateTime ExpiresAt;
    }

    public partial class APS
    {
        private readonly Autodesk.SDKManager.SDKManager _sdkManager;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _callbackUri;
        private readonly List<Scopes> InternalTokenScopes = new List<Scopes> { Scopes.DataRead, Scopes.DataCreate, Scopes.DataWrite, Scopes.ViewablesRead };
        private readonly List<Scopes> PublicTokenScopes = new List<Scopes> { Scopes.ViewablesRead };

        public APS(string clientId, string clientSecret, string callbackUri)
        {
            _sdkManager = SdkManagerBuilder.Create().Build();
            _clientId = clientId;
            _clientSecret = clientSecret;
            _callbackUri = callbackUri;
        }

        internal string ClientId {
            get {
                return _clientId;
            }
        }
    }
}