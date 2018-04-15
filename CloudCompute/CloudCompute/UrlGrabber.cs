using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudCompute
{
  public partial class UrlGrabber : Form
  {

    public CcMethod selectedMethod;
    public CcMethod responseMethod;

    public string methodUrl;

    public UrlGrabber( )
    {
      InitializeComponent();

      MethodBox.TextChanged += MethodBox_TextChanged;

      this.KeyDown += ( object sender, KeyEventArgs e ) =>
      {
        if ( e.KeyCode == Keys.Enter )
        {
          if ( this.responseMethod == null )
          {
            MessageBox.Show( "No method selected. Sorry!" );
            return;
          }

          this.selectedMethod = this.responseMethod;

          this.DialogResult = DialogResult.OK;
          this.Close();
        }
      };
    }

    private void MethodBox_TextChanged( object sender, EventArgs e )
    {
      Uri uriResult;
      bool result = Uri.TryCreate( MethodBox.Text, UriKind.Absolute, out uriResult )
          && ( uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps );

      if ( !result ) return;

      var httpWebRequest = ( HttpWebRequest ) WebRequest.Create( uriResult );

      httpWebRequest.Method = "GET";

      try
      {
        var httpResponse = ( HttpWebResponse ) httpWebRequest.GetResponse();

        //basic check so we don't load the BBC
        if ( httpResponse.Headers[ "content-type" ] != "application/json" ) return;

        using ( var streamReader = new StreamReader( httpResponse.GetResponseStream() ) )
        {
          var responseString = streamReader.ReadToEnd();
          var myres = result;

          TreeTree.Nodes.Clear();

          TreeNode myTree = Json2Tree( JsonConvert.DeserializeObject<JObject>( responseString ) );


          TreeTree.Nodes.Add( myTree );
          TreeTree.Refresh();

          this.responseMethod = JsonConvert.DeserializeObject<CcMethod>( responseString );
          if ( this.responseMethod == null ) return;

          this.responseMethod.url = uriResult.ToString();
          this.responseMethod.methodBase = Cc.METHODS[ Convert.ToInt32( this.responseMethod.methodId ) ].methodBase;
        }
      }
      catch
      {

      }
    }


    private TreeNode Json2Tree( JObject obj )
    {
      //create the parent node
      TreeNode parent = new TreeNode();
      //loop through the obj. all token should be pair<key, value>
      foreach ( var token in obj )
      {
        //change the display Content of the parent
        parent.Text = token.Key.ToString();
        //create the child node
        TreeNode child = new TreeNode();
        child.Text = token.Key.ToString();
        //check if the value is of type obj recall the method
        if ( token.Value.Type.ToString() == "Object" )
        {
          // child.Text = token.Key.ToString();
          //create a new JObject using the the Token.value
          JObject o = ( JObject ) token.Value;
          //recall the method
          child = Json2Tree( o );
          //add the child to the parentNode
          parent.Nodes.Add( child );
        }
        //if type is of array
        else if ( token.Value.Type.ToString() == "Array" )
        {
          int ix = -1;
          //  child.Text = token.Key.ToString();
          //loop though the array
          foreach ( var itm in token.Value )
          {
            //check if value is an Array of objects
            if ( itm.Type.ToString() == "Object" )
            {
              TreeNode objTN = new TreeNode();
              //child.Text = token.Key.ToString();
              //call back the method
              ix++;

              JObject o = ( JObject ) itm;
              objTN = Json2Tree( o );
              objTN.Text = token.Key.ToString() + "[" + ix + "]";
              child.Nodes.Add( objTN );
              //parent.Nodes.Add(child);
            }
            //regular array string, int, etc
            else if ( itm.Type.ToString() == "Array" )
            {
              ix++;
              TreeNode dataArray = new TreeNode();
              foreach ( var data in itm )
              {
                dataArray.Text = token.Key.ToString() + "[" + ix + "]";
                dataArray.Nodes.Add( data.ToString() );
              }
              child.Nodes.Add( dataArray );
            }

            else
            {
              child.Nodes.Add( itm.ToString() );
            }
          }
          parent.Nodes.Add( child );
        }
        else
        {
          //if token.Value is not nested
          // child.Text = token.Key.ToString();
          //change the value into N/A if value == null or an empty string 
          if ( token.Value.ToString() == "" )
            child.Nodes.Add( "N/A" );
          else
            child.Nodes.Add( token.Value.ToString() );
          parent.Nodes.Add( child );
        }
      }
      return parent;

    }
  }
}
