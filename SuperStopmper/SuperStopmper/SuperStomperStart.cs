using System;
using System.Collections.Generic;
using Nancy.Hosting.Self;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace SuperStopmper
{
  public class SuperStomperStart : Command
  {

    NancyHost myHost;

    public SuperStomperStart( )
    {
      // Rhino only creates one instance of each command class defined in a
      // plug-in, so it is safe to store a refence in a static property.
      Instance = this;
    }

    ///<summary>The only instance of this command.</summary>
    public static SuperStomperStart Instance
    {
      get; private set;
    }

    ///<returns>The command name as it appears on the Rhino command line.</returns>
    public override string EnglishName
    {
      get { return "SuperStomperStart"; }
    }

    protected override Result RunCommand( RhinoDoc doc, RunMode mode )
    {
      // TODO: start here modifying the behaviour of your command.
      // ---
      RhinoApp.WriteLine( "Starting Super Stomper." );

      Cc.CreateMethodsCollections();

      myHost = new NancyHost( new Uri( "http://localhost:1337" ) );

      myHost.Start();

      RhinoApp.WriteLine( "Good to go @ 1337", EnglishName );
      return Result.Success;
    }
  }


}
