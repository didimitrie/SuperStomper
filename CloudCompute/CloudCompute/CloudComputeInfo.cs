using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace CloudCompute
{

  public class CcLoader : GH_AssemblyPriority
  {
    public override GH_LoadingInstruction PriorityLoad( )
    {
      CreateMethodsCollections();
      return GH_LoadingInstruction.Proceed;
    }

    public void CreateMethodsCollections( )
    {
      var asm = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault( assembly => assembly.GetName().Name == "RhinoCommon" );

      //var types = asm.GetTypes().Where( type => type.FullName.Contains( "Rhino.Geometry.Intersect" ) ).ToList();
      //var types = asm.GetTypes().Where( type => type.FullName == "Rhino.Geometry.Arc" ).ToList();
      var types = asm.GetTypes().Where( type => type.FullName.Contains( "Rhino.Geometry" ) ).ToList();

      string[ ] dontexpose = new string[ ] { "ToString", "GetHashCode", "GetType", "get_Unset", "Equals", "HasFlag" };

      List<CcMethodCollection> GlobalList = new List<CcMethodCollection>();
      int k = 0;
      foreach ( Type myType in types )
      {
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
          Cc.METHODS.Add( myMethod );
        }
      }
    }
  }

  public static class Cc
  {
    public static List<CcMethod> METHODS = new List<CcMethod>();

    //brilliant: https://stackoverflow.com/questions/3093622/generating-all-possible-combinations
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>( this IEnumerable<IEnumerable<T>> sequences )
    {
      IEnumerable<IEnumerable<T>> emptyProduct = new[ ] { Enumerable.Empty<T>() };
      return sequences.Aggregate(
          emptyProduct,
          ( accumulator, sequence ) =>
              from accseq in accumulator
              from item in sequence
              select accseq.Concat( new[ ] { item } )
          ).ToList();
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
    public bool returnsSelf { get; set; } = false;
    public List<CcParam> inputs { get; set; } = new List<CcParam>();
    public List<CcParam> outputs { get; set; } = new List<CcParam>();

    public MethodBase methodBase;

    [JsonIgnore]
    public string url;

    public string methodId { get; set; } = "0";

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
    public bool isOptional { get; set; } = false;
    public string type { get; set; }

    public override string ToString( )
    {
      return "CcParam: " + name + " (type: " + ( type.Length > 10 ? type.Substring( 0, 10 ) : type ) + " isByRef: " + isByRef + ")";
    }

  }

  public class CloudComputeInfo : GH_AssemblyInfo
  {
    public override string Name
    {
      get
      {
        return "CloudCompute";
      }
    }
    public override Bitmap Icon
    {
      get
      {
        //Return a 24x24 pixel bitmap to represent this GHA library.
        return null;
      }
    }
    public override string Description
    {
      get
      {
        //Return a short string describing the purpose of this GHA library.
        return "";
      }
    }
    public override Guid Id
    {
      get
      {
        return new Guid( "9bbe1921-5779-4743-8097-c51f0b02c6c1" );
      }
    }

    public override string AuthorName
    {
      get
      {
        //Return a string identifying you or your company.
        return "";
      }
    }
    public override string AuthorContact
    {
      get
      {
        //Return a string representing your preferred contact details.
        return "";
      }
    }
  }
}
