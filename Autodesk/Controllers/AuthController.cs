/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Developer Advocacy and Support
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

using Microsoft.AspNetCore.Mvc;
using Autodesk.Aps.Models;

namespace Autodesk.Aps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly APS _aps;

        public AuthController(ILogger<AuthController> logger, APS aps)
        {
            _logger = logger;
            _aps = aps;
        }

        public static async Task<Tokens> PrepareTokens(HttpRequest request, HttpResponse response, APS aps)
        {
            if (!request.Cookies.ContainsKey("internal_token"))
            {
                return null;
            }
            var tokens = new Tokens
            {
                PublicToken = request.Cookies["public_token"],
                InternalToken = request.Cookies["internal_token"],
                RefreshToken = request.Cookies["refresh_token"],
                ExpiresAt = DateTime.Parse(request.Cookies["expires_at"])
            };
            if (tokens.ExpiresAt < DateTime.Now.ToUniversalTime())
            {
                tokens = await aps.RefreshTokens(tokens);
                response.Cookies.Append("public_token", tokens.PublicToken);
                response.Cookies.Append("internal_token", tokens.InternalToken);
                response.Cookies.Append("refresh_token", tokens.RefreshToken);
                response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
            }
            return tokens;
        }

        [HttpGet("login")]
        public ActionResult Login()
        {
            var redirectUri = _aps.GetAuthorizationURL();
            return Redirect(redirectUri);
        }

        [HttpGet("logout")]
        public ActionResult Logout()
        {
            Response.Cookies.Delete("public_token");
            Response.Cookies.Delete("internal_token");
            Response.Cookies.Delete("refresh_token");
            Response.Cookies.Delete("expires_at");
            return Redirect("/");
        }

        [HttpGet("callback")]
        public async Task<ActionResult> Callback(string code)
        {
            var tokens = await _aps.GenerateTokens(code);
            Response.Cookies.Append("public_token", tokens.PublicToken);
            Response.Cookies.Append("internal_token", tokens.InternalToken);
            Response.Cookies.Append("refresh_token", tokens.RefreshToken);
            Response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
            return Redirect("/");
        }

        [HttpGet("profile")]
        public async Task<dynamic> GetProfile()
        {
            var tokens = await PrepareTokens(Request, Response, _aps);
            if (tokens == null)
            {
                return Unauthorized();
            }
            var profile = await _aps.GetUserProfile(tokens);
            return new
            {
                name = profile.Name,
                picture = profile.Picture
            };
        }

        [HttpGet("token")]
        public async Task<dynamic> GetPublicToken()
        {
            var tokens = await PrepareTokens(Request, Response, _aps);
            if (tokens == null)
            {
                return Unauthorized();
            }
            return new
            {
                access_token = tokens.PublicToken,
                token_type = "Bearer",
                expires_in = Math.Floor((tokens.ExpiresAt - DateTime.Now.ToUniversalTime()).TotalSeconds)
            };
        }

        [HttpGet]
        [Route("clientid")] // see Web.Config APS_CALLBACK_URL variable
        public dynamic GetClientID()
        {
            return new { id = _aps.ClientId };
        }
    }
}
