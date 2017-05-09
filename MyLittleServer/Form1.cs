using System;
using System.Windows.Forms;
using HtmlAgilityPack;
using S22.Xmpp.Client;
using S22.Xmpp.Im;
using System.IO;
using ConvNetSharp.Serialization;

namespace MyLittleServer
{
    public partial class Form1 : Form
    {
        string[] namesArray = new string[0];
        int[] countArray = new int[0];
        long[] idArray = new long[0];

        static string hostname = "jabber.ru";
        static string username = "";
        static string password = "";
<<<<<<< HEAD
        dbConnector dataBaseConn;
=======
>>>>>>> origin/master
        XmppClient client = new XmppClient(hostname, username, password);

        string workMode;

        public Form1()
        {
            InitializeComponent();
            
            client.Message += OnNewMessage;
        }

        private void Connect(string message)
        {
            // Проверяем нет ли вещи с таким штрихом-кодом в базе            
            if (dataBaseConn.checkBarcodeExisting(Convert.ToInt64(message)))
            { 
                dataBaseConn.updateData(workMode, Convert.ToInt64(message));
                logTextBox_textChange("Barcode есть, изменил количество");
            }
            
            else if (!dataBaseConn.checkBarcodeExisting(Convert.ToInt64(message)))
            {
                logTextBox_textChange("Barcode нет, парсю имя");
                HtmlWeb hw = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = hw.Load(@"https://barcodes.olegon.ru/" + message);
                var nodes = doc.DocumentNode.SelectNodes("//div[@id='names']");

                string responseData = "";

                foreach (HtmlNode node in nodes)
                {
                    responseData = node.InnerText;
                }

                responseData = responseData.Remove(0, 22);
                responseData = responseData.Replace('"', ' ');
                responseData = responseData.Replace('\'', ' ');
                responseData = responseData.Replace('{', ' ');
                responseData = responseData.Replace('}', ' ');

                // Если в базе сайта нет названия - даем предмету характерное название
                if (responseData == "")
                {
                    Namer f = new Namer();
                    f.ShowDialog();

                    responseData = f.newName;
                }

                if (responseData != null)
                {
                    // Проверяем нет ли вещи с таким именем в базе
                    if (workMode == "INC")
                    {
                        if(!dataBaseConn.checkNameExisting(responseData))
                        {
                            logTextBox_textChange("Имени нет, вставляю " + responseData);
                            dataBaseConn.insertData(responseData, 1, Convert.ToInt64(message));
                        }
                    }
                }
            }

            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            dataGridView1.Invoke((MethodInvoker)delegate
            {
                dataGridView1.DataSource = null;
                dataGridView1.Rows.Clear();
                dataGridView1.DataSource = dataBaseConn.GetComments("SELECT * FROM things ORDER BY `id` ASC");
            });
        }

        private void OnNewMessage(object sender, MessageEventArgs e)
        {
            string data = e.Message.Body;
            
            string[] substrings = data.Split(',');

            workMode = substrings[1];

            Connect(substrings[0]);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (client != null)
            {
                client.Close();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            net = null;
            try
            {
                dataBaseConn = new dbConnector();
            }
            catch (Exception)
            {
                MessageBox.Show("Не найдена база данных! Проверьте запущен ли сервер БД",
                               "Нет подключения к БД",
                               MessageBoxButtons.OK);
            }

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

            RefreshDataGrid();
        }

        private void logTextBox_textChange(string text)
        {
            log_textBox.Invoke((MethodInvoker)delegate
            {
                log_textBox.Text = text;
            });
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

        private void button4_Click(object sender, EventArgs e)
        {
            TestNetworkForTactile();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }
    }
}
