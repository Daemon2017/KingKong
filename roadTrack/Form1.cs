using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using S22.Xmpp.Client;

namespace roadTrack
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static string hostname = "jabber.ru";
        static string username = "";
        static string password = "";
        XmppClient client = new XmppClient(hostname, username, password);

        bool locked = false;
        short lockedFrames = 0;

        bool previousDetection = false;

        int framesNum = 0;

        string workMode = "INC";

        // Для вычисления FPS
        private const int statLength = 15;
        private int statIndex = 0;
        private int statReady = 0;
        private int[] statCount = new int[statLength];

        private void button1_Click(object sender,
                                   EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                workMode = "INC";
            }
            else if (radioButton2.Checked == true)
            {
                workMode = "DEC";
            }

            // Подключаем видео
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var videoStreamConfig = new VideoCaptureDeviceForm();

            if (videoStreamConfig.ShowDialog() == DialogResult.OK)
            {
                videoSourcePlayer1.VideoSource = videoStreamConfig.VideoDevice;
            }

            videoSourcePlayer1.Start();
            timer1.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                client.Tls = true;
                client.Connect();
            }
            catch
            {
                MessageBox.Show("Не удалось соединиться с сервером, проверьте логин/пароль!",
                                "Ошибка",
                                MessageBoxButtons.OK);
            }
        }

        private void Form1_FormClosed(object sender,
                                      FormClosedEventArgs e)
        {
            if (client != null)
            {
                client.Close();
            }

            timer1.Stop();
            videoSourcePlayer1.Stop();
        }

        private void videoSourcePlayer1_NewFrame(object sender,
                                                 ref Bitmap inputImage)
        {
            if (framesNum > 1)
            {
                bool detection = DetectBarcode(inputImage);

                if (detection == true)
                {
                    if (locked == true)
                    {
                        if (lockedFrames < 10)
                        {
                            lockedFrames++;
                        }
                        else
                        {
                            locked = false;
                            lockedFrames = 0;
                        }
                    }
                    else
                    {
                        if (previousDetection == true)
                        {

                        }
                        else
                        {
                            DecodeBarcode(inputImage);
                        }
                    }
                }

                previousDetection = detection;
            }

            label2.Invoke((MethodInvoker)delegate
            {
                label2.Text = "Защелка: " + lockedFrames.ToString();
            });

            framesNum++;
        }

        private void timer1_Tick(object sender,
                                 EventArgs e)
        {
            if (videoSourcePlayer1.VideoSource != null)
            {
                statCount[statIndex] = videoSourcePlayer1.VideoSource.FramesReceived;

                if (++statIndex >= statLength)
                {
                    statIndex = 0;
                }

                if (statReady < statLength)
                {
                    statReady++;
                }

                float fps = 0;

                for (int i = 0; i < statReady; i++)
                {
                    fps += statCount[i];
                }

                fps /= statReady;

                label1.Text = "FPS: " + fps.ToString();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            videoSourcePlayer1.Stop();
        }
    }
}
