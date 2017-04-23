using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net.Sockets;

using AForge.Video.DirectShow;

using MessagingToolkit.Barcode;
using HtmlAgilityPack;

using ConvNetSharp;
using ConvNetSharp.Training;
using System.IO;
using ConvNetSharp.Serialization;

namespace roadTrack
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        double[] sensorSample = new double[320*240];

        private int trainingBatchSize;

        private Net net;
        private AdadeltaTrainer trainer;

        private List<Entry> training;
        private List<Entry> testing;

        string[] names;

        // Ширина изображения
        int inputWidth = 240;
        // Высота изображения
        int inputHeight = 320;
        // Число каналов у изображения
        int inputDepth = 1;

        bool lockedInc = false;
        bool lockedDec = false;

        int lockedIncCounter = 0;
        int lockedDecCounter = 0;

        string[] namesArray = new string[0];
        int[] countArray = new int[0];
        long[] idArray = new long[0];

        BarcodeDecoder barcodeDecoderInc = new BarcodeDecoder();
        BarcodeDecoder barcodeDecoderDec = new BarcodeDecoder();

        int framesNumInc = 0;
        int framesNumDec = 0;

        Bitmap workImage,
               myImage;

        private const int statLength = 15;

        private int statIndexInc = 0;
        private int statIndexDec = 0;

        private int statReadyInc = 0;
        private int statReadyDec = 0;

        private int[] statCountInc = new int[statLength];
        private int[] statCountDec = new int[statLength];

        private void button1_Click(object sender,
                                   EventArgs e)
        {
            //Подключаем видео
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var form = new VideoCaptureDeviceForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                videoSourcePlayer1.VideoSource = form.VideoDevice;
            }

            videoSourcePlayer1.Start();
            timer1.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Подключаем видео
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var form = new VideoCaptureDeviceForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                videoSourcePlayer2.VideoSource = form.VideoDevice;
            }

            videoSourcePlayer2.Start();
            timer2.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PrepareData();

            net = null;
            var json_temp = File.ReadAllLines("NetworkStructure.json");
            string json = string.Join("", json_temp);
            net = SerializationExtensions.FromJSON(json);
        }

        private void Form1_FormClosed(object sender,
                                      FormClosedEventArgs e)
        {
            videoSourcePlayer1.Stop();
            timer1.Stop();

            videoSourcePlayer2.Stop();
            timer2.Stop();
        }

        private void videoSourcePlayer1_NewFrame(object sender,
                                                 ref Bitmap inputImage)
        {
            if (framesNumInc > 1)
            {
                //Загружаем
                Bitmap workImage = (Bitmap)inputImage.Clone();
                myImage = workImage;

                DecodeBarcodeInc(workImage);
            }

            framesNumInc++;
        }

        private void videoSourcePlayer2_NewFrame(object sender,
                                                 ref Bitmap inputImage)
        {
            if (framesNumDec > 1)
            {
                //Загружаем
                workImage = (Bitmap)inputImage.Clone();

                DecodeBarcodeDec(workImage);
            }

            framesNumDec++;
        }

        private void DecodeBarcodeInc(Bitmap image)
        {
            Dictionary<DecodeOptions, object> decodingOptions = new Dictionary<DecodeOptions, object>();
            List<BarcodeFormat> possibleFormats = new List<BarcodeFormat>(1);

            possibleFormats.Add(BarcodeFormat.EAN13);
            decodingOptions.Add(DecodeOptions.PossibleFormats, possibleFormats);
            decodingOptions.Add(DecodeOptions.PureBarcode, "");
            decodingOptions.Add(DecodeOptions.AutoRotate, true);

            try
            {
                Result decodedResult = barcodeDecoderInc.Decode(image, decodingOptions);

                if (decodedResult != null)
                {
                    if (lockedInc == false)
                    {
                        Connect(decodedResult.Text, "INC");
                    }
                }
            }
            catch (NotFoundException)
            {
                if (lockedInc == true)
                {
                    if (lockedIncCounter < 60)
                    {
                        lockedIncCounter++;
                    }
                    else
                    {
                        lockedInc = false;
                        lockedIncCounter = 0;
                    }
                }
            }
        }

        private void DecodeBarcodeDec(Bitmap image)
        {
            Dictionary<DecodeOptions, object> decodingOptions = new Dictionary<DecodeOptions, object>();
            List<BarcodeFormat> possibleFormats = new List<BarcodeFormat>(1);

            possibleFormats.Add(BarcodeFormat.EAN13);
            decodingOptions.Add(DecodeOptions.PossibleFormats, possibleFormats);
            decodingOptions.Add(DecodeOptions.PureBarcode, "");
            decodingOptions.Add(DecodeOptions.AutoRotate, true);

            try
            {
                Result decodedResult = barcodeDecoderDec.Decode(image, decodingOptions);

                if (decodedResult != null)
                {
                    if (lockedDec == false)
                    {
                        Connect(decodedResult.Text, "DEC");
                    }
                }
            }
            catch (NotFoundException)
            {
                if (lockedDec == true)
                {
                    if (lockedDecCounter < 60)
                    {
                        lockedDecCounter++;
                    }
                    else
                    {
                        lockedDec = false;
                        lockedDecCounter = 0;
                    }
                }
            }
        }

        void Connect(string message, string mode)
        {
            bool idExist = false;

            if (mode == "INC")
            {
                #region
                for (int w = 0; w < idArray.Length; w++)
                {
                    if (Convert.ToInt64(message) == idArray[w])
                    {
                        idExist = true;
                        countArray[w]++;

                        dataGridView1.Invoke((MethodInvoker)delegate
                        {
                            dataGridView1.Rows.Clear();

                            for (int i = 0; i < namesArray.GetLength(0); i++)
                            {
                                dataGridView1.Rows.Add();
                                dataGridView1[0, i].Value = namesArray[i];
                            }

                            for (int i = 0; i < countArray.GetLength(0); i++)
                            {
                                dataGridView1[1, i].Value = countArray[i];
                            }

                            for (int i = 0; i < idArray.GetLength(0); i++)
                            {
                                dataGridView1[2, i].Value = idArray[i];
                            }
                        });
                    }
                }
                #endregion
            }
            else if (mode == "DEC")
            {
                #region
                for (int w = 0; w < idArray.Length; w++)
                {
                    if (Convert.ToInt64(message) == idArray[w])
                    {
                        idExist = true;
                        if (countArray[w] > 0)
                        {
                            countArray[w]--;
                        }

                        dataGridView1.Invoke((MethodInvoker)delegate
                        {
                            dataGridView1.Rows.Clear();

                            for (int i = 0; i < namesArray.GetLength(0); i++)
                            {
                                dataGridView1.Rows.Add();
                                dataGridView1[0, i].Value = namesArray[i];
                            }

                            for (int i = 0; i < countArray.GetLength(0); i++)
                            {
                                dataGridView1[1, i].Value = countArray[i];
                            }

                            for (int i = 0; i < idArray.GetLength(0); i++)
                            {
                                dataGridView1[2, i].Value = idArray[i];
                            }
                        });
                    }
                }
                #endregion
            }

            if (idExist == true)
            {
                lockedInc = true;
            }
            else
            {
                HtmlWeb hw = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = hw.Load(@"https://barcodes.olegon.ru/" + message);
                var nodes = doc.DocumentNode.SelectNodes("//div[@id='names']");

                string responseData = "";

                foreach (HtmlNode node in nodes)
                {
                    responseData = node.InnerText;
                }

                responseData = responseData.Remove(0, 22);

                if (responseData == "")
                {
                    responseData = "tsp";

                    if (responseData == "tsp")
                    {
                        Namer f = new Namer();
                        f.ShowDialog();

                        responseData = f.newName;
                    }
                }

                bool nameExist = false;

                if (responseData != null)
                {
                    if (mode == "INC")
                    {
                        #region
                        lockedInc = true;

                        for (int a = 0; a < namesArray.GetLength(0); a++)
                        {
                            if (namesArray[a] == responseData)
                            {
                                nameExist = true;
                                countArray[a]++;

                                dataGridView1.Invoke((MethodInvoker)delegate
                                {
                                    dataGridView1.Rows.Clear();

                                    for (int i = 0; i < namesArray.GetLength(0); i++)
                                    {
                                        dataGridView1.Rows.Add();
                                        dataGridView1[0, i].Value = namesArray[i];
                                    }

                                    for (int i = 0; i < countArray.GetLength(0); i++)
                                    {
                                        dataGridView1[1, i].Value = countArray[i];
                                    }

                                    for (int i = 0; i < idArray.GetLength(0); i++)
                                    {
                                        dataGridView1[2, i].Value = idArray[i];
                                    }
                                });
                            }
                        }

                        if (nameExist == false)
                        {
                            Array.Resize(ref idArray, idArray.Length + 1);
                            idArray[idArray.Length - 1] = Convert.ToInt64(message);

                            Array.Resize(ref namesArray, namesArray.Length + 1);
                            namesArray[namesArray.Length - 1] = responseData;

                            Array.Resize(ref countArray, countArray.Length + 1);
                            countArray[countArray.Length - 1] = 1;

                            dataGridView1.Invoke((MethodInvoker)delegate
                            {
                                dataGridView1.Rows.Clear();

                                for (int i = 0; i < namesArray.GetLength(0); i++)
                                {
                                    dataGridView1.Rows.Add();
                                    dataGridView1[0, i].Value = namesArray[i];
                                }

                                for (int i = 0; i < countArray.GetLength(0); i++)
                                {
                                    dataGridView1[1, i].Value = countArray[i];
                                }

                                for (int i = 0; i < idArray.GetLength(0); i++)
                                {
                                    dataGridView1[2, i].Value = idArray[i];
                                }
                            });
                        }
                        #endregion
                    }
                    else if (mode == "DEC")
                    {
                        #region
                        lockedDec = true;

                        for (int a = 0; a < namesArray.GetLength(0); a++)
                        {
                            if (namesArray[a] == responseData)
                            {
                                nameExist = true;
                                if (countArray[a] > 0)
                                {
                                    countArray[a]--;
                                }

                                dataGridView1.Invoke((MethodInvoker)delegate
                                {
                                    dataGridView1.Rows.Clear();

                                    for (int i = 0; i < namesArray.GetLength(0); i++)
                                    {
                                        dataGridView1.Rows.Add();
                                        dataGridView1[0, i].Value = namesArray[i];
                                    }

                                    for (int i = 0; i < countArray.GetLength(0); i++)
                                    {
                                        dataGridView1[1, i].Value = countArray[i];
                                    }

                                    for (int i = 0; i < idArray.GetLength(0); i++)
                                    {
                                        dataGridView1[2, i].Value = idArray[i];
                                    }
                                });
                            }
                        }
                        #endregion
                    }
                }
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        private void timer1_Tick(object sender,
                                 EventArgs e)
        {
            if (videoSourcePlayer1.VideoSource != null)
            {
                statCountInc[statIndexInc] = videoSourcePlayer1.VideoSource.FramesReceived;

                if (++statIndexInc >= statLength)
                {
                    statIndexInc = 0;
                }

                if (statReadyInc < statLength)
                {
                    statReadyInc++;
                }

                float fps = 0;

                for (int i = 0; i < statReadyInc; i++)
                {
                    fps += statCountInc[i];
                }

                fps /= statReadyInc;

                label1.Text = "FPS: " + fps.ToString();
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (videoSourcePlayer2.VideoSource != null)
            {
                statCountDec[statIndexDec] = videoSourcePlayer2.VideoSource.FramesReceived;

                if (++statIndexDec >= statLength)
                {
                    statIndexDec = 0;
                }

                if (statReadyDec < statLength)
                {
                    statReadyDec++;
                }

                float fps = 0;

                for (int i = 0; i < statReadyInc; i++)
                {
                    fps += statCountDec[i];
                }

                fps /= statReadyDec;

                label2.Text = "FPS: " + fps.ToString();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            net = null;

            CreateNetworkForTactile();
            TrainNetworkForTactile(0.01);

            //testingToolStripMenuItem.Enabled = true;
            //saveToolStripMenuItem.Enabled = true;

            MessageBox.Show("Обучение завершено!",
                            "Готово",
                            MessageBoxButtons.OK);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //ResultSchunkPicBox.Image = null;
            //ResultsSchunkTxtBox.Text = null;

            try
            {
                //ReceiveTCP("sensor", "127.0.0.1", 4446);

                TestNetworkForTactile();
            }
            catch (SocketException)
            {
                //ResultsSchunkTxtBox.Text = "Ошибка приёма данных!";
            }
        }
    }
}
