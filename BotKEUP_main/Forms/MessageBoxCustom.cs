using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BotKEUP_main.Forms
{
    public partial class MessageBoxCustom : Form
    {
        public string text;
        public string title;
        public bool needconfirm;
        public MessageBoxCustom()
        {
            InitializeComponent();
        }

        private void MessageBoxCustom_Load(object sender, EventArgs e)
        {
            SystemSounds.Beep.Play();
            guna2HtmlLabel1.Text = title;
            guna2HtmlLabel2.Text = text;
            guna2TileButton1.Visible = needconfirm;
        }

        private void guna2HtmlLabel1_MouseMove(object sender, MouseEventArgs e)
        {
            guna2DragControl1.TargetControl = (sender as Control);
        }

        private void guna2TileButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void guna2TileButton1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
