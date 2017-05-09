using System;
using System.Windows.Forms;

namespace MyLittleServer
{
    public partial class Namer : Form
    {
        public Namer()
        {
            InitializeComponent();
        }

        public string newName;

        private void button1_Click(object sender, EventArgs e)
        {
            newName = textBox1.Text;

            Close();
        }
    }
}
