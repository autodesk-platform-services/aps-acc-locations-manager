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
// AUTODESK PROVIDES THIS PROGRAM 'AS IS' AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

window.CURRENT_PROJECT_ID = null;
function prepareLocationsTree(projectId) {
    console.log(projectId);

    if (window.CURRENT_PROJECT_ID == projectId)
        return;
    else
        window.CURRENT_PROJECT_ID = projectId;

    $('#locationsTree').jstree('destroy');
    $('#locationsTree').empty();
    $('<div>', { id: 'locationsTree' }).appendTo('#acc-viewport');

    $('#locationsTree').jstree({
        'core': {
            'themes': { 'icons': true },
            'multiple': false,
            'data': {
                'url': `/api/aps/acc/projects/${projectId}/locations`,
                'dataType': 'json',
                'cache': false,
                'data': function (node) {
                    $('#locationsTree').jstree(true).toggle_node(node);
                    return { 'id': node.id };
                }
            }
        },
        'types': {
            'default': {
                'icon': false
            },
            'root': {
                'icon': false
            },
            'area': {
                'icon': false
            },
            'unsupported': {
                'icon': 'glyphicon glyphicon-ban-circle'
            }
        },
        'plugins': ['types', 'state', /*'sort',*/ 'contextmenu'],
        contextmenu: { items: autodeskCustomMenu },
        'state': { 'key': 'autodeskLBS' }// key restore tree state
    })
        .bind('loaded.jstree', function (e, data) {
            expandFirstTreeTier(data.instance);
        })
        .bind('refresh.jstree', function (e, data) {
            expandFirstTreeTier(data.instance);
        });
}

function expandFirstTreeTier(tree) {
    /** 
     * Open nodes on load (until x'th level) 
     * ref: https://stackoverflow.com/a/15485761
     */
    let depth = 2;
    tree.get_container().find('li').each(function (i) {
        if (tree.get_path($(this)).length <= depth) {
            tree.open_node($(this));
        }
    });
}