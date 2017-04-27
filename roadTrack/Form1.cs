using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

using AForge.Video.DirectShow;

using MessagingToolkit.Barcode;

using ConvNetSharp.Serialization;
using System.Net.Sockets;

namespace roadTrack
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        int framesNum = 0;

        string workMode = "INC";

        Bitmap myImage;

        Result oldDecodedResult;

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

            var form = new VideoCaptureDeviceForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                videoSourcePlayer1.VideoSource = form.VideoDevice;
            }

            videoSourcePlayer1.Start();
            timer1.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                PrepareData();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Не найдены некоторый .cfg файлы: обучение невозможно!",
                                "Отсутствует файл",
                                MessageBoxButtons.OK);
            }

            net = null;
            try
            {
                var json_temp = File.ReadAllLines("NetworkStructure.json");
                string json = string.Join("", json_temp);
                net = SerializationExtensions.FromJSON(json);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Не найден файл с обученной нейросетью. Необходимо обучение!",
                                "Отсутствует файл",
                                MessageBoxButtons.OK);
            }
        }

        private void Form1_FormClosed(object sender,
                                      FormClosedEventArgs e)
        {
            videoSourcePlayer1.Stop();
            timer1.Stop();
        }

        private void videoSourcePlayer1_NewFrame(object sender,
                                                 ref Bitmap inputImage)
        {
            if (framesNum > 1)
            {
                // Загружаем
                Bitmap workImage = (Bitmap)inputImage.Clone();

                // Создаем копию для работы в СНС
                myImage = workImage;

                DecodeBarcode(workImage);
            }

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

                    if (newDecodedResult != oldDecodedResult)
                    {
                        Connect(newDecodedResult.Text, workMode);
                    }

                    oldDecodedResult = newDecodedResult;
            }
            catch (NotFoundException)
            {

            }
        }

        private void Connect(string barcode, string mode)
        {
            try
            {
                TcpClient client = new TcpClient("127.0.0.1", 13000);

                byte[] data = System.Text.Encoding.ASCII.GetBytes(barcode + "," + mode);

                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                data = new byte[256];

                string responseData = string.Empty;

                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException)
            {

            }
            catch (SocketException)
            {

            }
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

        private void button3_Click(object sender, EventArgs e)
        {
            net = null;

            CreateNetworkForTactile();
            TrainNetworkForTactile(0.01);

            MessageBox.Show("Обучение завершено!",
                            "Готово",
                            MessageBoxButtons.OK);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            videoSourcePlayer1.Stop();
            timer1.Stop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TestNetworkForTactile();
        }
    }
}
