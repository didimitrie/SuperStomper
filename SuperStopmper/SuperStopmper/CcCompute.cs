using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace SuperStopmper
{
  public static class Cc
  {
    public static List<CcMethod> METHODS = new List<CcMethod>();
    public static List<CcMethodCollection> MethodCollections = new List<CcMethodCollection>();

    public static void CreateMethodsCollections( )
    {
      var point3d = new Point3d();

      var asm = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault( assembly => assembly.GetName().Name == "RhinoCommon" );

      var types = asm.GetTypes().Where( type => type.FullName.Contains( "Rhino.Geometry" ) ).ToList();

      string[ ] dontexpose = new string[ ] { "ToString", "GetHashCode", "GetType", "get_Unset", "Equals", "HasFlag" };


      int k = 0;
      foreach ( Type myType in types )
      {
        CcMethodCollection myColl = new CcMethodCollection() { name = myType.Name };
        var methods = myType.GetMethods();
        foreach ( var method in methods )
        {
          var myMethod = new CcMethod();
          if ( dontexpose.Any( method.Name.Contains ) ) continue;

          myMethod.name = method.Name;
          myMethod.methodBase = method;
          myMethod.isStatic = method.IsStatic;

          var pinfos = method.GetParameters();
          foreach ( var pi in pinfos )
          {
            if ( pi.ParameterType.IsByRef )
              myMethod.outputs.Add( new CcParam() { name = pi.Name, type = pi.ParameterType.FullName, isByRef = pi.ParameterType.IsByRef, isOptional = pi.IsOptional } );
            else
              myMethod.inputs.Add( new CcParam() { name = pi.Name, type = pi.ParameterType.FullName, isByRef = pi.ParameterType.IsByRef, isOptional = pi.IsOptional } );
          }

          if ( !method.IsStatic )
            myMethod.inputs.Add( new CcParam() { name = myType.Name, type = myType.FullName, isSelf = true } );

          if ( method.ReturnParameter.ParameterType == typeof( void ) )
          {
            myMethod.outputs.Add( new CcParam() { name = "Out " + myType.Name, type = myType.FullName } );
            myMethod.returnsSelf = true;
          }
          else
          {
            myMethod.outputs.Add( new CcParam() { name = method.ReturnParameter.ParameterType.Name, type = method.ReturnParameter.ParameterType.FullName } );
          }

          myMethod.parent = myType.FullName;
          myMethod.methodId = k++.ToString();

          myColl.methods.Add( myMethod );
          Cc.METHODS.Add( myMethod );
        }

        // CTORS
        var ctors = myType.GetConstructors();
        foreach ( var ctor in ctors )
        {
          var myMethod = new CcMethod();
          myMethod.isCtor = true;
          myMethod.name = "New " + myType.Name;
          myMethod.methodBase = ctor;

          var pinfos = ctor.GetParameters();
          foreach ( var pi in pinfos )
          {
            if ( pi.ParameterType.IsByRef )
              myMethod.outputs.Add( new CcParam() { name = pi.Name, type = pi.ParameterType.FullName, isByRef = pi.ParameterType.IsByRef, isOptional = pi.IsOptional } );
            else
              myMethod.inputs.Add( new CcParam() { name = pi.Name, type = pi.ParameterType.FullName, isByRef = pi.ParameterType.IsByRef, isOptional = pi.IsOptional } );
          }
          myMethod.outputs.Add(
            new CcParam() { name = myType.Name, type = myType.FullName, isByRef = false } );

          myMethod.parent = myType.FullName;
          myMethod.methodId = k++.ToString();

          myColl.methods.Add( myMethod );
          Cc.METHODS.Add( myMethod );
        }

        MethodCollections.Add( myColl );
      }

      //jString = JsonConvert.SerializeObject( METHODS );
      //jBuff = Encoding.UTF8.GetBytes( jString );
    }
  }

  public class CcMethodCollection
  {
    public string name { get; set; }
    public List<CcMethod> methods { get; set; } = new List<CcMethod>();

    public override string ToString( )
    {
      return "CcMethodCollection: " + name + " (" + methods.Count + " methods)";
    }
  }

  public class CcMethod
  {
    public string name { get; set; }

    public string parent { get; set; }

    public bool isCtor { get; set; } = false;

    public bool isStatic { get; set; } = false;

    [JsonIgnore]
    public bool returnsSelf { get; set; } = false;

    public List<CcParam> inputs { get; set; } = new List<CcParam>();

    public List<CcParam> outputs { get; set; } = new List<CcParam>();

    [JsonIgnore]
    public MethodBase methodBase;

    public string methodId { get; set; } = "0";

    [JsonIgnore]
    public string inpt
    {
      get
      {
        string ret = "";
        foreach ( var param in inputs )
        {
          var tt = param.type != null ? param.type.Split( '.' ) : new string[ ] { "" };
          ret += tt.Last() + " " + param.name + ", ";
        }
        return ret;
      }
    }

    public override string ToString( )
    {
      return "CcMethod: " + name + " (" + inputs.Count + " inputs, " + outputs.Count + " outputs)" + " ctor: " + isCtor;
    }
  }

  public class CcParam
  {
    public string name { get; set; }
    public bool isByRef { get; set; } = false;
    public bool isSelf { get; set; } = false;
    [JsonIgnore]
    public bool isOptional { get; set; } = false;

    public string type { get; set; }

    public override string ToString( )
    {
      return "CcParam: " + name + " (type: " + ( type.Length > 10 ? type.Substring( 0, 10 ) : type ) + " isByRef: " + isByRef + ")";
    }

  }
}
