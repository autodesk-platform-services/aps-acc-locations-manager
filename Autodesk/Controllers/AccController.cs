/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
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
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Autodesk.Forge;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.Forge.Models;
using System.Web;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Autodesk.Forge.Libs;

namespace Autodesk.Forge.Controllers
{
    public partial class AccController : ControllerBase
    {
        private const string BASE_URL = "https://developer.api.autodesk.com";

        [HttpGet]
        [Route("api/forge/acc/projects/{projectId}/locations")]
        public async Task<IActionResult> GetLocationsAsync(string projectId)
        {
            Credentials credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (credentials == null)
            {
                throw new InvalidOperationException("Failed to refresh access token");
            }

            var locations = await AccDataUtil.GetLocationsAsync(credentials.TokenInternal, projectId);

            var data = locations.Results.OrderBy(node => node.Order)
            .Select(node => new
            {
                id = $"lbs_{node.Id}",
                text = node.Name,
                type = node.Type.ToLower(),
                barcode = node.Barcode,
                parent = string.IsNullOrWhiteSpace(node.ParentId) ? "#" : $"lbs_{node.ParentId}"
            });

            return Ok(data);
        }

        [HttpPost]
        [Route("api/forge/acc/projects/{projectId}/locations")]
        public async Task<IActionResult> CreateLocationAsync(string projectId, [FromBody] JObject payload)
        {
            Credentials credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (credentials == null)
            {
                throw new InvalidOperationException("Failed to refresh access token");
            }

            string name = ((dynamic)payload).name;
            string barcode = ((dynamic)payload).barcode;
            string parentId = ((dynamic)payload).parentId;
            string targetNodeId = ((dynamic)payload).targetNodeId;
            string insertOption = ((dynamic)payload).insertOption;

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new
                {
                    title = "Invalid Data",
                    detail = "Missing field `name` in the request body"
                });

            if (string.IsNullOrWhiteSpace(parentId))
                return BadRequest(new
                {
                    title = "Invalid Data",
                    detail = "Missing field `parentId` in the request body"
                });

            var location = new Location
            {
                Name = name,
                Type = "Area",
                ParentId = parentId
            };

            if (!string.IsNullOrWhiteSpace(barcode))
                location.Barcode = barcode;

            try
            {
                var result = await AccDataUtil.CreateLocationsNodeAsync(credentials.TokenInternal, projectId, location, targetNodeId, insertOption);
                if (result == null)
                    throw new InvalidOperationException("Failed to create new node");

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch]
        [Route("api/forge/acc/projects/{projectId}/locations/{nodeId}")]
        public async Task<IActionResult> UpdateLocationAsync(string projectId, string nodeId, [FromBody] JObject payload)
        {
            Credentials credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (credentials == null)
            {
                throw new InvalidOperationException("Failed to refresh access token");
            }

            string name = ((dynamic)payload).name;
            string barcode = ((dynamic)payload).barcode;

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(barcode))
                return BadRequest(new
                {
                    title = "Invalid Data",
                    detail = "Both `name` & `barcode` is missing in the request body. At least one of these fields must be included in the payload."
                });

            var data = new JObject();

            if (!string.IsNullOrWhiteSpace(name))
                data.Add("name", name);

            if (!string.IsNullOrWhiteSpace(barcode))
                data.Add("barcode", barcode);

            try
            {
                var result = await AccDataUtil.PatchLocationsNodeAsync(credentials.TokenInternal, projectId, nodeId, data);
                if (result == null)
                    throw new InvalidOperationException($"Failed to update the node `{nodeId}`");

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("api/forge/acc/projects/{projectId}/locations/{nodeId}")]
        public async Task<IActionResult> DeleteLocationAsync(string projectId, string nodeId)
        {
            Credentials credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (credentials == null)
            {
                throw new InvalidOperationException("Failed to refresh access token");
            }

            try
            {
                var result = await AccDataUtil.DeleteLocationsNodeAsync(credentials.TokenInternal, projectId, nodeId);
                if (result == false)
                    throw new InvalidOperationException($"Failed to delete the node `{nodeId}`");

                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("api/forge/acc/projects/{projectId}/locations:destroy")]
        public async Task<IActionResult> DestroyLocationTreeAsync(string projectId, string nodeId)
        {
            Credentials credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (credentials == null)
            {
                throw new InvalidOperationException("Failed to refresh access token");
            }

            try
            {
                var result = await AccDataUtil.DestroyLocationTreeAsync(credentials.TokenInternal, projectId);
                if (result == false)
                    throw new InvalidOperationException("Failed to destroy the tree");

                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/forge/acc/projects/{projectId}/locations:import")]
        public async Task<IActionResult> ImportLocations([FromRoute] string projectId, [FromBody] JObject payload)
        {
            Credentials credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (credentials == null)
            {
                throw new InvalidOperationException("Failed to refresh access token");
            }

            string urn = ((dynamic)payload).urn;
            if (string.IsNullOrWhiteSpace(urn))
                return BadRequest(new
                {
                    title = "Invalid Data",
                    detail = "Missing field `urn` in the request body"
                });

            byte[] data = Convert.FromBase64String(urn.Replace('_', '/'));
            string versionId = Encoding.UTF8.GetString(data);

            var locations = await AccDataUtil.ImportLocationsFromModelPropsAsync(credentials.TokenInternal, projectId, versionId);

            return Ok(locations);
        }
    }
}