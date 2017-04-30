using System;
using System.Windows.Forms;
using HtmlAgilityPack;
using S22.Xmpp.Client;
using S22.Xmpp.Im;

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
        XmppClient client = new XmppClient(hostname, username, password);

        string workMode;

        public Form1()
        {
            InitializeComponent();

            client.Message += OnNewMessage;
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
            try
            {
                client.Connect();
            }
            catch
            {
                MessageBox.Show("Не удалось соединиться с сервером, проверьте логин/пароль!",
                                "Ошибка",
                                MessageBoxButtons.OK);
            }
        }
    }
}
