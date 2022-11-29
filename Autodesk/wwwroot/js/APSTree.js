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

$(document).ready(function () {
  // first, check if current visitor is signed in
  jQuery.ajax({
    url: '/api/aps/oauth/token',
    success: function (res) {
      // yes, it is signed in...
      $('#signOut').show();
      $('#refreshHubs').show();

      // prepare sign out
      $('#signOut').click(function () {
        $('#hiddenFrame').on('load', function (event) {
          location.href = '/api/aps/oauth/signout';
        });
        $('#hiddenFrame').attr('src', 'https://accounts.autodesk.com/Authentication/LogOut');
        // learn more about this signout iframe at
        // https://aps.autodesk.com/blog/log-out-forge
      })

      // and refresh button
      $('#refreshHubs').click(function () {
        $('#userHubs').jstree(true).refresh();
      });

      // finally:
      prepareUserHubsTree();
      showUser();
    }
  });

  $('#autodeskSigninButton').click(function () {
    jQuery.ajax({
      url: '/api/aps/oauth/url',
      success: function (url) {
        location.href = url;
      }
    });
  });

  $('#createNewLocation').click(function () {
    let autodeskNode = $('#userHubs').jstree(true).get_selected(true)[0];
    let idParams = autodeskNode.id.split('/');
    let hubId = idParams[idParams.length - 3].replace('b.', '');
    let projectId = idParams[idParams.length - 1].replace('b.', '');

    let selectedLocationNode = $('#locationsTree').jstree(true).get_selected(true)[0];
    let data = {
      name: $('#locationNodeName').val(),
      barcode: $('#locationNodeBarcode').val(),
      parentId: selectedLocationNode.id.replace('lbs_', '')
    };

    jQuery.post({
      url: `/api/aps/acc/projects/${projectId}/locations`,
      contentType: 'application/json',
      data: JSON.stringify(data),
      success: function (data) {
        $('#locationsTree').jstree(true).refresh();
        $('#addLocationModal').modal('toggle');
      },
      error: function (err) {
        alert('Failed to create new location node');

        console.log(err);
      }
    });
  });

  $('#createNewPeerLocation').click(function () {
    let autodeskNode = $('#userHubs').jstree(true).get_selected(true)[0];
    let idParams = autodeskNode.id.split('/');
    let hubId = idParams[idParams.length - 3].replace('b.', '');
    let projectId = idParams[idParams.length - 1].replace('b.', '');

    let selectedLocationNode = $('#locationsTree').jstree(true).get_selected(true)[0];
    let data = {
      name: $('#peerLocationNodeName').val(),
      barcode: $('#peerLocationNodeBarcode').val(),
      parentId: selectedLocationNode.parent.replace('lbs_', ''),
      insertOption: $('#insertLocationMode :selected').text(),
      targetNodeId: selectedLocationNode.id.replace('lbs_', '')
    };

    jQuery.post({
      url: `/api/aps/acc/projects/${projectId}/locations`,
      contentType: 'application/json',
      data: JSON.stringify(data),
      success: function (data) {
        $('#locationsTree').jstree(true).refresh();
        $('#addPeerLocationModal').modal('toggle');
      },
      error: function (err) {
        alert('Failed to create new location node');

        console.log(err);
      }
    });
  });

  $('#updateLocation').click(function () {
    let autodeskNode = $('#userHubs').jstree(true).get_selected(true)[0];
    let idParams = autodeskNode.id.split('/');
    let hubId = idParams[idParams.length - 3].replace('b.', '');
    let projectId = idParams[idParams.length - 1].replace('b.', '');

    let selectedLocationNode = $('#locationsTree').jstree(true).get_selected(true)[0];
    let nodeId = selectedLocationNode.id.replace('lbs_', '');
    let data = {
      name: $('#newLocationNodeName').val(),
      barcode: $('#newLocationNodeBarcode').val()
    };

    jQuery.ajax({
      url: `/api/aps/acc/projects/${projectId}/locations/${nodeId}`,
      method: 'patch',
      contentType: 'application/json',
      data: JSON.stringify(data),
      success: function (data) {
        $('#locationsTree').jstree(true).refresh();
        $('#updateLocationModal').modal('toggle');
      },
      error: function (err) {
        alert('Failed to update location node data');

        console.log(err);
      }
    });
  });

  $('#deleteLocation').click(function () {
    let autodeskNode = $('#userHubs').jstree(true).get_selected(true)[0];
    let idParams = autodeskNode.id.split('/');
    let hubId = idParams[idParams.length - 3].replace('b.', '');
    let projectId = idParams[idParams.length - 1].replace('b.', '');

    let selectedLocationNode = $('#locationsTree').jstree(true).get_selected(true)[0];
    let nodeId = selectedLocationNode.id.replace('lbs_', '');

    jQuery.ajax({
      url: `/api/aps/acc/projects/${projectId}/locations/${nodeId}`,
      method: 'delete',
      contentType: 'application/json',
      success: function (data) {
        $('#locationsTree').jstree(true).refresh();
        $('#deleteLocationModal').modal('hide');
      },
      error: function (err) {
        alert('Failed to delete the location node');

        console.log(err);
      }
    });
  });

  $('#importModelLocationFromModelButton').click(function () {
    $('#deleteLocationTreeModal').modal('show');
  });

  $('#deleteLocationTree').click(function () {
    $('#importModelLocationFromModelProgressBar .progress-bar').css('width', '0%').attr('aria-valuenow', 0);
    $('#importModelLocationFromModelProgressBar p').text('0/2 Processing ...');

    $('#importModelLocationFromModelButton').addClass('hidden');
    $('#importModelLocationFromModelButton').removeClass('show');

    $('#importModelLocationFromModelProgressBar').removeClass('hidden');
    $('#importModelLocationFromModelProgressBar').addClass('show');

    let autodeskNode = $('#userHubs').jstree(true).get_selected(true)[0];
    let urn = autodeskNode.id;
    let idParams = autodeskNode.parent.split('/');
    let projectId = idParams[idParams.length - 3].replace('b.', '');
    let itemId = idParams[idParams.length - 1].replace('b.', '');

    jQuery.ajax({
      url: `/api/aps/acc/projects/${projectId}/locations:destroy`,
      method: 'delete',
      contentType: 'application/json',
      success: function (data) {
        console.log(`Iot Project \`${projectId}\` created.`, data);

        $('#importModelLocationFromModelProgressBar .progress-bar').css('width', '50%').attr('aria-valuenow', 50);
        $('#importModelLocationFromModelProgressBar p').text('1/2 Fetching and importing locations data from model ...');

        jQuery.post({
          url: `/api/aps/acc/projects/${projectId}/locations:import`,
          contentType: 'application/json',
          data: JSON.stringify({
            urn
          }),
          success: function (data) {
            console.log(`Completed! Successfully imported locations from model for project \`${projectId}\`.`, data);

            $('#importModelLocationFromModelProgressBar .progress-bar').css('width', '100%').attr('aria-valuenow', 100);
            $('#importModelLocationFromModelProgressBar p').text('2/2 Imported locations from model ...');

            setTimeout(
              () => {
                $('#locationsTree').jstree(true).refresh();
                $('#importModelLocationFromModelModal').modal('hide');
              },
              3000
            );
          },
          error: function (err) {
            alert('Failed to import locations from model');

            console.log(err);

            $('#importModelLocationFromModelProgressBar .progress-bar').css('width', '0%').attr('aria-valuenow', 0);
            $('#importModelLocationFromModelProgressBar p').text('0/2 Processing ...');

            $('#importModelLocationFromModelProgressBar').removeClass('show');
            $('#importModelLocationFromModelProgressBar').addClass('hidden');

            $('#importModelLocationFromModelButton').addClass('show');
            $('#importModelLocationFromModelButton').removeClass('hidden');
          }
        });
      },
      error: function (err) {
        alert('Failed to delete all nodes from the location tree');

        console.log(err);

        $('#importModelLocationFromModelProgressBar .progress-bar').css('width', '0%').attr('aria-valuenow', 0);
        $('#importModelLocationFromModelProgressBar p').text('0/2 Processing ...');

        $('#importModelLocationFromModelProgressBar').removeClass('show');
        $('#importModelLocationFromModelProgressBar').addClass('hidden');

        $('#importModelLocationFromModelButton').addClass('show');
        $('#importModelLocationFromModelButton').removeClass('hidden');
      }
    });
  });

  $.getJSON('/api/aps/clientid', function (res) {
    $('#ClientID').val(res.id);
    $('#provisionAccountSave').click(function () {
      $('#provisionAccountModal').modal('toggle');
      $('#userHubs').jstree(true).refresh();
    });
  });
});

function prepareUserHubsTree() {
  var haveBIM360Hub = false;
  $('#userHubs').jstree({
    'core': {
      'themes': { 'icons': true },
      'multiple': false,
      'data': {
        'url': '/api/aps/datamanagement',
        'dataType': 'json',
        'cache': false,
        'data': function (node) {
          $('#userHubs').jstree(true).toggle_node(node);
          return { 'id': node.id };
        },
        'success': function (nodes) {
          nodes.forEach(function (n) {
            if (n.type === 'bim360Hubs' && n.id.indexOf('b.') > 0)
              haveBIM360Hub = true;
          });

          if (!haveBIM360Hub) {
            $('#provisionAccountModal').modal();
            haveBIM360Hub = true;
          }
        }
      }
    },
    'types': {
      'default': {
        'icon': 'glyphicon glyphicon-question-sign'
      },
      '#': {
        'icon': 'glyphicon glyphicon-user'
      },
      // 'hubs': {
      //   'icon': 'https://cdn.autodesk.io/dm/a360hub.png'
      // },
      // 'personalHub': {
      //   'icon': 'https://cdn.autodesk.io/dm/a360hub.png'
      // },
      // 'bim360Hubs': {
      //   'icon': 'https://cdn.autodesk.io/dm/bim360hub.png'
      // },
      // 'bim360projects': {
      //   'icon': 'https://cdn.autodesk.io/dm/bim360project.png'
      // },
      // 'a360projects': {
      //   'icon': 'https://cdn.autodesk.io/dm/a360project.png'
      // },
      'hubs': {
        'icon': 'https://github.com/Autodesk-Forge/learn.forge.viewhubmodels/raw/master/img/a360hub.png'
      },
      'personalHub': {
        'icon': 'https://github.com/Autodesk-Forge/learn.forge.viewhubmodels/raw/master/img/a360hub.png'
      },
      'bim360Hubs': {
        'icon': 'https://github.com/Autodesk-Forge/learn.forge.viewhubmodels/raw/master/img/bim360hub.png'
      },
      'bim360projects': {
        'icon': 'https://github.com/Autodesk-Forge/learn.forge.viewhubmodels/raw/master/img/bim360project.png'
      },
      'a360projects': {
        'icon': 'https://github.com/Autodesk-Forge/learn.forge.viewhubmodels/raw/master/img/a360project.png'
      },
      'items': {
        'icon': 'glyphicon glyphicon-file'
      },
      'bim360documents': {
        'icon': 'glyphicon glyphicon-file'
      },
      'folders': {
        'icon': 'glyphicon glyphicon-folder-open'
      },
      'versions': {
        'icon': 'glyphicon glyphicon-time'
      },
      'unsupported': {
        'icon': 'glyphicon glyphicon-ban-circle'
      }
    },
    'sort': function (a, b) {
      var a1 = this.get_node(a);
      var b1 = this.get_node(b);
      var parent = this.get_node(a1.parent);
      if (parent.type === 'items') {
        var id1 = Number.parseInt(a1.text.substring(a1.text.indexOf('v') + 1, a1.text.indexOf(':')))
        var id2 = Number.parseInt(b1.text.substring(b1.text.indexOf('v') + 1, b1.text.indexOf(':')));
        return id1 > id2 ? 1 : -1;
      }
      else if (parent.type === 'bim360Hubs') {
        return (a1.text > b1.text) ? 1 : -1;
      }
      else return a1.type < b1.type ? -1 : (a1.text > b1.text) ? 1 : 0;
    },
    'plugins': ['types', 'state', 'sort', 'contextmenu'],
    contextmenu: { items: autodeskCustomMenu },
    'state': { 'key': 'autodeskHubs' }// key restore tree state
  }).bind('activate_node.jstree', function (evt, data) {
    if (data != null && data.node != null) {
      let idParams = null;
      let projectId = null;

      switch (data.node.type) {
        case 'bim360projects':
          idParams = data.node.id.split('/');
          projectId = idParams[idParams.length - 1].replace('b.', '');
          break;
        case 'folders': case 'items':
          idParams = data.node.id.split('/');
          projectId = idParams[idParams.length - 3].replace('b.', '');
          break;
        case 'versions':
          idParams = data.node.parent.split('/');
          projectId = idParams[idParams.length - 3].replace('b.', '');
          break;
      }

      if (!projectId) return;

      prepareLocationsTree(projectId);
    }
  });
}

function autodeskCustomMenu(autodeskNode, buildContextMenu) {
  function createLocationNodeAction(event) {
    console.log(event);
    let tree = $.jstree.reference(event.reference);
    let node = tree.get_node(event.reference);

    console.log(node);

    $('#addLocationModal').modal('show');
  }

  var items;

  switch (autodeskNode.type) {
    case 'versions':
      items = {
        importLocationsFromModel: {
          label: 'Import locations from this model',
          action: function () {
            $('#importModelLocationFromModelModal').modal('show');
          },
        }
      };

      break;
    case 'root':
      items = {
        createLbsNode: {
          label: 'Add new location',
          action: createLocationNodeAction,
        }
      };

      break;
    case 'area': case 'level':
      items = {
        createLbsNode: {
          label: 'Add sub-location',
          action: createLocationNodeAction,
        },
        createPeerLbsNode: {
          label: 'Add peer location',
          action: function (event) {
            console.log(event);
            let tree = $.jstree.reference(event.reference);
            let node = tree.get_node(event.reference);

            console.log(node);

            $('#addPeerLocationModal').modal('show');
          },
        },
        updateLbsNode: {
          label: 'Update location data',
          action: function (event) {
            console.log(event);
            let tree = $.jstree.reference(event.reference);
            let node = tree.get_node(event.reference);

            $('input#newLocationNodeName').val(node.original.text);
            $('input#newLocationNodeBarcode').val(node.original.barcode);
            console.log(node);

            $('#updateLocationModal').modal('show');
          },
        },
        removeLbsNode: {
          label: 'Remove this location',
          action: function (event) {
            console.log(event);
            let tree = $.jstree.reference(event.reference);
            let node = tree.get_node(event.reference);

            $('#deleteLocationTarget').text(node.original.text);
            $('#deleteLocationModal').modal('show');
            console.log(node);
          },
        }
      };

      break;

      default:
        items = {};
        break;
  }

  buildContextMenu(items);
}

function showUser() {
  jQuery.ajax({
    url: '/api/aps/user/profile',
    success: function (profile) {
      var img = '<img src="' + profile.picture + '" height="30px">';
      $('#userInfo').html(img + profile.name);
    }
  });
}