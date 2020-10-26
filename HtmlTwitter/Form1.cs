using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;


namespace HtmlTwitter
{
    public partial class Form1 : Form
    {
        public string html;
        public Uri url;
        
        public  class Row
        { 
            public string title { get; set; }
            public DateTime pubdate { get; set; }
            public string guid { get; set; }

            public DateTime dbhour { get; set; }
            

        }

        static string connection = ConfigurationManager.AppSettings["ConnectionString"].ToString();
        OracleConnection conn = new OracleConnection(connection);
      //  OracleConnection conn = new OracleConnection("Data Source=localhost:1521/orcl;Persist Security Info=True;User ID=RSS;Password=Krcmst11079");

        public Form1()
        {
            InitializeComponent();
        }

        public void VeriAl(string Url)
        {
            url = new Uri(Url);
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            html = client.DownloadString(url);
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            int count = Regex.Matches(html, "<li class=\"js-stream-item stream-item stream-item").Count;


            listBox1.Items.Clear();
            listBox2.Items.Clear();
            listBox3.Items.Clear();

            for (int i = 1; i <= count; i++)
            {

                //CikanSonuc.Items.Add(doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("title", "").ToString());
                //listBox2.Items.Add(doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[2]/p/text()").InnerText.ToString());
                //listBox3.Items.Add(doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("data-conversation-id", "").ToString());

                // var date = doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("title", "").ToString();
                var date=Convert.ToDateTime(doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("title", "").ToString().Replace("-", "")).AddHours(10.0);

                var twt = doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[2]/p/text()").InnerText.ToString();
                var id =  doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("data-conversation-id", "").ToString();
                var dbhour = DateTime.Now;



                if (kontrol(id))
                {


                    //DateTime DateT = DateTime.Parse(row.pubdate, System.Globalization.CultureInfo.InvariantCulture);
                    //DateTime DateT = DateTime.ParseExact(row.pubdate, "yyyy-AA-aa HH: mm tt", null);

                    OracleCommand ekle = new OracleCommand("INSERT INTO HTMLTABLE(GUID,TITLE,PUBDATE,DBHOUR)" + " VALUES(:guid,:title,:pubdate,:dbhour)", conn);
                    ekle.Parameters.Add(":guid", id);
                    ekle.Parameters.Add(":title", twt);
                    ekle.Parameters.Add(":pubdate", date);
                    ekle.Parameters.Add(":dbour", dbhour);

                    conn.Open();

                    ekle.ExecuteNonQuery();
                    conn.Close();
                }


            }




            //List<Row> rowList = new List<Row>();
            //for (int i = 0; i < count; i++)
            //{
            //    Row package = new Row();
                
            //    //package.guid = doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("title", "").ToString();
            //    //package.guid =
            //    package.title = listBox2.Items[i].ToString();
            //    package.pubdate = Convert.ToDateTime(listBox1.Items[i].ToString().Replace("-", "")).AddHours(10.0);
            //    package.dbhour = DateTime.Now;

            //    rowList.Add(package);


              


            //}
            //conn.Close();
             

            //foreach(Row row in rowList )
            //{
                
            //    listBox1.Items.Add(row.pubdate);
            //    listBox2.Items.Add(row.title);
            //    listBox3.Items.Add(row.guid);


            //    if (kontrol(row.guid))
            //    {
                    

            //        //DateTime DateT = DateTime.Parse(row.pubdate, System.Globalization.CultureInfo.InvariantCulture);
            //        //DateTime DateT = DateTime.ParseExact(row.pubdate, "yyyy-AA-aa HH: mm tt", null);

            //        OracleCommand ekle = new OracleCommand("INSERT INTO HTMLTABLE(GUID,TITLE,PUBDATE,DBHOUR)" + " VALUES(:guid,:title,:pubdate,:dbhour)", conn);
            //        ekle.Parameters.Add(":guid", row.guid);
            //        ekle.Parameters.Add(":title", row.title);
            //        ekle.Parameters.Add(":pubdate", row.pubdate);
            //        ekle.Parameters.Add(":dbour", row.dbhour);

            //        conn.Open();

            //        ekle.ExecuteNonQuery();
            //        conn.Close();
            //    }

            //}


          

        }

        private bool kontrol(string guid)
        {
            conn.Open();
            OracleCommand select = new OracleCommand("select * from HTMLTABLE Where GUID = :guid", conn);
            select.Parameters.Add(":guid", guid);

            object read = select.ExecuteScalar();
            conn.Close();

            return (read is null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
            var value = ConfigurationManager.AppSettings["HtmlUrl"];

            VeriAl(value);

           

        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
        }
    }
    


}
