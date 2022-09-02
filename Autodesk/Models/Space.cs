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
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace Autodesk.Forge.Models
{
    /// <summary>
    /// Location
    /// </summary>
    public class Space
    {
        /// <summary>
        /// Node id
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Parent node Id. null if this is the root node
        /// </summary>
        public string ParentId { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// Node name
        /// </summary>
        [MaxLength(255)]
        public string Name { get; set; }
        public int Order { get; set; }
        /// <summary>
        /// The list of child location
        /// </summary>
        public List<Space> Children { get; set; }
    }
}