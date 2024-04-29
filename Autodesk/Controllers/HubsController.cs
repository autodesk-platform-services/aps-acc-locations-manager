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
using Autodesk.DataManagement.Model;

namespace Autodesk.Aps.Controllers
{
    [ApiController]
    [Route("api/datamanagement")]
    public class HubsController : OAuthBasedController
    {
        private readonly ILogger<HubsController> _logger;

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes).Replace("/", "_");
        }

        public HubsController(ILogger<HubsController> logger, APS aps) : base(aps)
        {
            _logger = logger;
        }

        [HttpGet()]
        public async Task<IList<JsTreeNode>> GetTreeNodeAsync(string id)
        {
            var nodes = new List<JsTreeNode>();

            if (id == "#") // root
                return await GetHubsAsync();
            else
            {
                string[] idParams = id.Split('/');
                string resource = idParams[idParams.Length - 2];

                switch (resource)
                {
                    case "hubs": // hubs node selected/expanded, show projects
                        return await GetProjectsAsync(id);
                    case "projects": // projects node selected/expanded, show root folder contents
                        return await GetProjectContents(id);
                    case "folders": // folders node selected/expanded, show folder contents
                        return await GetFolderContents(id);
                    case "items":
                        return await GetItemVersions(id);
                }
            }

            return nodes;
        }

        private async Task<List<JsTreeNode>> GetHubsAsync()
        {
            var nodes = new List<JsTreeNode>();
            var hubs = await _aps.GetHubs(this._tokens);
            foreach (HubsData hub in hubs)
            {
                string nodeType = "hubs";
                switch (hub.Attributes.Extension.Type)
                {
                    case "hubs:autodesk.core:Hub":
                        nodeType = "unsupported";
                        break;
                    case "hubs:autodesk.a360:PersonalHub":
                        nodeType = "unsupported";
                        break;
                    case "hubs:autodesk.bim360:Account":
                        nodeType = "bim360Hubs";
                        break;
                }

                if (nodeType == "unsupported") continue;

                // create a tree node with the values
                var hubNode = new JsTreeNode(hub.Links.Self.Href, hub.Attributes.Name, nodeType, !(nodeType == "unsupported"));
                nodes.Add(hubNode);
            }
            return nodes;
        }

        private async Task<List<JsTreeNode>> GetProjectsAsync(string href)
        {
            // extract the hubId from the href
            string[] idParams = href.Split('/');
            string hubId = idParams[idParams.Length - 1];

            var nodes = new List<JsTreeNode>();
            var projects = await _aps.GetProjects(hubId, this._tokens);

            foreach (ProjectsData project in projects)
            {
                // check the type of the project to show an icon
                string nodeType = "projects";
                switch (project.Attributes.Extension.Type)
                {
                    case "projects:autodesk.core:Project":
                        nodeType = "a360projects";
                        break;
                    case "projects:autodesk.bim360:Project":
                        nodeType = "bim360projects";
                        break;
                }

                if (nodeType == "a360projects") continue;

                // create a treenode with the values
                var projectNode = new JsTreeNode(project.Links.Self.Href, project.Attributes.Name, nodeType, true);
                nodes.Add(projectNode);
            }

            return nodes;
        }

        private async Task<List<JsTreeNode>> GetProjectContents(string href)
        {
            // extract the hubId & projectId from the href
            string[] idParams = href.Split('/');
            string hubId = idParams[idParams.Length - 3];
            string projectId = idParams[idParams.Length - 1];

            var nodes = new List<JsTreeNode>();
            var folders = await _aps.GetTopFolders(hubId, projectId, this._tokens);
            foreach (TopFoldersData folder in folders)
            {
                if (folder.Attributes.Extension.Data.FolderType.ToLower() == "plan") continue;

                nodes.Add(new JsTreeNode(folder.Links.Self.Href, folder.Attributes.DisplayName, "folders", true));
            }

            return nodes;
        }

        private async Task<List<JsTreeNode>> GetFolderContents(string href)
        {
            // extract the projectId & folderId from the href
            string[] idParams = href.Split('/');
            string folderId = idParams[idParams.Length - 1];
            string projectId = idParams[idParams.Length - 3];

            var nodes = new List<JsTreeNode>();
            var folderContents = await _aps.GetFolderContents(projectId, folderId, this._tokens);
            var folderData = folderContents.Data;
            var folderIncluded = folderContents.Included;

            foreach (FolderContentsData folderContentItem in folderData)
            {
                string extension = folderContentItem.Attributes.Extension.Type;
                // if the type is items:autodesk.bim360:Document we need some manipulation...
                // if (extension.Equals("items:autodesk.bim360:Document"))
                // {
                //     // as this is a DOCUMENT, lets interate the FOLDER INCLUDED to get the name (known issue)
                //     foreach (var includedItem in folderIncluded)
                //     {
                //         // check if the id match...
                //         if (includedItem.Relationships.Item.Data.Id.IndexOf(folderContentItem.Id) != -1)
                //         {
                //             // found it! now we need to go back on the FOLDER DATA to get the respective FILE for this DOCUMENT
                //             foreach (var folderContentItem1 in folderData)
                //             {
                //                 if (folderContentItem1.Attributes.Extension.Type.IndexOf("File") == -1) continue; // skip if type is NOT File

                //                 // check if the sourceFileName match...
                //                 if (folderContentItem1.Attributes.SourceFileName == includedItem.Value.attributes.extension.data.sourceFileName)
                //                 {
                //                     // ready!

                //                     // let's return for the jsTree with a special id:
                //                     // itemUrn|versionUrn|viewableId|versionNumber
                //                     // itemUrn: used as target_urn to get document issues
                //                     // versionUrn: used to launch the Viewer
                //                     // viewableId: which viewable should be loaded on the Viewer
                //                     // versionNumber: version number of the document
                //                     // this information will be extracted when the user click on the tree node, see APSTree.js:136 (activate_node.jstree event handler)
                //                     string treeId = string.Format("{0}|{1}|{2}|{3}",
                //                         folderContentItem.Id, // item urn
                //                         Base64Encode(folderContentItem1.Relationships.Tip.Data.Id), // version urn
                //                         includedItem.Attributes.Extension.Data.ViewableId, // viewableID
                //                         includedItem.Attributes.VersionNumber
                //                     );
                //                     nodes.Add(new JsTreeNode(treeId, HttpUtility.UrlEncode(includedItem.Attributes.Name), "bim360documents", false));
                //                 }
                //             }
                //         }
                //     }
                // }
                // else
                // {
                nodes.Add(new JsTreeNode(folderContentItem.Links.Self.Href, folderContentItem.Attributes.DisplayName, folderContentItem.Type, true));
                // }
            }

            return nodes;
        }

        private async Task<List<JsTreeNode>> GetItemVersions(string href)
        {
            // extract the projectId & itemId from the href
            string[] idParams = href.Split('/');
            string itemId = idParams[idParams.Length - 1];
            string projectId = idParams[idParams.Length - 3];

            var nodes = new List<JsTreeNode>();
            var versions = await _aps.GetVersions(projectId, itemId, this._tokens);

            foreach (VersionsData version in versions)
            {
                DateTime versionDate = DateTime.Parse(version.Attributes.LastModifiedTime);
                string verNum = version.Id.Split("=")[1];
                string userName = version.Attributes.LastModifiedUserName;

                string urn = Base64Encode(version.Id);
                var node = new JsTreeNode(
                    urn,
                    string.Format("v{0}: {1} by {2}", verNum, versionDate.ToString("dd/MM/yy HH:mm:ss"), userName),
                    "versions",
                    false
                );

                nodes.Add(node);
            }

            return nodes;
        }
    }
}