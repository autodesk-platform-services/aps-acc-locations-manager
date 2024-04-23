/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by APS Partner Development
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

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Autodesk.Aps.Models;
using Autodesk.Aps.Libs;

namespace Autodesk.Aps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class AccController : OAuthBasedController
    {
        private readonly ILogger<AccController> _logger;

        public AccController(ILogger<AccController> logger, APS aps) : base(aps)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("projects/{projectId}/locations")]
        public async Task<IActionResult> GetLocationsAsync(string projectId)
        {
            var locations = await AccDataUtil.GetLocationsAsync(projectId, this._tokens);

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
        [Route("projects/{projectId}/locations")]
        public async Task<IActionResult> CreateLocationAsync(string projectId, [FromBody] JObject payload)
        {
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
                var result = await AccDataUtil.CreateLocationsNodeAsync(projectId, location, this._tokens, targetNodeId, insertOption);
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
        [Route("projects/{projectId}/locations/{nodeId}")]
        public async Task<IActionResult> UpdateLocationAsync(string projectId, string nodeId, [FromBody] JObject payload)
        {
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
                var result = await AccDataUtil.PatchLocationsNodeAsync(projectId, nodeId, data, this._tokens);
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
        [Route("projects/{projectId}/locations/{nodeId}")]
        public async Task<IActionResult> DeleteLocationAsync(string projectId, string nodeId)
        {
            try
            {
                var result = await AccDataUtil.DeleteLocationsNodeAsync(projectId, nodeId, this._tokens);
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
        [Route("projects/{projectId}/locations:destroy")]
        public async Task<IActionResult> DestroyLocationTreeAsync(string projectId)
        {
            try
            {
                var result = await AccDataUtil.DestroyLocationTreeAsync(projectId, this._tokens);
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
        [Route("projects/{projectId}/locations:import")]
        public async Task<IActionResult> ImportLocations([FromRoute] string projectId, [FromBody] JObject payload)
        {
            string urn = ((dynamic)payload).urn;
            if (string.IsNullOrWhiteSpace(urn))
                return BadRequest(new
                {
                    title = "Invalid Data",
                    detail = "Missing field `urn` in the request body"
                });

            byte[] data = Convert.FromBase64String(urn.Replace('_', '/'));
            string versionId = Encoding.UTF8.GetString(data);

            var locations = await AccDataUtil.ImportLocationsFromModelPropsAsync(projectId, versionId, this._tokens, this._aps);

            return Ok(locations);
        }
    }
}