using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

using AForge.Video.DirectShow;

using MessagingToolkit.Barcode;

using HtmlAgilityPack;

using ConvNetSharp.Serialization;

namespace roadTrack
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        bool locked = false;

        int lockedCounter = 0;

        string[] namesArray = new string[0];
        int[] countArray = new int[0];
        long[] idArray = new long[0];

        int framesNum = 0;

        string workMode = "INC";

        Bitmap myImage;

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
                Result decodedResult = barcodeDecoder.Decode(image, decodingOptions);

                if (decodedResult != null)
                {
                    if (locked == false)
                    {
                        Connect(decodedResult.Text);
                    }
                }
            }
            catch (NotFoundException)
            {
                if (locked == true)
                {
                    if (lockedCounter < 60)
                    {
                        lockedCounter++;
                    }
                    else
                    {
                        locked = false;
                        lockedCounter = 0;
                    }
                }
            }
        }

        private void RefreshDataGrid()
        {
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

        private void Connect(string message)
        {
            bool idExist = false;

            // Проверяем нет ли вещи с таким штрихом-кодом в базе
            for (int w = 0; w < idArray.Length; w++)
            {
                if (Convert.ToInt64(message) == idArray[w])
                {
                    idExist = true;

                    if (workMode == "INC")
                    {
                        countArray[w]++;
                    }
                    else if (workMode == "DEC")
                    {
                        if (countArray[w] > 0)
                        {
                            countArray[w]--;
                        }
                    }

                    RefreshDataGrid();
                }
            }


            if (idExist == true)
            {
                locked = true;
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

                bool nameExist = false;

                // Если в базе сайта нет названия - даем предмету характерное название
                if (responseData == "")
                {
                    Namer f = new Namer();
                    f.ShowDialog();

                    responseData = f.newName;
                }

                if (responseData != null)
                {
                    locked = true;

                    // Проверяем нет ли вещи с таким именем в базе
                    for (int a = 0; a < namesArray.GetLength(0); a++)
                    {
                        if (namesArray[a] == responseData)
                        {
                            nameExist = true;

                            if (workMode == "INC")
                            {
                                countArray[a]++;
                            }
                            else if (workMode == "DEC")
                            {
                                if (countArray[a] > 0)
                                {
                                    countArray[a]--;
                                }
                            }

                            RefreshDataGrid();
                        }
                    }

                    if (workMode == "INC")
                    {
                        if (nameExist == false)
                        {
                            Array.Resize(ref idArray, idArray.Length + 1);
                            idArray[idArray.Length - 1] = Convert.ToInt64(message);

                            Array.Resize(ref namesArray, namesArray.Length + 1);
                            namesArray[namesArray.Length - 1] = responseData;

                            Array.Resize(ref countArray, countArray.Length + 1);
                            countArray[countArray.Length - 1] = 1;

                            RefreshDataGrid();
                        }
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
