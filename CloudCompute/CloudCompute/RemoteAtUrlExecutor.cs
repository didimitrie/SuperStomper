using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace CloudCompute
{
  public class RemoteUrlExecutor : GH_Component
  {
    public CcMethod selectedMethod = null;

    System.Timers.Timer executeTimer;
    bool isExecuting = false;
    bool justSetResults = false;

    List<List<object>> AllResults = new List<List<object>>();

    /// <summary>
    /// Initializes a new instance of the RemoteExecutor class.
    /// </summary>
    public RemoteUrlExecutor( )
      : base( "RemoteURLExecutor", "WW",
          "Paste in a URL of a Stomper method endpoint, and it will instantiate it and call it.",
          "Params", "CC" )
    {
    }

    public override bool Write( GH_IWriter writer )
    {
      if ( selectedMethod == null ) return false;

      writer.SetString( "ccmethod", selectedMethod.methodId );
      writer.SetString( "www", selectedMethod.url );
      return base.Write( writer );
    }

    public override bool Read( GH_IReader reader )
    {
      try
      {
        var methodId = reader.GetString( "ccmethod" );
        var www = reader.GetString( "www" );
        selectedMethod = Cc.METHODS[ Convert.ToInt32( methodId ) ];
        selectedMethod.url = www;
      }
      catch { }

      return base.Read( reader );
    }

    public override void AddedToDocument( GH_Document document )
    {
      base.AddedToDocument( document );

      executeTimer = new System.Timers.Timer( 500 ) { AutoReset = false, Enabled = false };
      executeTimer.Elapsed += ExecuteTimer_Elapsed;

      if ( selectedMethod == null )
      {
        var myForm = new UrlGrabber();
        var result = myForm.ShowDialog();
        myForm.StartPosition = FormStartPosition.Manual;
        Grasshopper.GUI.GH_WindowsFormUtil.CenterFormOnCursor( myForm, true );

        if ( result == DialogResult.OK )
        {
          selectedMethod = myForm.selectedMethod;
          SetIO();
        }
      }
      else
      {
        //SetIO(); // io is already set you
      }
    }


    public override void RemovedFromDocument( GH_Document document )
    {
      executeTimer.Dispose();
      base.RemovedFromDocument( document );
    }

    public List<object> PrimeParam( List<object> inputs, int mIndex )
    {
      Type theType = null;
      try
      {
        var pinfo = selectedMethod.methodBase.GetParameters()[ mIndex ];
        theType = pinfo.ParameterType;
      }
      catch
      {
        theType = selectedMethod.methodBase.DeclaringType;
      }

      for ( int i = 0; i < inputs.Count; i++ )
      {
        var obj = inputs[ i ];

        if ( typeof( IEnumerable ).IsAssignableFrom( theType ) )
        {
          var listType = typeof( List<> ).MakeGenericType( theType.GetGenericArguments()[ 0 ] );
          var myList = Activator.CreateInstance( listType );

          foreach ( object myobj in ( ( IEnumerable ) obj ) )
            myList.GetType().GetMethod( "Add" ).Invoke( myList, new object[ ] { myobj } );

          inputs[ i ] = myList;
          continue;
        }

        try
        {
          inputs[ i ] = System.Convert.ChangeType( inputs[ i ], theType );
        }
        catch
        {
          inputs[ i ] = inputs[ i ]; // ?? the fck
        }

        i++;
      }

      return inputs;
    }


    private void ExecuteTimer_Elapsed( object sender, System.Timers.ElapsedEventArgs e )
    {
      if ( selectedMethod == null ) return;
      isExecuting = true;
      this.Message = "I am running";

      // Get the data
      List<List<object>> datas = new List<List<object>>();
      List<object> inputs = new List<object>();

      int m = 0;

      for ( int i = 0; i < selectedMethod.inputs.Count; i++ )
      {
        if ( i < Params.Input.Count )
        {
          var myParam = Params.Input[ i ];
          List<object> data = new List<object>();
          foreach ( object o in myParam.VolatileData.AllData( false ) )
            data.Add( o );
          // remove gh bs
          data = data.Select( obj => obj.GetType().GetProperty( "Value" ).GetValue( obj ) ).ToList();

          if ( myParam.Name.Contains( "(List)" ) )
          {
            datas.Add( PrimeParam( new List<object>() { data }, i ) );
          }
          else
          {
            datas.Add( PrimeParam( data, i ) );
          }
        }
        else
        {
          datas.Add( new List<object>() { null } );
        }
      }


      var calls = datas.CartesianProduct().ToList();
      List<List<object>> theCalls = new List<List<object>>();
      calls.ForEach( obj =>
      {
        List<object> myList = new List<object>();
        foreach ( var ob in obj ) myList.Add( ob );
        theCalls.Add( myList );
      } );

      var clsz = theCalls;
      // bust some balls

      var jsonSerializerSettings = new JsonSerializerSettings()
      {
        TypeNameHandling = TypeNameHandling.All
      };
      var jsonString = JsonConvert.SerializeObject( theCalls, jsonSerializerSettings );

      var Client = new HttpClient();
      var TheRequest = new HttpRequestMessage( HttpMethod.Post, selectedMethod.url );

      TheRequest.Content = new System.Net.Http.StringContent( jsonString );
      TheRequest.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse( "application/json" );
      TheRequest.Method = new System.Net.Http.HttpMethod( "POST" );
      TheRequest.Headers.Accept.Add( new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue( "application/json" ) );
      TheRequest.Headers.AcceptEncoding.Add( new System.Net.Http.Headers.StringWithQualityHeaderValue( "gzip" ) );

      TheRequest.Content = new GzipContent( TheRequest.Content );

      var response = Client.SendAsync( TheRequest ).Result;


      string responseData;
      var bytes = response.Content.ReadAsByteArrayAsync().Result;

      using ( var compressedStream = new MemoryStream( bytes ) )
      using ( var zipStream = new System.IO.Compression.GZipStream( compressedStream, System.IO.Compression.CompressionMode.Decompress ) )
      using ( var resultStream = new MemoryStream() )
      {
        zipStream.CopyTo( resultStream );
        var xx = System.Text.Encoding.UTF8.GetString( resultStream.ToArray() );
        responseData = xx;
      }

      AllResults = JsonConvert.DeserializeObject( responseData, jsonSerializerSettings ) as List<List<object>>;

      justSetResults = true;
      Action expire = ( ) => this.ExpireSolution( true );
      Rhino.RhinoApp.MainApplicationWindow.Invoke( expire );

      isExecuting = false;
      this.Message = "I am slacking";
    }

    public override bool AppendMenuItems( ToolStripDropDown menu )
    {

      return base.AppendMenuItems( menu );

    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams( GH_Component.GH_InputParamManager pManager )
    {
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams( GH_Component.GH_OutputParamManager pManager )
    {
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
    /// to store data in output parameters.</param>
    protected override void SolveInstance( IGH_DataAccess DA )
    {
      if ( AllResults.Count != 0 )
      {
        int i = 0;

        foreach ( var outp in Params.Output )
        {
          List<object> myOut = new List<object>();
          foreach ( var res in AllResults )
          {
            myOut.Add( res[ i ] );
          }

          DA.SetDataList( i, myOut );

          i++;
        }
        if ( justSetResults )
        {
          justSetResults = false;
          return;
        }
      }

      executeTimer.Start();
    }

    public void SetIO( )
    {
      if ( selectedMethod == null ) return;

      this.Name = this.NickName = selectedMethod.name;

      foreach ( var param in Params.Input.ToArray() )
      {
        Params.UnregisterParameter( param );
        //Params.UnregisterInputParameter( param );
      }

      foreach ( var param in Params.Output.ToArray() )
      {
        Params.UnregisterParameter( param );
        //Params.UnregisterOutputParameter( param );
      }

      foreach ( var inp in selectedMethod.inputs )
      {
        Param_GenericObject newParam = new Param_GenericObject();
        newParam.NickName = string.Format( "{0}", inp.name );
        newParam.Name = string.Format( "{0} ({1})", inp.name, inp.type );
        if ( !inp.isOptional )
          newParam.Optional = false;

        if ( inp.type.Contains( "numerable" ) )
          newParam.Name += " (List)";

        newParam.Access = GH_ParamAccess.list;

        Params.RegisterInputParam( newParam );
      }

      foreach ( var inp in selectedMethod.outputs )
      {
        Param_GenericObject newParam = new Param_GenericObject();

        newParam.NickName = string.Format( "{0}", inp.name );
        newParam.Name = string.Format( "{0} ({1})", inp.name, inp.type );

        newParam.Access = GH_ParamAccess.list;

        Params.RegisterOutputParam( newParam );
      }

      Params.OnParametersChanged();
    }

    public bool CanInsertParameter( GH_ParameterSide side, int index )
    {
      return false;
    }

    public bool CanRemoveParameter( GH_ParameterSide side, int index )
    {
      return false;
    }

    public IGH_Param CreateParameter( GH_ParameterSide side, int index )
    {
      throw new NotImplementedException();
    }

    public bool DestroyParameter( GH_ParameterSide side, int index )
    {
      throw new NotImplementedException();
    }

    public void VariableParameterMaintenance( )
    {

    }

    /// <summary>
    /// Provides an Icon for every component that will be visible in the User Interface.
    /// Icons need to be 24x24 pixels.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        // You can add image files to your project resources and access them like this:
        //return Resources.IconForThisComponent;
        return null;
      }
    }

    /// <summary>
    /// Each component must have a unique Guid to identify it. 
    /// It is vital this Guid doesn't change otherwise old ghx files 
    /// that use the old ID will partially fail during loading.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid( "F7E37FA9-90A5-4053-B70B-972A8AE9F6CB" ); }
    }
  }
}