using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.IO;

namespace MyLittleServer
{
    class dbConnector
    {
        DataTable dataBase;
        MySqlConnectionStringBuilder mysqlCSB;
        public static string data = null;
        public string[] rowsAsString;

        string[] mySqlConfig = new string[4];

        public string strNeedsString = @"SELECT 
                                        NAME,
                                        QUANTITY
                                        FROM   things
                                        WHERE  QUANTITY <= 0";

        public string strAvaliableString = @"SELECT 
                                            NAME,
                                            QUANTITY
                                            FROM   things
                                            WHERE  QUANTITY > 0";

        public dbConnector()
        {
            dataBase = new DataTable();
            mysqlCSB = new MySqlConnectionStringBuilder();
            mysqlCSB.Server = "";
            mysqlCSB.Port = 3306;
            mysqlCSB.Database = "";
            mysqlCSB.UserID = "";
            mysqlCSB.Password = "";
        }
        
        public bool checkBarcodeExisting(long barcode)
        {
            string strNameExisting = @"SELECT id, NAME, QUANTITY FROM things WHERE BARCODE= " + '"' + barcode + '"';
            string l = convertDataTableToString(GetComments(strNameExisting));
            if (l.Length > 7)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool checkNameExisting(string name)
        {
            string strNameExisting = @"SELECT id, NAME, QUANTITY FROM things WHERE NAME = " + '"' + name + '"';
            string l = convertDataTableToString(GetComments(strNameExisting));

            if (l.Length > 7)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void insertData(string name, int quantity, long barcode)
        {
            GetComments(createInsertRequestString(name, quantity, barcode));
        }

        public void updateData(string type, long barcode)
        {
            if (type == "INC")
            {
                GetComments(createUpdateIncrementRequestString(barcode));
            }
            else if (type == "DEC")
            {
                GetComments(createUpdateDecrementRequestString(barcode));
            }
        }

        public void updateData(string type, string name)
        {
            if (type == "INC")
            {
                GetComments(createUpdateIncrementRequestString(name));
            }
            else if (type == "DEC")
            {
                GetComments(createUpdateDecrementRequestString(name));
            }
        }

        private string createInsertRequestString(string NAME, int QUANTITY, long BARCODE)
        {
            string request = @"INSERT INTO `things` (`NAME`, `QUANTITY`, `BARCODE`) " +
                "VALUES " + "('" + NAME + "'" + ", '" + QUANTITY + "', '" + BARCODE + "')";
            return request;
        }

        private string createUpdateIncrementRequestString(long BARCODE)
        {
            string request = @"UPDATE things SET QUANTITY = QUANTITY + 1 WHERE BARCODE = " + '"' + BARCODE + '"';
            return request;
        }

        private string createUpdateIncrementRequestString(string NAME)
        {
            string request = @"UPDATE things SET QUANTITY = QUANTITY + 1 WHERE NAME = " + '"' + NAME + '"';
            return request;
        }

        //DECREMENT strings
        private string createUpdateDecrementRequestString(long BARCODE)
        {
            string request = @"UPDATE things SET QUANTITY = QUANTITY - 1 WHERE (BARCODE = " + '"' + BARCODE + '"' + ") AND (QUANTITY > 0)";
            return request;
        }

        private string createUpdateDecrementRequestString(string NAME)
        {
            string request = @"UPDATE things SET QUANTITY = QUANTITY - 1 WHERE (NAME = " + '"' + NAME + '"'+ ") AND (QUANTITY > 0)";
            return request;
        }

        public string getInformationFromBd(string requestt)
        {
            convertDataTableToStringArray(GetComments(requestt));
            return rowsAsString[1] + rowsAsString[2];
        }

        public DataTable GetComments(string request)
        {
            mySqlConfig= File.ReadAllLines("SQL.cfg");

            dataBase = new DataTable();
            mysqlCSB = new MySqlConnectionStringBuilder();
            mysqlCSB.Server = mySqlConfig[0];
            mysqlCSB.Port = 3306;
            mysqlCSB.Database = mySqlConfig[1];
            mysqlCSB.UserID = mySqlConfig[2];
            mysqlCSB.Password = mySqlConfig[3];
            
            using (MySqlConnection con = new MySqlConnection())
            {
                try
                {
                    con.ConnectionString = mysqlCSB.ConnectionString;
                    MySqlCommand com = new MySqlCommand(request, con);
                    con.Open();

                    using (MySqlDataReader dr = com.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            dataBase.Load(dr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return dataBase;
        }

        public void convertDataTableToStringArray(DataTable dataTable)
        {
            rowsAsString = new string[20];
            string data = string.Empty;

            DataRow dr = dataTable.Rows[0];
            for (int i = 0; i < dr.ItemArray.Length; i++)
            {
                rowsAsString[i] = dr[i].ToString();
                rowsAsString[i] += " ";
            }
        }
        

        public static string convertDataTableToString(DataTable dataTable)
        {
            string data = string.Empty;
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                DataRow row = dataTable.Rows[i];
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    data += dataTable.Columns[j].ColumnName + "~" + row[j];
                    if (j == dataTable.Columns.Count - 1)
                    {
                        if (i != (dataTable.Rows.Count - 1))
                            data += "$";
                    }
                    else
                        data += "|";
                }
            }
            return data;
        }

        private void dataTableAsStringStorage(DataTable datTab)
        {

        }
    }
}
