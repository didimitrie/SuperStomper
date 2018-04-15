using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace CloudCompute
{
  public class CuriousComputerAsync : GH_Component, IGH_VariableParameterComponent
  {

    public CcMethod selectedMethod = null;

    System.Timers.Timer executeTimer;
    bool isExecuting = false;
    bool justSetResults = false;

    List<List<object>> AllResults = new List<List<object>>();

    public CuriousComputerAsync( )
      : base( "CuriousComputerAsync", "CCA",
          "Search & invoke almost any Rhino.Geometry method (non-ui thread).",
          "Params", "CC" )
    {
    }

    public override bool Write( GH_IWriter writer )
    {
      if ( selectedMethod == null ) return false;

      writer.SetString( "ccmethod", selectedMethod.methodId );

      return base.Write( writer );
    }

    public override bool Read( GH_IReader reader )
    {
      try
      {
        var methodId = reader.GetString( "ccmethod" );
        selectedMethod = Cc.METHODS[ Convert.ToInt32( methodId ) ];
      }
      catch { }

      return base.Read( reader );
    }

    public override void AddedToDocument( GH_Document document )
    {
      base.AddedToDocument( document );

      executeTimer = new System.Timers.Timer( 200 ) { AutoReset = false, Enabled = false };
      executeTimer.Elapsed += ExecuteTimer_Elapsed;

      if ( selectedMethod == null )
      {
        var myForm = new PoopUp();
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
      AllResults = new List<List<object>>();

      foreach ( var call in calls )
      {
        object invokeResult = null;
        inputs = call.ToList();
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


      justSetResults = true;

      Action expire = ( ) => this.ExpireSolution( true );
      Rhino.RhinoApp.MainApplicationWindow.Invoke( expire );

      isExecuting = false;
      this.Message = "I am slacking";
    }

    public override bool AppendMenuItems( ToolStripDropDown menu )
    {

      //GH_DocumentObject.Menu_AppendItem( menu, "Show Form", ( sender, e ) =>
      //{
      //  var myForm = new PoopUp();
      //  var result = myForm.ShowDialog();
      //  myForm.StartPosition = FormStartPosition.Manual;
      //  Grasshopper.GUI.GH_WindowsFormUtil.CenterFormOnCursor( myForm, true );

      //  if ( result == DialogResult.OK )
      //  {
      //    selectedMethod = myForm.selectedMethod;
      //    SetIO();
      //    Action expire = ( ) => this.ExpireSolution( true );
      //    Rhino.RhinoApp.MainApplicationWindow.Invoke( expire );
      //  }

      //} );

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

      foreach ( var param in Params.ToArray() )
        Params.UnregisterParameter( param );

      // set inputs
      foreach ( var inp in selectedMethod.inputs )
      {
        Param_GenericObject newParam = new Param_GenericObject();
        newParam.NickName = string.Format( "{0}", inp.name );
        newParam.Name = string.Format( "{0} ({1})", inp.name, inp.type );

        if ( inp.type.Contains( "numerable" ) )
        {
          newParam.Name += " (List)";
          newParam.Access = GH_ParamAccess.list;
        }
        else newParam.Access = GH_ParamAccess.item;
        Params.RegisterInputParam( newParam );
      }

      // set outputs
      foreach ( var inp in selectedMethod.outputs )
      {
        Param_GenericObject newParam = new Param_GenericObject();
        newParam.NickName = string.Format( "{0}", inp.name );
        newParam.Name = string.Format( "{0} ({1})", inp.name, inp.type );

        if ( inp.type.Contains( "numerable" ) )
        {
          newParam.Name += " (List)";
          newParam.Access = GH_ParamAccess.list;
        }
        else newParam.Access = GH_ParamAccess.item;
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
      get { return new Guid( "713af539-1f63-41e1-9711-e5cf4dd16b3b" ); }
    }
  }

}
