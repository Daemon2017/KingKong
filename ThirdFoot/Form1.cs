using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ThirdFoot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        float[,] data = new float[20, 76800];

        private void button1_Click(object sender, EventArgs e)
        {
            Bitmap bmp;

            for (int n = 0; n < data.GetLength(0); n++)
            {
                bmp = new Bitmap(n + ".jpg");

                int a = 0;

                for (int x = 0; x < bmp.Width; ++x)
                {
                    for (int y = 0; y < bmp.Height; ++y)
                    {
                        Color curr = bmp.GetPixel(x, y);
                        data[n, a] = curr.GetBrightness();
                        a++;
                    }
                }

                a = 0;
            }

            using (StreamWriter stream = new StreamWriter("Ideal_Input_Tactile.cfg"))
            {
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    for (int k = 0; k < data.GetLength(1); k++)
                    {
                        stream.Write("{0};", data[i, k]);
                    }
                    stream.WriteLine();
                }
            }

            data = null;
        }
    }
}
