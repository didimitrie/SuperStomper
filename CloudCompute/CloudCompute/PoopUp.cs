using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CloudCompute
{
  public partial class PoopUp : Form
  {

    public CcMethod selectedMethod;

    public PoopUp( )
    {
      InitializeComponent();

      var bindingList = new BindingList<CcMethod>( Cc.METHODS );
      var source = new BindingSource( bindingList, null );
      this.MethodGrid.DataSource = ConvertToDatatable( Cc.METHODS );

      this.MethodGrid.DefaultCellStyle.BackColor = Color.White;
      this.MethodGrid.DefaultCellStyle.Font = new Font( "Tahoma", 8 );
      this.MethodGrid.DefaultCellStyle.Padding = new Padding( 5 );

      this.MethodGrid.RowHeadersVisible = false;
      this.MethodGrid.ColumnHeadersVisible = false;

      this.MethodGrid.Columns[ "isCtor" ].Visible = false;
      this.MethodGrid.Columns[ "returnsSelf" ].Visible = false;
      this.MethodGrid.Columns[ "parent" ].DisplayIndex = 0;
      this.MethodGrid.Columns[ "parent" ].Width = 500;
      this.MethodGrid.Columns[ "name" ].DisplayIndex = 1;
      this.MethodGrid.Columns[ "name" ].Width = 342;
      this.MethodGrid.Columns[ "inpt" ].DisplayIndex = 2;
      this.MethodGrid.Columns[ "inpt" ].Width = 402;

      this.SearchBox.TextChanged += SearchBox_TextChanged;
      this.SearchBox.GotFocus += ( object sender, EventArgs e ) =>
      {
        if ( SearchBox.Text == "type to search" )
          SearchBox.Text = "";
        SearchBox.ForeColor = Color.Black;
      };

      this.SearchBox.LostFocus += ( object sender, EventArgs e ) =>
      {
        //SearchBox.Text = "type to search...";
        SearchBox.ForeColor = Color.DimGray;
      };

      this.KeyDown += PoopUp_KeyDown;
      this.SearchBox.KeyDown += PoopUp_KeyDown;

      this.KeyDown += ( object sender, KeyEventArgs e ) =>
      {
        if ( e.KeyCode == Keys.Enter )
        {
          var x = MethodGrid.CurrentRow.DataBoundItem as DataRowView;
          selectedMethod = Cc.METHODS[ Convert.ToInt32( ( string ) x.Row.ItemArray[ 7 ] ) ];

          this.DialogResult = DialogResult.OK;
          this.Close();
        }
      };
    }

    private void PoopUp_KeyDown( object sender, KeyEventArgs e )
    {
      if ( e.KeyCode == Keys.Down )
      {
        try
        {
          int i = MethodGrid.CurrentRow.Index + 1;
          MethodGrid.CurrentCell = MethodGrid.Rows[ i ].Cells[ 0 ];
          e.SuppressKeyPress = true;
        }
        catch { }
      }
      if ( e.KeyCode == Keys.Up )
      {
        try
        {
          int i = MethodGrid.CurrentRow.Index - 1;
          MethodGrid.CurrentCell = MethodGrid.Rows[ i ].Cells[ 0 ];
          e.SuppressKeyPress = true;
        }
        catch { }
      }
    }

    private void SearchBox_TextChanged( object sender, EventArgs e )
    {
      //Debug.WriteLine( SearchBox.Text );
      //if ( SearchBox.Text == "" ) return;

      string query = "";

      string[ ] parts = SearchBox.Text.Split( ' ' );

      int k = 0;

      foreach ( string part in parts )
      {
        if ( part == " " || part == "" ) continue;

        query += string.Format( "([name] LIKE '%{0}%' OR [parent] LIKE '%{0}%') ", part );
        query += "AND";
      }
      if ( parts.Length != 0 && query.LastIndexOf( " AND" ) != -1 )
        query = query.Substring( 0, query.LastIndexOf( " AND" ) );

      Debug.WriteLine( query );

      ( ( DataTable ) MethodGrid.DataSource ).DefaultView.RowFilter =
          string.Format( query );
    }

    // https://stackoverflow.com/questions/19076034/how-to-fill-a-datatable-with-listt
    private static DataTable ConvertToDatatable<T>( List<T> data )
    {
      PropertyDescriptorCollection props = TypeDescriptor.GetProperties( typeof( T ) );
      DataTable table = new DataTable();
      for ( int i = 0; i < props.Count; i++ )
      {
        PropertyDescriptor prop = props[ i ];
        if ( prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof( Nullable<> ) )
          table.Columns.Add( prop.Name, prop.PropertyType.GetGenericArguments()[ 0 ] );
        else
          table.Columns.Add( prop.Name, prop.PropertyType );
      }

      object[ ] values = new object[ props.Count ];
      foreach ( T item in data )
      {
        for ( int i = 0; i < values.Length; i++ )
        {
          values[ i ] = props[ i ].GetValue( item );
        }
        table.Rows.Add( values );
      }
      return table;
    }
  }
}
