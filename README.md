# ACC Locations Manager Sample

![Platforms](https://img.shields.io/badge/platform-Windows|MacOS-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET%20-6.0-blue.svg)
[![MIT](https://img.shields.io/badge/License-MIT-blue.svg)](http://opensource.org/licenses/MIT)

[![oAuth2](https://img.shields.io/badge/oAuth2-v1-green.svg)](http://forge.autodesk.com/)
[![Data-Management](https://img.shields.io/badge/Data%20Management-v1-green.svg)](http://forge.autodesk.com/)
[![ACC Locations API](https://img.shields.io/badge/ACC%20Locations%20api-v1-green.svg)](https://forge.autodesk.com/en/docs/acc/v1/reference/http/locations-nodes-GET/)
[![ACC Model Properties API](https://img.shields.io/badge/ACC%20Model%20Properties%20api-v1-green.svg)](https://forge.autodesk.com/en/docs/acc/v1/reference/http/index-v2-index-fields-get/)

[![Level](https://img.shields.io/badge/Level-Intermediate-blue.svg)](http://developer.autodesk.com/)

# Description

This sample demonstrates the following use cases:

* View Locations tree (included sub-location nodes) similar to ACC Admin Locations UI.
* Add Locations node
  * Add sub-locations nodes
  * Add node before and after the target node. (in the same tree tier)
* Edit and delete the Locations node.
* Import Locations (Levels & Rooms) from the selected Revit model via using [Model Properties API](https://forge.autodesk.com/en/docs/acc/v1/reference/http/index-v2-index-fields-get/)

This sample is implemented based on the .NET Core version of [Learn Forge Tutorial](https://github.com/Autodesk-Forge/learn.forge.viewhubmodels/tree/netcore). Please refer to https://learnforge.autodesk.io/ for details about the framework.

## Thumbnail

![thumbnail](.readme/thumbnail.png)

## Demonstration

Here is the video demonstrating how this sample works quickly.

[![](http://img.youtube.com/vi/sD9AZfFpydQ/0.jpg)](http://www.youtube.com/watch?v=sD9AZfFpydQ "Demo how to manage ACC **Locations** data and import locations from the selected Revit model.")

# Web App Setup

## Prerequisites

1. **Forge Account**: Learn how to create a Forge Account, activate subscription, and create an app at [this tutorial](http://learnforge.autodesk.io/#/account/). 
2. **ACC Account**: must be Account Admin to add the app integration. [Learn about provisioning](https://forge.autodesk.com/blog/bim-360-docs-provisioning-forge-apps).
3. **Visual Studio**: Either Community 2019+ (Windows) or Code (Windows, MacOS).
4. **.NET 6** basic knowledge with C#
5. **JavaScript** basic knowledge with **jQuery**

## Running locally

Clone this project or download it. It's recommended to install [GitHub Desktop](https://desktop.github.com/). To clone it via command line, use the following (**Terminal** on MacOSX/Linux, **Git Shell** on Windows):

    git clone https://github.com/autodesk-forge/forge-acc.locations.manager

**Visual Studio** (Windows):

Right-click on the project, then go to **Debug**. Adjust the settings as shown below.

![](.readme/visual_studio_settings.png) 

**Visual Studio Code** (Windows, MacOS):

Open the folder, at the bottom-right, select **Yes** and **Restore**. This restores the packages (e.g., Autodesk.Forge) and creates the launch.json file. See *Tips & Tricks* for .NET Core on MacOS.

![](.readme/visual_code_restore.png)

At the `.vscode\launch.json`, find the env vars and add your Forge Client ID, Secret, and callback URL. Also, define the `ASPNETCORE_URLS` variable. The end result should be as shown below:

```json
"env": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "ASPNETCORE_URLS" : "http://localhost:3000",
    "FORGE_CLIENT_ID": "your id here",
    "FORGE_CLIENT_SECRET": "your secret here",
    "FORGE_CALLBACK_URL": "http://localhost:3000/api/forge/callback/oauth",
},
```

Run the app. Open `http://localhost:3000` to view your files. It may be required to **Enable my ACC Account** (see app top-right).

## Use Cases

1. Open the browser: http://localhost:3000.
2. After user logging, select a project or click either tree nodes in the read line. It will load the Locations data for the selected project from the Locations service.

    <img src=".readme/open-locations-tree.png" style="width:500px" /><br/>

3. Once Locations are loaded, we can see the root node called `Project` on the right side. When right-clicking on it, we can create the first location node.

    <img src=".readme/create-first-location-node.png" style="width:500px" /><br/>
    <img src=".readme/create-first-location-node-2.png" style="width:500px" /><br/>

4. When right-clicking on the non-root-typed location node, there are 4 supported operations:

    <img src=".readme/node-operations.png" style="width:500px" /><br/>

    * **Add sub-location node:** To add child nodes for this node.
        <br/><img src=".readme/node-operations-1.png" style="width:500px" /><br/>
    * **Add peer location node:** To add a node in the same tier/depth for this node.
        <br/><img src=".readme/node-operations-2.png" style="width:500px" /><br/>
    * **Update location data:** To update the name or barcode for this node.
        <br/><img src=".readme/node-operations-3.png" style="width:500px" /><br/>
    * **Delete this location:** To Delete selected node.
        <br/><img src=".readme/node-operations-4.png" style="width:500px" /><br/>

5. When right-clicking on the model version node, the `Import locations from this model` feature will pop up. We can use it to fetch locations from the selected Revit model and import the result to Location service.

    <img src=".readme/import-locations-from-model.png" style="width:500px" /><br/>

    5.1 Click `Start` here to start the importing task, and then it will prompt a warning that all existing location nodes will be deleted before importing.<br/> 
        <img src=".readme/import-locations-from-model-1.png" style="width:500px" /><br/>
        <img src=".readme/import-locations-from-model-2.png" style="width:500px" /><br/>
    5.2 This sample will start using model properties API to query levels and rooms from the Revit model, and then import the result to the Location service.<br/>
        <img src=".readme/import-locations-from-model-3.png" style="width:500px" /><br/>
        <img src=".readme/import-locations-from-model-4.png" style="width:500px" /><br/>
        <img src=".readme/import-locations-from-model-5.png" style="width:500px" /><br/>

## Deployment

To deploy this application to Heroku, the **Callback URL** for Forge must use your `.herokuapp.com` address. After clicking on the button below, on the Heroku Create New App page, set your Client ID, Secret, and Callback URL for Forge.

[![Deploy](https://www.herokucdn.com/deploy/button.svg)](https://heroku.com/deploy?template=https://github.com/Autodesk-Forge/forge-acc.locations.manager)

Watch [this video](https://www.youtube.com/watch?v=Oqa9O20Gj0c) on how to deploy samples to Heroku.

# Further Reading

Documentation:

- [Data Management API](https://forge.autodesk.com/en/docs/data/v2/overview/)
- [Locations API Field Guid](https://forge.autodesk.com/en/docs/acc/v1/overview/field-guide/locations/)
- [Locations API Reference](https://forge.autodesk.com/en/docs/acc/v1/reference/http/locations-nodes-GET/)

Tutorials:

- [Configure a Locations Tree](https://forge.autodesk.com/en/docs/acc/v1/tutorials/locations/)

Blogs:

- [Forge Blog](https://forge.autodesk.com/apis-and-services/autodesk-construction-cloud-acc-apis)
- [Field of View](https://fieldofviewblog.wordpress.com/), a BIM-focused blog

### Tips & Tricks

This sample uses .NET Core and works fine on both Windows and MacOS. See [this tutorial for MacOS](https://github.com/augustogoncalves/dotnetcoreheroku).

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

## Change Log
- 09/03/2022: first release

## Written by

Eason Kang [@yiskang](https://twitter.com/yiskang), [Forge Partner Development](http://forge.autodesk.com)