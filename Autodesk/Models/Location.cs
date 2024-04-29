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

using System;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace Autodesk.Aps.Models
{
    /// <summary>
    /// Location
    /// </summary>
    public class Location: Space
    {
        public Location()
        {
            this.Path = new List<string>();
        }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        public string Barcode { get; set; }
        /// <summary>
        /// Node document count
        /// </summary>
        public string DocumentCount { get; set; }
        /// <summary>
        /// Path information from the root node to the current node. This information is only included if you use the filter[id] parameter
        /// </summary>
        public List<string> Path { get; set; }

        public static List<Location> BuildTree(List<Location> list, string parentId)
        {
            return list.Where(x => x.ParentId == parentId).Select(x =>
            {
                Location loc = x.MemberwiseClone() as Location;
                loc.Children = Location.BuildTree(list, x.Id).ToList<Space>();
                return loc;
            }).ToList();
        }
    }
}