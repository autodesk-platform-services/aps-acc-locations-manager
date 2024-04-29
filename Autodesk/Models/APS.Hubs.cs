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

using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.DataManagement;
using Autodesk.DataManagement.Http;
using Autodesk.DataManagement.Model;

namespace Autodesk.Aps.Models
{
    public partial class APS
    {
        public async Task<IEnumerable<HubsData>> GetHubs(Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var hubs = await dataManagementClient.GetHubsAsync(accessToken: tokens.InternalToken, filterExtensionType: new List<string> { "hubs:autodesk.bim360:Account" });
            return hubs.Data;
        }

        public async Task<IEnumerable<ProjectsData>> GetProjects(string hubId, Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var projects = await dataManagementClient.GetHubProjectsAsync(hubId, accessToken: tokens.InternalToken);
            return projects.Data;
        }

        public async Task<Project> GetProject(string hubId, string projectId, Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var project = await dataManagementClient.GetProjectAsync(hubId, projectId, accessToken: tokens.InternalToken);
            return project;
        }

        public async Task<IEnumerable<TopFoldersData>> GetTopFolders(string hubId, string projectId, Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var folders = await dataManagementClient.GetProjectTopFoldersAsync(hubId, projectId, accessToken: tokens.InternalToken);
            return folders.Data;
        }

        public async Task<FolderContents> GetFolderContents(string projectId, string folderId, Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var contents = await dataManagementClient.GetFolderContentsAsync(projectId, folderId, accessToken: tokens.InternalToken);
            return contents;
        }

        public async Task<IEnumerable<VersionsData>> GetVersions(string projectId, string itemId, Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var versions = await dataManagementClient.GetItemVersionsAsync(projectId, itemId, accessToken: tokens.InternalToken);
            return versions.Data;
        }

        public async Task<VersionDetails> GetVersion(string projectId, string versionId, Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var version = await dataManagementClient.GetVersionAsync(projectId, versionId, accessToken: tokens.InternalToken);
            return version;
        }

        public async Task<RelationshipRefs> GetVersionRelationshipsRefs(string projectId, string versionId, Tokens tokens)
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);
            var relationshipRefs = await dataManagementClient.GetVersionRelationshipsRefsAsync(projectId, versionId, accessToken: tokens.InternalToken);
            return relationshipRefs;
        }
    }
}