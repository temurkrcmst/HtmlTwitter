using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Oracle.ManagedDataAccess.Client;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Contexts;
using System.Collections;

namespace HtmlService
{
    public partial class Service1 : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };
        public Service1()
        {
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();//Eventlog oluşturup isimlendirildi.
            if (!System.Diagnostics.EventLog.SourceExists("TwiterService"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "TwiterService", "TwiterService");
            }
            eventLog1.Source = "TwiterService";
            eventLog1.Log = "TwiterService";


            
        }


        static string connection =ConfigurationManager.AppSettings["ConnectionString"].ToString();
        OracleConnection conn = new OracleConnection(connection);
        //Veri Tabanı bağlantısı
        public string html;
        public Uri url;

        public class Row
        {
            public string title { get; set; }
            public DateTime pubdate { get; set; }
            public string guid { get; set; }

            public DateTime dbhour { get; set; }


        }
        private bool kontrol(string guid)//aynı id(guid)'den oluşan veri varsa veri tabanına eklemesini engellendiği sorgu.
        {
            conn.Open();
            OracleCommand select = new OracleCommand("select * from HTMLTABLE Where GUID = :guid", conn);
            select.Parameters.Add(":guid", guid);

            object read = select.ExecuteScalar();
            conn.Close();

            return (read is null);
        }

        public void VeriAl(string Url)
        {
            url = new Uri(Url);
            WebClient client = new WebClient();// webclient nesnesini kullanıyoruz bağlanmak için.
            client.Encoding = Encoding.UTF8;//türkçe karakter sorunu yapmaması için encoding utf8 yapıyoruz.
            html = client.DownloadString(url);// siteye bağlanıp tüm sayfanın html içeriğini çekiyoruz.
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();//kütüphanemizi kullanıp htmldocument oluşturuyoruz.
            doc.LoadHtml(html);//documunt değişkeninin html ine çektiğimiz htmli veriyoruz.
            int count = Regex.Matches(html, "<li class=\"js-stream-item stream-item stream-item").Count;


            

            for (int i = 1; i <= count; i++)
            {
   
                var date = Convert.ToDateTime(doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("title", "").ToString().Replace("-", "")).AddHours(10.0);
               //Html Sayfasındaki yolununun belirtildiği kısım.
                //Tarih kısmının çekildiği Xpath yolu ve çekilirken tarihle zaman arasındaki "-" işareti sorun çıkardığı için boşluk olarak yazılmasını sağladığımız yer.
                //Saat kısmı Türkiye yerel saatine göre ayarlandı.
                var twt = doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[2]/p/text()").InnerText.ToString();//Twittin içeriği "item" kısmı
                var id = doc.DocumentNode.SelectSingleNode("//li[" + i.ToString() + "]/div[1]/div[2]/div[1]/small/a").GetAttributeValue("data-conversation-id", "").ToString();
                //Twittin kendine özgü id(guid)'sinin çekildiği kısım
                var dbhour = DateTime.Now;//Veri Tabanına her çekilen verinin ne zaman kayıt olduğunu göstermek için kayıt edildiği tarihi ve saatin yazıldığı kısım.


                if (kontrol(id))//Veri Tabanında Kayıtlı olan aynı id(guid)'li veri varsa yazılmamasının engellendiği sorgu
                {

                    OracleCommand ekle = new OracleCommand("INSERT INTO HTMLTABLE(GUID,TITLE,PUBDATE,DBHOUR)" + " VALUES(:guid,:title,:pubdate,:dbhour)", conn);
                    //Oracle tarafında oluşturulan sutunların içerisine ekleme sorgusu.
                    ekle.Parameters.Add(":guid", id);
                    ekle.Parameters.Add(":title", twt);
                    ekle.Parameters.Add(":pubdate", date);
                    ekle.Parameters.Add(":dbour", dbhour);

                    conn.Open();
                    ekle.ExecuteNonQuery();
                    conn.Close();
                }


            }


        }


        protected override void OnStart(string[] args)
        {

           

            eventLog1.WriteEntry("In OnStart.");
            Timer timer = new Timer();
            timer.Interval = 30000; // 30 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();


            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }
        private int eventId = 1;
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            
            // TODO: Insert monitoring activities here.
            eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);
            var value = ConfigurationManager.AppSettings["HtmlUrl"];//App.config tarafına gömülü olan url'yi tanımlama kısmı.

            VeriAl(value);

        }
        protected override void OnStop()
        {
            eventLog1.WriteEntry("In OnStop.");

            

        }
        protected override void OnContinue()
        {
            eventLog1.WriteEntry("In OnContinue.");
        }
        protected override void OnPause()
        {
            eventLog1.WriteEntry("In OnPause.");

        }
        protected override void OnShutdown()
        {
            eventLog1.WriteEntry("In OnShutdown.");

           
        }

        private void EventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

        }
    }
}
