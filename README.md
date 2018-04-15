# SuperStomper
An **ungraceful** (hence the name) approach to Rhino as a Server and Gh Cloud Components. 

The Rhino Server (Super Stomper) essentially exposes all `Rhino.Geometry` methods from RhinoCommon.dll as REST api endpoints. It reflects on them when the server is first run, creates a simple abstraction layer, and then presents them in quasi readable format.

Examples: 
- All methods for the [sphere namespace](https://stomper.speckle.works/types/Sphere).
- The brep-bound [CreateBooleanUnion method](https://stomper.speckle.works/methods/1068).

To get a method, simply `GET  /methods/{methodId}`, where methodId is a number (i think there's ±8k methods now, most of them not really usefull).

## The Rhino Server

> A test version is online at [stomper.speckle.works](https://stomper.speckle.works).

To get it running:

**Option one**: install the .rhp located [here](https://github.com/didimitrie/SuperStomper/blob/master/SuperStopmper/SuperStopmper/bin/SuperStomper_RH.zip). Don't forget to:
- download the zip & unzip to a folder
- unblock all files (right click, unblock)
- drag the .rhp in rhino 6.

**Option two:** open the `.sln` in Visual Studio and hit run. 

**Finally**, to get the server running type `SuperStomperStart` in the rhino command line. If you see a message saying that you're good to go @ 1337, it's working fine! Otherwise, check if port 1337 is not in use.

To see it in action point your browser to [localhost:1337](http://localhost:1337). You should see something like this:
![image](https://user-images.githubusercontent.com/7696515/38782947-77fef9bc-40f3-11e8-926b-e626171f14c1.png)
