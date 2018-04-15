namespace CloudCompute
{
  partial class UrlGrabber
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
      this.MethodBox = new System.Windows.Forms.TextBox();
      this.TreeTree = new System.Windows.Forms.TreeView();
      this.SuspendLayout();
      // 
      // MethodBox
      // 
      this.MethodBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.MethodBox.Cursor = System.Windows.Forms.Cursors.Hand;
      this.MethodBox.Font = new System.Drawing.Font("Arial", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.MethodBox.ForeColor = System.Drawing.SystemColors.WindowFrame;
      this.MethodBox.Location = new System.Drawing.Point(0, 1);
      this.MethodBox.MinimumSize = new System.Drawing.Size(0, 100);
      this.MethodBox.Name = "MethodBox";
      this.MethodBox.Size = new System.Drawing.Size(840, 62);
      this.MethodBox.TabIndex = 1;
      this.MethodBox.Text = "method url";
      this.MethodBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      // 
      // TreeTree
      // 
      this.TreeTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.TreeTree.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.TreeTree.Location = new System.Drawing.Point(0, 69);
      this.TreeTree.Name = "TreeTree";
      this.TreeTree.Size = new System.Drawing.Size(840, 451);
      this.TreeTree.TabIndex = 2;
      // 
      // UrlGrabber
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(844, 519);
      this.Controls.Add(this.TreeTree);
      this.Controls.Add(this.MethodBox);
      this.KeyPreview = true;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "UrlGrabber";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "UrlGrabber";
      this.TopMost = true;
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox MethodBox;
    private System.Windows.Forms.TreeView TreeTree;
  }
}