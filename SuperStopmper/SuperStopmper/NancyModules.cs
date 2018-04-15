using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Extensions;
using Nancy.Gzip;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Rhino;

namespace SuperStopmper
{

  public class Bootstrapper : DefaultNancyBootstrapper
  {
    protected override void ApplicationStartup( TinyIoCContainer container, IPipelines pipelines )
    {
      // deflate all the things
      pipelines.EnableGzipCompression( new GzipCompressionSettings() { MinimumBytes = 0 } );
      base.ApplicationStartup( container, pipelines );
    }
  }

  public class MainModule : NancyModule
  {

    public static int RequestCounter = 0;

    public MainModule( )
    {
      Get[ "/", runAsync: true ] = async ( parameters, token ) =>
     {
       Dictionary<string, object> ret = new Dictionary<string, object>();
       Cc.MethodCollections.ForEach( coll =>
       {
         try
         {
           ret.Add( coll.name, new { url = "https://stomper.speckle.works/types/" + coll.name, methodCount = coll.methods.Count } );
         }
         catch { }
       } );

       var buff = Encoding.ASCII.GetBytes( JsonConvert.SerializeObject( ret ) );

       var response = new Response();
       response.ContentType = "application/json";
       response.Contents = s => s.Write( buff, 0, buff.Length );

       return response;
     };

      Get[ "/types/{typeName}", runAsync: true ] = async ( parameters, token ) =>
      {
        Dictionary<string, object> ret = new Dictionary<string, object>();

        var selectedMGroup = Cc.MethodCollections.FirstOrDefault( mmcol => mmcol.name == ( string ) parameters.typeName );

        selectedMGroup.methods.ForEach( method =>
         {
           ret.Add( method.methodId, new
           {
             name = method.name,
             url = "https://stomper.speckle.works/methods/" + method.methodId,
             desc = method.ToString()
           } );
         } );

        var buff = Encoding.ASCII.GetBytes( JsonConvert.SerializeObject( ret ) );

        var response = new Response();
        response.ContentType = "application/json";
        response.Contents = s => s.Write( buff, 0, buff.Length );
        return response;
      };

      Get[ "/methods/{methodId}" ] = parameters =>
      {
        var selectedMethod = Cc.METHODS[ Convert.ToInt32( ( string ) parameters.methodId ) ];

        var buff = Encoding.ASCII.GetBytes( JsonConvert.SerializeObject( selectedMethod ) );

        var response = new Response();
        response.ContentType = "application/json";
        response.Contents = s => s.Write( buff, 0, buff.Length );
        return response;
      };

      Post[ "/methods/{methodId}", runAsync: true ] = async ( parameters, token ) =>
      {
        var selectedMethod = Cc.METHODS[ Convert.ToInt32( ( string ) parameters.methodId ) ];
        if ( selectedMethod == null )
          return Response.AsJson( "No idea what you want me to do.", HttpStatusCode.BadRequest );

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var requestNumber = RequestCounter + 1; RequestCounter+=1;


        byte[ ] byteResponse = new byte[ Request.Body.Length ];
        Request.Body.Read( byteResponse, 0, ( int ) Request.Body.Length );

        string jString = null;

        using ( var compressedStream = new MemoryStream( byteResponse ) )
        using ( var zipStream = new GZipStream( compressedStream, CompressionMode.Decompress ) )
        using ( var resultStream = new MemoryStream() )
        {
          zipStream.CopyTo( resultStream );
          var xx = Encoding.UTF8.GetString( resultStream.ToArray() );
          jString = xx;
        }

        var jsonSerializerSettings = new JsonSerializerSettings()
        {
          TypeNameHandling = TypeNameHandling.All
        };

        var calls = JsonConvert.DeserializeObject( jString, jsonSerializerSettings ) as List<List<object>>;

        RhinoApp.WriteLine( String.Format( "Req #{1} || Computing {0} || {2} time(s)", selectedMethod.ToString(), requestNumber, calls.Count ) );

        List<List<object>> AllResults = new List<List<object>>();

        foreach ( var call in calls )
        {
          object invokeResult = null;
          var inputs = call.ToList();
          object[ ] invokeInputs = call.ToArray();

          if ( !selectedMethod.isCtor && !selectedMethod.isStatic )
          {
            object self = null;
            bool foundSelf = false;
            for ( int k = selectedMethod.inputs.Count - 1; k >= 0 && !foundSelf; k-- )
            {
              var inp = selectedMethod.inputs[ k ];
              if ( inp.isSelf )
              {
                self = inputs[ k ];
                inputs.RemoveAt( k );
                foundSelf = true;
              }
            }
            invokeInputs = inputs.ToArray();
            invokeResult = selectedMethod.methodBase.Invoke( self, invokeInputs );

          }
          else if ( selectedMethod.isStatic )
          {
            invokeResult = selectedMethod.methodBase.Invoke( null, invokeInputs );
          }
          else if ( selectedMethod.isCtor )
          {
            invokeResult = ( ( ConstructorInfo ) selectedMethod.methodBase ).Invoke( invokeInputs );
          }

          List<object> results = new List<object>();

          int j = 0;
          foreach ( var inp in selectedMethod.methodBase.GetParameters() )
          {
            if ( inp.ParameterType.IsByRef )
            {
              results.Add( invokeInputs[ j ] );
            }
            j++;
          }

          results.Add( invokeResult );
          AllResults.Add( results );
        }

        var copy = AllResults;

        var buff = Encoding.ASCII.GetBytes( JsonConvert.SerializeObject( AllResults, jsonSerializerSettings ) );

        var response = new Response();
        response.ContentType = "application/json";
        response.Contents = s => s.Write( buff, 0, buff.Length );

        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        var log = String.Format( "Req #{0} || finished {2} calls to {3} in {1}ms", requestNumber, watch.Elapsed.Milliseconds, calls.Count, selectedMethod.ToString() );
        RhinoApp.WriteLine( log ); ;

        response.Headers.Add( "X-Stomper-Info", log );

        return response;
      };
    }

  }
}
