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

using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.Forge;
using Autodesk.Forge.Model;
using Autodesk.Aps;
using System.IO;
using System.Text.RegularExpressions;
using Autodesk.Aps.Models;
using System.Net;
using Newtonsoft.Json.Serialization;

namespace Autodesk.Aps.Libs
{
    public class AccDataUtil
    {
        private const string BASE_URL = "https://developer.api.autodesk.com";

        public static string SerializeJsonObject(object? data)
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            return JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });
        }

        public static async Task<string> GetContainerIdAsync(string credentials, string accountId, string projectId, ContainerType type)
        {
            ProjectsApi projectsApi = new ProjectsApi();
            projectsApi.Configuration.AccessToken = credentials;
            var project = await projectsApi.GetProjectAsync(accountId, projectId);
            var relationships = project.data.relationships;
            string containerId = string.Empty;

            var result = relationships.Dictionary;

            foreach (var relation in result)
            {
                string name = relation.Key;
                if (name != type.Value)
                    continue;

                var data = relation.Value.data;
                if (data == null || !data.type.Contains(type.Value))
                    continue;

                containerId = data.id;
            }

            return containerId;
        }

        public async static Task<dynamic> GetItemAsync(string projectId, string itemId, string credentials)
        {
            var itemsApi = new ItemsApi();
            itemsApi.Configuration.AccessToken = credentials;

            var itemRes = await itemsApi.GetItemAsync(projectId, itemId);
            return itemRes;
        }

        private async static Task<dynamic> GetVersionRefDerivativeUrnAsync(string projectId, string versionId, string credentials)
        {
            var versionApi = new VersionsApi();
            versionApi.Configuration.AccessToken = credentials;

            var relationshipRefs = await versionApi.GetVersionRelationshipsRefsAsync(projectId, versionId);
            if (relationshipRefs == null && relationshipRefs.included == null) return null;

            var includedData = new DynamicDictionaryItems(relationshipRefs.included);

            if (includedData.Count() > 0)
            {
                var refData = includedData.Cast<KeyValuePair<string, dynamic>>()
                    .FirstOrDefault(d => d.Value != null &&
                        d.Value.type == "versions" &&
                        d.Value.attributes.extension.type == "versions:autodesk.bim360:File");

                if (!refData.Equals(default(KeyValuePair<string, dynamic>)))
                {
                    return refData.Value.relationships.derivatives.data.id;
                }

                return null;
            }

            return null;
        }

        public async static Task<dynamic> GetVersionDerivativeUrnAsync(string projectId, string versionId, string credentials)
        {
            var versionsApi = new VersionsApi();
            versionsApi.Configuration.AccessToken = credentials;

            var version = await versionsApi.GetVersionAsync(projectId, versionId);

            string versionDerivativeId = null;

            var attributesData = new DynamicDictionaryItems(version.data.attributes.extension.data);
            var checkViewableGuid = attributesData.Cast<KeyValuePair<string, dynamic>>().Select(d => d.Key).Any(d => d == "viewableGuid");

            if (version.data.attributes.extension.data != null && checkViewableGuid && !string.IsNullOrWhiteSpace(version.data.attributes.extension.data.viewableGuid))
            {
                versionDerivativeId = await GetVersionRefDerivativeUrnAsync(projectId, versionId, credentials);
            }
            else
            {
                versionDerivativeId = (version.data.relationships != null && version.data.relationships.derivatives != null ? version.data.relationships.derivatives.data.id : null);
            }

            return versionDerivativeId;
        }

        public static async Task<PaginatedLocations> GetLocationsAsync(string credentials, string projectId)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/locations/v2/projects/{project_id}/trees/{tree_id}/nodes", RestSharp.Method.Get);
            request.AddParameter("project_id", projectId, ParameterType.UrlSegment);
            request.AddParameter("tree_id", "default", ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            RestResponse locsResponse = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<PaginatedLocations>(locsResponse.Content);
            return result;
        }

        public static async Task<Location> GetLocationsRootAsync(string credentials, string projectId)
        {
            var locations = await GetLocationsAsync(credentials, projectId);
            return locations.Results.FirstOrDefault();
        }

        public static async Task<List<Location>> GetLocationsFirstTierAsync(string credentials, string projectId)
        {
            var locations = await GetLocationsAsync(credentials, projectId);
            var root = locations.Results.First();
            var firstTierNodes = locations.Results.Where(location => location.ParentId == root.Id).ToList();
            return firstTierNodes;
        }

        public static async Task<bool> DestroyLocationTreeAsync(string credentials, string projectId)
        {
            var firstTierNodes = await AccDataUtil.GetLocationsFirstTierAsync(credentials, projectId);
            foreach (var location in firstTierNodes)
            {
                var result = await AccDataUtil.DeleteLocationsNodeAsync(credentials, projectId, location.Id);
            }

            return true;
        }

        public static async Task<Location> CreateLocationsNodeAsync(string credentials, string projectId, Location data, string targetNodeId, string insertOption = "After")
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/locations/v2/projects/{project_id}/trees/{tree_id}/nodes", RestSharp.Method.Post);
            request.AddParameter("project_id", projectId, ParameterType.UrlSegment);
            request.AddParameter("tree_id", "default", ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            if (!string.IsNullOrWhiteSpace(targetNodeId))
            {
                request.AddQueryParameter("targetNodeId", targetNodeId);

                if (!string.IsNullOrWhiteSpace(insertOption))
                    request.AddQueryParameter("insertOption", insertOption);
            }

            var body = AccDataUtil.SerializeJsonObject(data);
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            RestResponse response = await client.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var result = JsonConvert.DeserializeObject<Location>(response.Content);
                return result;
            }

            return null;
        }

        public static async Task<Location> PatchLocationsNodeAsync(string credentials, string projectId, string nodeId, JObject data)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/locations/v2/projects/{project_id}/trees/{tree_id}/nodes/{node_id}", RestSharp.Method.Patch);
            request.AddParameter("project_id", projectId, ParameterType.UrlSegment);
            request.AddParameter("tree_id", "default", ParameterType.UrlSegment);
            request.AddParameter("node_id", nodeId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            var body = AccDataUtil.SerializeJsonObject(data);
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            RestResponse response = await client.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = JsonConvert.DeserializeObject<Location>(response.Content);
                return result;
            }

            return null;
        }

        public static async Task<bool> DeleteLocationsNodeAsync(string credentials, string projectId, string nodeId)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/locations/v2/projects/{project_id}/trees/{tree_id}/nodes/{node_id}", RestSharp.Method.Delete);
            request.AddParameter("project_id", projectId, ParameterType.UrlSegment);
            request.AddParameter("tree_id", "default", ParameterType.UrlSegment);
            request.AddParameter("node_id", nodeId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            RestResponse response = await client.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return true;

            return false;
        }

        public static async Task<dynamic> BuildPropertyIndexesAsync(string credentials, string projectId, List<string> versionIds)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/index/v2/projects/{project_id}/indexes:batchStatus", RestSharp.Method.Post);
            request.AddParameter("project_id", projectId.Replace("b.", string.Empty), ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            var data = versionIds.Select(versionId => new
            {
                versionUrn = versionId
            });

            request.AddJsonBody(new
            {
                versions = data
            });

            RestResponse indexingResponse = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<dynamic>(indexingResponse.Content);
            return result;
        }

        public static async Task<dynamic> GetPropertyIndexesStatusAsync(string credentials, string projectId, string indexId)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/index/v2/projects/{project_id}/indexes/{index_id}", RestSharp.Method.Get);
            request.AddParameter("project_id", projectId.Replace("b.", string.Empty), ParameterType.UrlSegment);
            request.AddParameter("index_id", indexId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            RestResponse response = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            return result;
        }

        public static async Task<dynamic> GetPropertyIndexesManifestAsync(string credentials, string projectId, string indexId)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/index/v2/projects/{project_id}/indexes/{index_id}/manifest", RestSharp.Method.Get);
            request.AddParameter("project_id", projectId.Replace("b.", string.Empty), ParameterType.UrlSegment);
            request.AddParameter("index_id", indexId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            RestResponse response = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            return result;
        }

        public static async Task<dynamic> BuildPropertyQueryAsync(string credentials, string projectId, string indexId, JObject data)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/index/v2/projects/{project_id}/indexes/{index_id}/queries", RestSharp.Method.Post);
            request.AddParameter("project_id", projectId.Replace("b.", string.Empty), ParameterType.UrlSegment);
            request.AddParameter("index_id", indexId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);
            //request.AddJsonBody(data);

            var body = AccDataUtil.SerializeJsonObject(data);
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            RestResponse response = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            return result;
        }

        public static async Task<dynamic> GetPropertyQueryStatusAsync(string credentials, string projectId, string indexId, string queryId)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/index/v2/projects/{project_id}/indexes/{index_id}/queries/{query_id}", RestSharp.Method.Get);
            request.AddParameter("project_id", projectId.Replace("b.", string.Empty), ParameterType.UrlSegment);
            request.AddParameter("index_id", indexId, ParameterType.UrlSegment);
            request.AddParameter("query_id", queryId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            RestResponse response = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            return result;
        }

        public static List<JObject> ParseLineDelimitedJson(string content)
        {
            var data = new List<JObject>();
            using (var jsonTextReader = new JsonTextReader(new StringReader(content)))
            {
                jsonTextReader.SupportMultipleContent = true;
                var jsonSerializer = new JsonSerializer();
                while (jsonTextReader.Read())
                {
                    var item = jsonSerializer.Deserialize<JObject>(jsonTextReader);
                    data.Add(item);
                }
            }
            return data;
        }

        public static async Task<dynamic> GetPropertyFieldsAsync(string credentials, string projectId, string indexId)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/index/v2/projects/{project_id}/indexes/{index_id}/fields", RestSharp.Method.Get);
            request.AddParameter("project_id", projectId.Replace("b.", string.Empty), ParameterType.UrlSegment);
            request.AddParameter("index_id", indexId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            RestResponse response = await client.ExecuteAsync(request);
            var data = ParseLineDelimitedJson(response.Content);
            return data;
        }

        public static async Task<List<JObject>> GetPropertyQueryResultsAsync(string credentials, string projectId, string indexId, string queryId)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = new RestRequest("/construction/index/v2/projects/{project_id}/indexes/{index_id}/queries/{query_id}/properties", RestSharp.Method.Get);
            request.AddParameter("project_id", projectId.Replace("b.", string.Empty), ParameterType.UrlSegment);
            request.AddParameter("index_id", indexId, ParameterType.UrlSegment);
            request.AddParameter("query_id", queryId, ParameterType.UrlSegment);
            request.AddHeader("Authorization", "Bearer " + credentials);

            RestResponse response = await client.ExecuteAsync(request);
            var data = ParseLineDelimitedJson(response.Content);
            return data;
        }

        public static async Task<List<Level>> GetLevelsFromAecModelData(string credentials, string urn)
        {
            var derivativeApi = new DerivativesApi();
            string aecModelDataUrn = string.Empty;
            var data = await derivativeApi.GetManifestAsync(urn);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data.ToString());

            foreach (var derivative in result.derivatives)
            {
                if ((((dynamic)derivative).outputType != "svf") && (((dynamic)derivative).outputType != "svf2")) continue;

                foreach (var derivativeChild in ((dynamic)derivative).children)
                {
                    if (((dynamic)derivativeChild).role != "Autodesk.AEC.ModelData") continue;

                    aecModelDataUrn = ((dynamic)derivativeChild).urn;
                    break;
                }

                if (!string.IsNullOrWhiteSpace(aecModelDataUrn))
                    break;
            }

            if (string.IsNullOrWhiteSpace(aecModelDataUrn))
                throw new InvalidDataException($"No AEC model data found for this urn `{urn}`");

            System.IO.MemoryStream stream = await derivativeApi.GetDerivativeManifestAsync(urn, aecModelDataUrn);
            if (stream == null)
                throw new InvalidOperationException("Failed to download AecModelData");

            stream.Seek(0, SeekOrigin.Begin);

            JObject aecdata;
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                aecdata = serializer.Deserialize<JObject>(jsonTextReader);
            }

            if (aecdata == null)
                throw new InvalidOperationException("Failed to process AecModelData");

            var levelJsonToken = aecdata.GetValue("levels");
            var levelData = levelJsonToken.ToObject<List<AecLevel>>();

            var filteredLevels = levelData.Where(lvl => lvl.Extension != null)
                    .Where(lvl => lvl.Extension.BuildingStory == true)
                    .ToList();

            Func<AecLevel, double> getProjectElevation = default(Func<AecLevel, double>);
            getProjectElevation = level =>
            {
                return level.Extension.ProjectElevation.HasValue ? level.Extension.ProjectElevation.Value : level.Elevation;
            };

            var levels = new List<Level>();
            double zOffsetHack = 1.0 / 12.0;
            for (var i = 0; i < filteredLevels.Count; i++)
            {
                var level = filteredLevels[i];
                double nextElevation;
                if (i + 1 < filteredLevels.Count)
                {
                    var nextLevel = filteredLevels[i + 1];
                    nextElevation = getProjectElevation(nextLevel);
                }
                else
                {
                    var topLevel = filteredLevels[filteredLevels.Count - 1];
                    var topElevation = getProjectElevation(topLevel);
                    nextElevation = topElevation + topLevel.Height;
                }

                levels.Add(new Level
                {
                    Index = i + 1,
                    Guid = level.Guid,
                    Name = level.Name,
                    ZMin = getProjectElevation(level) - zOffsetHack,
                    ZMax = nextElevation,
                });
            }

            return levels;
        }

        public static async Task<dynamic> QueryRoomsPropertiesAsync(string accessToken, string projectId, string indexId, string roomCategoryName = "'Revit Rooms'")
        {
            List<JObject> propFields = await GetPropertyFieldsAsync(accessToken, projectId, indexId);
            dynamic levelField = propFields.FirstOrDefault(field => ((dynamic)field).name.ToString() == "Level" && ((dynamic)field).category.ToString() == "Constraints");

            string levelFieldKeyValue = levelField.key;
            string levelFieldKeyName = levelField.name;
            string levelFieldKey = $"s.props.{levelFieldKeyValue}";

            var data = new JObject(
                new JProperty("query",
                    new JObject(
                        new JProperty("$and",
                            new JArray {
                                new JObject(
                                    new JProperty("$eq",
                                        new JArray
                                        {
                                            new JValue("s.props.p5eddc473"),
                                            new JValue(roomCategoryName)
                                        }
                                    )
                                ),
                                new JObject(
                                    new JProperty("$gt",
                                        new JArray
                                        {
                                            new JObject(
                                                new JProperty("$count",
                                                    new JValue("s.views")
                                                )
                                            ),
                                            new JValue(0)
                                        }
                                    )
                                )
                            }
                        )
                    )
                ),
                new JProperty("columns",
                    new JObject(
                        new JProperty("svfId",
                            new JValue("s.lmvId")
                        ),
                        new JProperty("s.svf2Id",
                            new JValue(true)
                        ),
                        new JProperty("s.externalId",
                            new JValue(true)
                        ),
                        new JProperty("level",
                            //new JValue("s.props.p01bbdcf2") //!<<< Might need to be changed by models
                            new JValue(levelFieldKey)
                        ),
                        new JProperty("name",
                            new JValue("s.props.p153cb174")
                        ),
                        new JProperty("category",
                            new JValue("s.props.p5eddc473")
                        ),
                        new JProperty("revitCategory",
                            new JValue("s.props.p20d8441e")
                        ),
                        new JProperty("s.views",
                            new JValue(true)
                        )
                    )
                )
            );

            return await BuildPropertyQueryAsync(accessToken, projectId, indexId, data);
        }
        public static async Task<dynamic> BuildSpaceDataAsync(string accessToken, string projectId, List<string> versionIds, bool buildTree = false)
        {
            dynamic indexingRes = await BuildPropertyIndexesAsync(accessToken, projectId, versionIds);
            string indexId = indexingRes.indexes[0].indexId;
            string state = indexingRes.indexes[0].state;
            while (state != "FINISHED")
            {
                //keep polling
                dynamic result = await GetPropertyIndexesStatusAsync(accessToken, projectId, indexId);
                state = result.state;
            }

            dynamic queryRes = await QueryRoomsPropertiesAsync(accessToken, projectId, indexId);
            string queryId = queryRes.queryId;
            state = queryRes.state;

            while (state != "FINISHED")
            {
                //keep polling
                dynamic result = await GetPropertyQueryStatusAsync(accessToken, projectId, indexId, queryId);
                state = result.state;
            }

            List<JObject> queryResultRes = await GetPropertyQueryResultsAsync(accessToken, projectId, indexId, queryId);
            dynamic manifestRes = await GetPropertyIndexesManifestAsync(accessToken, projectId, indexId);
            //dynamic fieldRes = await GetPropertyFieldsAsync(accessToken, projectId, indexId);

            var views = manifestRes.seedFiles[0].views as JArray;
            // Assume that rooms extracted in one phase/master view only.
            dynamic view = views.Where(view => ((string)((dynamic)view).id) == (string)((dynamic)queryResultRes[0]).views[0]).FirstOrDefault();

            int levelIndex = 0;
            var spaces = new List<Space>();
            var svfIds = new List<int>();
            var svf2Ids = new List<int>();

            var groupResult = queryResultRes.GroupBy(item =>
            {
                dynamic room = item as dynamic;
                return room.level;
            });

            if (buildTree)
            {
                foreach (var group in groupResult)
                {
                    var level = new Space
                    {
                        Name = group.Key,
                        Type = "Levels",
                        Order = ++levelIndex
                    };

                    int roomIndex = 0;
                    level.Children = group.Select(item =>
                    {
                        dynamic room = item as dynamic;
                        int svf2Id = room.svf2Id;
                        int svfId = room.svfId;

                        svfIds.Add(svfId);
                        svf2Ids.Add(svf2Id);

                        return new AecRoom
                        {
                            Id = room.externalId,
                            Name = Regex.Replace(room.name.ToString(), @"\[[^]]*\]", string.Empty).Trim(),
                            // category = room.category,
                            // revitCategory = room.revitCategory
                            Type = room.revitCategory,
                            ParentId = level.Id,
                            Order = ++roomIndex,
                            Svf2Id = svf2Id,
                            SvfId = svfId,
                        };
                    }).ToList<Space>();

                    spaces.Add(level);
                }
            }
            else
            {
                foreach (var group in groupResult)
                {
                    var level = new Space
                    {
                        Name = group.Key,
                        Type = "Levels",
                        Order = ++levelIndex
                    };

                    int roomIndex = 0;

                    var rooms = group.Select(item =>
                    {
                        dynamic room = item as dynamic;
                        int svf2Id = room.svf2Id;
                        int svfId = room.svfId;

                        svfIds.Add(svfId);
                        svf2Ids.Add(svf2Id);

                        return new AecRoom
                        {
                            Id = room.externalId,
                            Name = Regex.Replace(room.name.ToString(), @"\[[^]]*\]", string.Empty).Trim(),
                            // category = room.category,
                            // revitCategory = room.revitCategory
                            Type = room.revitCategory,
                            ParentId = level.Id,
                            Order = ++roomIndex,
                            Svf2Id = svf2Id,
                            SvfId = svfId,
                        };
                    });

                    spaces.Add(level);
                    spaces.AddRange(rooms);
                }
            }

            dynamic data = new System.Dynamic.ExpandoObject();
            data.view = view;
            data.spaces = spaces;
            data.svfIds = svfIds;
            data.svf2Ids = svf2Ids;

            return data;
        }

        public static async Task<List<Location>> ImportLocationsFromModelPropsAsync(string accessToken, string projectId, string versionId)
        {
            System.Diagnostics.Trace.WriteLine("1. Getting file derivative urn from Docs service");
            var derivativeUrn = await AccDataUtil.GetVersionDerivativeUrnAsync($"b.{projectId}", versionId, accessToken);

            System.Diagnostics.Trace.WriteLine("2. Getting AEC model data from derivative manifest");
            List<Level> levelsData = await AccDataUtil.GetLevelsFromAecModelData(accessToken, derivativeUrn);

            System.Diagnostics.Trace.WriteLine("3. Getting space data from Model Properties API");
            dynamic locationsFromModelProps = await AccDataUtil.BuildSpaceDataAsync(accessToken, projectId, new List<string>{
                versionId
            }, false);

            System.Diagnostics.Trace.WriteLine("4. Getting location root from Locations API");
            string levelGuid = "";
            var locationRootNode = await AccDataUtil.GetLocationsRootAsync(accessToken, projectId);

            System.Diagnostics.Trace.WriteLine("5. Iterating space data and importing them into Locations service");
            var locations = new List<Location>();

            foreach (Space space in locationsFromModelProps.spaces)
            {
                var name = space.Name;
                var type = space.Type;
                Location location = null;
                bool isLevel = false;

                if (type == "Levels")
                {
                    isLevel = true;
                    var levelInAecData = levelsData.Find(lvl => lvl.Name == space.Name);
                    var externalId = levelInAecData == null ? space.Id : levelInAecData.Guid;

                    var bytesEncodedExternalId = System.Text.Encoding.UTF8.GetBytes(externalId);
                    var base64ExternalId = System.Convert.ToBase64String(bytesEncodedExternalId);

                    location = new Location
                    {
                        Name = name,
                        Type = "Area",
                        Barcode = base64ExternalId,
                        ParentId = locationRootNode.Id
                    };
                }
                else
                {
                    isLevel = false;
                    var bytesEncodedExternalId = System.Text.Encoding.UTF8.GetBytes(space.Id);
                    var base64ExternalId = System.Convert.ToBase64String(bytesEncodedExternalId);
                    location = new Location
                    {
                        Name = name,
                        Type = "Area",
                        Barcode = base64ExternalId,
                        ParentId = levelGuid
                    };
                }

                var result = await AccDataUtil.CreateLocationsNodeAsync(accessToken, projectId, location, null, null);
                if (result == null)
                    throw new InvalidOperationException("Failed to create new node");

                locations.Add(result);

                if (isLevel)
                    levelGuid = result.Id;
            }

            System.Diagnostics.Trace.WriteLine("6. Successfully imported all space data into Locations service!");

            return locations;
        }
    }
}