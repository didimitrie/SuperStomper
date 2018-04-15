using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace CloudCompute
{
  public class CuriousComputer : GH_Component, IGH_VariableParameterComponent
  {

    public CcMethod selectedMethod = null;

    bool isExecuting = false;
    bool justSetResults = false;

    public CuriousComputer( )
      : base( "CuriousComputer", "CC",
          "Search & invoke almost any Rhino.Geometry method.",
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
    }

    /// <summary>
    /// Tries and makes sure the param is the correct type for the invoke.
    /// </summary>
    /// <param name="inputs">the data collected for the param</param>
    /// <param name="mIndex">the parameter index of the method</param>
    /// <returns></returns>
    public object PrimeParam( object input, int mIndex )
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

      if ( typeof( IEnumerable ).IsAssignableFrom( theType ) )
      {
        var listType = typeof( List<> ).MakeGenericType( theType.GetGenericArguments()[ 0 ] );
        var myList = Activator.CreateInstance( listType );

        foreach ( object myobj in ( ( IEnumerable ) input ) )
          myList.GetType().GetMethod( "Add" ).Invoke( myList, new object[ ] { myobj } );
        input = myList;
      }
      else
      {
        try { input = System.Convert.ChangeType( input, theType ); } catch { }
      }

      return input;
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
      if ( selectedMethod == null ) return;

      List<object> inputs = new List<object>();

      for ( int i = 0; i < selectedMethod.inputs.Count; i++ )
      {
        if ( i < Params.Input.Count )
        {
          var myParam = Params.Input[ i ];
          if ( myParam.Name.Contains( "(List)" ) )
          {
            var myList = new List<object>();
            DA.GetDataList( i, myList );
            myList = myList.Select( obj => obj.GetType().GetProperty( "Value" ).GetValue( obj ) ).ToList();
            inputs.Add( PrimeParam( myList, i ) );
          }
          else
          {
            object myObject = null;
            DA.GetData( i, ref myObject );
            myObject = myObject.GetType().GetProperty( "Value" ).GetValue( myObject );
            inputs.Add( PrimeParam( myObject, i ) );
          }
        }
        else
        {
          inputs.Add( null );
        }
      }

      foreach(var param in selectedMethod.outputs )
      {
        if(param.isByRef)
        {
          inputs.Add( null );
        }
      }


      object invokeResult = null;
      object[ ] invokeInputs = inputs.ToArray();

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


      if ( results.Count != 0 )
      {
        int i = 0;

        foreach ( var outp in Params.Output )
        {
          if ( outp.Name.Contains( "(List)" ) )
          {
            var myList = new List<GH_ObjectWrapper>();
            foreach ( var obj in ( IEnumerable ) results[ i ] )
              myList.Add( new GH_ObjectWrapper() { Value = obj } );
            DA.SetDataList( i, myList );
          }
          else
          {
            DA.SetData( i, new GH_ObjectWrapper() { Value = results[ i ] } );
          }
          i++;
        }
      }
    }


    // creates the input output params for this component
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

        if ( inp.type.Contains( "[" ) )
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
      get { return new Guid( "6315FCB5-7C0B-4DEA-AEF1-C936A8F07651" ); }
    }
  }

}
