namespace CloudCompute
{
  partial class PoopUp
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose();
      }
      base.Dispose( disposing );
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent( )
    {
      this.components = new System.ComponentModel.Container();
      this.SearchBox = new System.Windows.Forms.TextBox();
      this.MethodGrid = new System.Windows.Forms.DataGridView();
      this.ccBindingSource = new System.Windows.Forms.BindingSource(this.components);
      this.ccBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
      ((System.ComponentModel.ISupportInitialize)(this.MethodGrid)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.ccBindingSource)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.ccBindingSource1)).BeginInit();
      this.SuspendLayout();
      // 
      // SearchBox
      // 
      this.SearchBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.SearchBox.Cursor = System.Windows.Forms.Cursors.Hand;
      this.SearchBox.Font = new System.Drawing.Font("Arial", 40.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.SearchBox.ForeColor = System.Drawing.SystemColors.WindowFrame;
      this.SearchBox.Location = new System.Drawing.Point(1, 0);
      this.SearchBox.Name = "SearchBox";
      this.SearchBox.Size = new System.Drawing.Size(1242, 124);
      this.SearchBox.TabIndex = 0;
      this.SearchBox.Text = "type to search";
      this.SearchBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      // 
      // MethodGrid
      // 
      this.MethodGrid.AllowUserToAddRows = false;
      this.MethodGrid.AllowUserToDeleteRows = false;
      this.MethodGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.MethodGrid.Location = new System.Drawing.Point(1, 124);
      this.MethodGrid.MultiSelect = false;
      this.MethodGrid.Name = "MethodGrid";
      this.MethodGrid.ReadOnly = true;
      this.MethodGrid.RowHeadersVisible = false;
      this.MethodGrid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
      this.MethodGrid.RowTemplate.Height = 50;
      this.MethodGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
      this.MethodGrid.ShowEditingIcon = false;
      this.MethodGrid.Size = new System.Drawing.Size(1242, 534);
      this.MethodGrid.TabIndex = 1;
      // 
      // ccBindingSource
      // 
      this.ccBindingSource.DataSource = typeof(CloudCompute.Cc);
      // 
      // ccBindingSource1
      // 
      this.ccBindingSource1.DataSource = typeof(CloudCompute.Cc);
      // 
      // PoopUp
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(28F, 55F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.SystemColors.Control;
      this.ClientSize = new System.Drawing.Size(1242, 654);
      this.Controls.Add(this.MethodGrid);
      this.Controls.Add(this.SearchBox);
      this.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.KeyPreview = true;
      this.Margin = new System.Windows.Forms.Padding(7, 7, 7, 7);
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "PoopUp";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "Poop Up 💩";
      this.TopMost = true;
      ((System.ComponentModel.ISupportInitialize)(this.MethodGrid)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.ccBindingSource)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.ccBindingSource1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox SearchBox;
    private System.Windows.Forms.DataGridView MethodGrid;
    private System.Windows.Forms.BindingSource ccBindingSource1;
    private System.Windows.Forms.BindingSource ccBindingSource;
  }
}