# SuperStomper
An **ungraceful** (hence the name) approach to Rhino as a Server and Gh Cloud/Async/Generic Components, done as a weekend project.

The Rhino Server (Super Stomper) essentially exposes all `Rhino.Geometry` methods from RhinoCommon.dll as REST api endpoints. It reflects on them when the server is first run, creates a simple abstraction layer, and then presents them in quasi readable format.

Examples: 
- All methods for the [sphere namespace](https://stomper.speckle.works/types/Sphere).
- The brep-bound [CreateBooleanUnion method](https://stomper.speckle.works/methods/1068).

To get a method, simply `GET  /methods/{methodId}`, where methodId is a number (i think there's ±8k methods now, most of them not really usefull).

> for best viewing experience of json data in a browser, either use firefox or try this [chrome extension](https://chrome.google.com/webstore/detail/json-viewer-awesome/iemadiahhbebdklepanmkjenfdebfpfe?hl=en).

The Gh components located in the [cloud compute folder](https://github.com/didimitrie/SuperStomper/tree/master/CloudCompute/CloudCompute) help you call those methods. See below for more info.

## Why did you bother? 

I wanted to put my money where my mouth is, and have some fun before annoying people from mcneel. Some takeaways:

#### 1. let's have a nice proper way to serialise method info!  
So that we can call them easily and without hassle from anywhere, ie a browser based interface or a morphing gh component! (see below). 

#### 2. gzip all the things! 
I get around *60-90%* request and response payload size reduction. I've currently implemented at software level - because i hate configuring IIS which is powering mr. stomper on azure. For example, creating 100 spheres, response goes down from ±160kb to 24kb!

#### 3. deployment and scalability
Well right now you're bound to one server per rhino running at one port. Test the best scenarios:
- To scale on one vm: open more rhinos, get IIS to do roundrobins as a reverse proxy. I hate IIS, nginx is much nicer. You can use one rhino license though! 
- To scale on more vms: move the reverse proxy out and do the load balancing on a uhm loadbalancer. I guess. 

#### 4. the gh components

Takeaway 1:

The gh components essentially all reflect on a method and morph into a visual aide to help you call that method via gh's lovely ui. So no more coding of SolveInstance and stuff, you just write nice methods that later get transformed into compoents. They don't need to be static, it's easy to invoke non-static ones! 

Takeaway 1.1: 

This can would allow for a node-to-code approach. *All components are just methods called in a specific order.*

Takeaway 1.2:  

If you codifiy the above nicely, you can run globs of functions on a server. That would allow for totally rad things:
- a continous build & test piepline for design 
- (ie, like autonomous clusters verifying data everytime you push geometry to speckle stream)
- tests can be defined by anyone with some experience of visual programming
- tests can be run locally for cool dudes, but also remotely - ie structural engineer defining a test for architect / costing for design  / etc. 

Takeaway 2:

You can defs do async stuff, without blocking the ui thread, with the component approach above. 

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

Ok, great, now how do I get to invoke these methods? Onwards:

## The Grasshopper Componets

There's several components (four in total) that do different things and push different agendas. There's only one that works with the server. In reverse order of (geek) coolness: 

### The Remote (At Url) Executor

Lovely name, isn't it? When you drop one of these bad boys on the canvas, it will ask you for a method url. Just paste one, for example: `https://stomper.speckle.works/methods/6770` (creates a new sphere). Alternatively, you can use your own server if it's running (`http://localhost:1337/methods/6770`). 

> Be sure not to confuse between RemoteUrlExecutor (`ww` shortcut) and RemoteExecutor (`re` shortcut). The former can actually call methods on any stomper server, whereas the latter is bound to a local server on `localhost:1337`. 

If the url is correct and actually points to a stomper method, you will see a little ugly json tree under it. If it looks good, just  hit enter. The following will happen: 

- the component will instantiate itself with the requiered inputs and outputs.
- once you plug in all the requiered data (check your types!), a debounced (every 200ms) gzipped POST request will be sent to the server. 
- once the results are in, they will be populated in the component's outputs and `ExpireSolution(true)` is called, so you can get your Sphere out (grasshopper will cast it to a breps unfortunately, and lists are not yet there).  

**Congrats, you've just made your first remote rhino server request!**

Some fun methods that actually work nicely with gh: 
- boolean union: https://stomper.speckle.works/methods/1068
- contour curves: https://stomper.speckle.works/methods/1081
- sphere, points, cones, etc. 
- mesh from brep, a holy grail: https://stomper.speckle.works/methods/3645
- create a spiral: https://stomper.speckle.works/methods/6328


To get what's going on, install [fiddler](https://www.telerik.com/fiddler) and inspect the traffic. You'll be able to grok easier the behind the scenes. 

### The Curious Computer Component  & Curios Computer Async

This guy is actually a stab at grasshopper's rather clunky way of dealing with components. Discussed a lot @luisfraguada what this can mean for the future, plenty of ideas, no time.

What it does is simple: it reflects on a `Rhino.Geometry.*.*` method of your choice, morphs into something that resembles that function's input and output, and then invokes it when all inputs are done. 

#### The Sync Component
This one just invokes stuff from `SolveInstance`, thus bloking the ui thread on long computations.

#### The Async Component
This one debounces the invokation to one every 200ms, and does it outside the main thread. Because we're missing gh's native data collection capabilities, it can't really do data matching (it does a cartesian set if lists are inputted where an item is expected).

That's it. I'm done for the weekend. No more fun coding :( 

