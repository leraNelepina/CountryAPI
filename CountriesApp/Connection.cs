using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CountriesApp
{
    class Connection
    {
        readonly string Conn = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\CountriesDB.mdf;Integrated Security=True";
        SqlConnection sqlConn = null;
        SqlCommandBuilder sqlBuil = null;
        SqlDataAdapter sqlDA = null;
        DataSet dataSet = null;
        DataCountry country = new DataCountry();
        dynamic json;

        public void CloseConnection()
        {
            if (sqlConn != null && sqlConn.State != ConnectionState.Closed)
                sqlConn.Close();
        }

        private void IsConnectionOpen()
        {
            if (sqlConn == null)
            {
                sqlConn = new SqlConnection(Conn);
                sqlConn.Open();
            }
        }

        public DataCountry GetDataAPI(string Name)
        {
            try
            {
                WebRequest request = WebRequest.Create("https://restcountries.eu/rest/v2/name/" + Name);

                request.Method = "GET";

                request.ContentType = "application/x-www-urlencoded";

                WebResponse response = request.GetResponse();

                using (Stream s = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(s))
                    {
                        json = System.Web.Helpers.Json.Decode(reader.ReadToEnd());
                    }
                }

                country.Name = json[0].name;
                country.CallingCodes = json[0].callingCodes[0];
                country.Capital = json[0].capital;
                country.Area = Convert.ToDouble(json[0].area);
                country.Population = json[0].population;
                country.Region = json[0].region;

                response.Close();

                return country;
            }
            catch (Exception ex)
            {
                if (ex.Message == "Удаленный сервер возвратил ошибку: (404) Не найден.")
                    MessageBox.Show("The country is not found \nTry again?", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public DataTable LoadData()
        {
            try
            {
                IsConnectionOpen();

                sqlDA = new SqlDataAdapter(@"SELECT C.Name, Code_country, Cities.Name AS Capital, Area, Population, R.Name AS Region FROM Countries C 
                                            JOIN Cities ON C.Capital = Cities.Id
                                            JOIN Regions R ON C.Region = R.Id", sqlConn);
                sqlBuil = new SqlCommandBuilder(sqlDA);
                dataSet = new DataSet();

                sqlDA.Fill(dataSet, "Countries");

                return dataSet.Tables["Countries"];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        //обновление данных всех стран в datagridview
        public DataTable ReloadData()
        {
            try
            {
                dataSet.Tables["Countries"].Clear();
                sqlDA.Fill(dataSet, "Countries");

                return dataSet.Tables["Countries"];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        //добавление данных(столицы, региона и страны) в бд
        public void InsertData()
        {
            int idCity, idRegion;
            DataRow row = null;

            try
            {
                IsConnectionOpen();

                idCity = SearchData("Cities", country.Capital);
                idRegion = SearchData("Regions", country.Region);

                sqlDA = new SqlDataAdapter("SELECT * FROM Countries WHERE Code_country = " + country.CallingCodes, sqlConn);
                sqlBuil = new SqlCommandBuilder(sqlDA);
                dataSet = new DataSet();

                sqlDA.Fill(dataSet, "Countries");

                if (dataSet.Tables["Countries"].Rows.Count == 0)
                    row = dataSet.Tables["Countries"].NewRow();
                else
                    row = dataSet.Tables["Countries"].Rows[0];

                row["Name"] = country.Name;
                row["Code_country"] = country.CallingCodes;
                if (idCity > 0) row["Capital"] = idCity;
                row["Area"] = country.Area;
                row["Population"] = country.Population;
                if (idRegion > 0) row["Region"] = idRegion;

                if (dataSet.Tables["Countries"].Rows.Count == 0) dataSet.Tables["Countries"].Rows.Add(row);

                sqlDA.Update(dataSet, "Countries");
                dataSet.AcceptChanges();
                if (row != null) MessageBox.Show("The database was updated successfully!", "Success", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //поиск столицы и региона в бд
        private int SearchData(string table, string name)
        {
            int id = 0;
            DataRow row = null;
            try
            {
                IsConnectionOpen();

                sqlDA = new SqlDataAdapter("SELECT * FROM " + table + " WHERE Name = '" + name + "'", sqlConn);
                sqlBuil = new SqlCommandBuilder(sqlDA);
                dataSet = new DataSet();

                sqlDA.Fill(dataSet, table);

                if (dataSet.Tables[table].Rows.Count == 0)
                {
                    sqlDA.InsertCommand = new SqlCommand("Create" + table, sqlConn);
                    sqlDA.InsertCommand.CommandType = CommandType.StoredProcedure;
                    sqlDA.InsertCommand.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 50, "Name"));

                    SqlParameter parameter = sqlDA.InsertCommand.Parameters.Add("@Id", SqlDbType.Int, 0, "Id");
                    parameter.Direction = ParameterDirection.Output;

                    row = dataSet.Tables[table].NewRow();
                    row["Name"] = name;
                    dataSet.Tables[table].Rows.Add(row);

                    sqlDA.Update(dataSet, table);
                    dataSet.AcceptChanges();

                    id = Convert.ToInt32(row["Id"]);
                }
                else
                {
                    row = dataSet.Tables[table].Rows[0];
                    id = Convert.ToInt32(row["Id"]);
                }

                return id;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return id;
            }
        }
    }
}
