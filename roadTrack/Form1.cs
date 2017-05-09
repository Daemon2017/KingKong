using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using AForge.Video.DirectShow;
using MessagingToolkit.Barcode;
using S22.Xmpp.Client;

namespace roadTrack
{
    public partial class Form1 : Form
    {
        static string hostname = "jabber.ru";
        static string username = "";
        static string password = "";
        XmppClient client = new XmppClient(hostname, username, password);

        bool locked = false;
        short lockedFrames = 0;

        int framesNum = 0;

        string workMode = "INC";

        // Для вычисления FPS
        private const int statLength = 15;
        private int statIndex = 0;
        private int statReady = 0;
        private int[] statCount = new int[statLength];

        public Form1()
        {
            InitializeComponent();
        }

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

            videoSourcePlayer1.Stop();
            timer1.Stop();
        }

        private void videoSourcePlayer1_NewFrame(object sender,
                                                 ref Bitmap inputImage)
        {
            if (framesNum > 1)
            {
                if (locked == false)
                {
                    DecodeBarcode(inputImage);
                }
                else
                {
                    if (lockedFrames < 15)
                    {
                        lockedFrames++;
                    }
                    else
                    {
                        locked = false;
                        lockedFrames = 0;
                    }
                }
            }

            label2.Invoke((MethodInvoker)delegate
            {
                label2.Text = "Защелка: " + lockedFrames.ToString();
            });

            framesNum++;
        }

        private void DecodeBarcode(Bitmap image)
        {
            Dictionary<DecodeOptions, object> decodingOptions = new Dictionary<DecodeOptions, object>();
            List<BarcodeFormat> possibleFormats = new List<BarcodeFormat>(1);

            possibleFormats.Add(BarcodeFormat.EAN13);
            decodingOptions.Add(DecodeOptions.PossibleFormats, possibleFormats);
            decodingOptions.Add(DecodeOptions.PureBarcode, "");
            decodingOptions.Add(DecodeOptions.AutoRotate, true);

            try
            {
                BarcodeDecoder barcodeDecoder = new BarcodeDecoder();
                Result newDecodedResult = barcodeDecoder.Decode(image, decodingOptions);

                image = null;

                Connect(newDecodedResult.Text, workMode);

                locked = true;
            }
            catch (NotFoundException)
            {

            }
        }

        private void Connect(string barcode, string mode)
        {
            client.SendMessage(username + "@" + hostname, barcode + "," + mode);
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
            videoSourcePlayer1.Stop();
            timer1.Stop();
        }
    }
}
