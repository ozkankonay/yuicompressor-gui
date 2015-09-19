using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace yuicompressor_gui
{
    public partial class FormTextOutput : Form
    {
        public FormTextOutput()
        {
            InitializeComponent();
        }

        public void SetJsContent(string text)
        {
            this.richTextBox1.Text = text;

            if (string.IsNullOrEmpty(this.richTextBox2.Text)) splitContainer1.Panel2Collapsed = true;
            splitContainer1.Panel1Collapsed = false;
        }

        public void SetCssContent(string text)
        {
            this.richTextBox2.Text = text;

            if (string.IsNullOrEmpty(this.richTextBox1.Text)) splitContainer1.Panel1Collapsed = true;
            splitContainer1.Panel2Collapsed = false;
        }



    }
}
