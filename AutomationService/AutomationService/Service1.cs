using System;
using System.Configuration;
using System.ServiceProcess;
using System.Timers;

namespace AutomationService
{
    partial class Service1 : ServiceBase
    {
        private Timer _timer = new Timer();
        private readonly bool _prod = bool.Parse(ConfigurationManager.AppSettings["IsProduction"]);
        private int _lastHour = -1;

        public Service1()
        {
            InitializeComponent();
        }

        public void OnNewExecution()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            if (_prod)
            {
                System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "SendReports" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
                _timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
                _timer.Interval = Convert.ToDouble(ConfigurationManager.AppSettings["Timer"]);
                _timer.Enabled = true;
            }
            else
            {
                var info = TimeZoneInfo.FindSystemTimeZoneById("Central Brazilian Standard Time");
                var localServerTime = DateTimeOffset.Now;
                var localTime = TimeZoneInfo.ConvertTime(localServerTime, info);
                var from = "2018-04-01";
                var to = "2018-05-18";
                if (bool.Parse(ConfigurationManager.AppSettings["IsProduction"]))
                {
                    from = localTime.Date.ToShortDateString();
                    to = localTime.Date.ToShortDateString();
                }
                Program.SendInclutions(Convert.ToDateTime(@from), Convert.ToDateTime(to));
            }
        }

        protected override void OnStop()
        {
            System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "SendReportsServiceStop" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
            _timer.Enabled = false;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            var info = TimeZoneInfo.FindSystemTimeZoneById("Central Brazilian Standard Time");
            var localServerTime = DateTimeOffset.Now;
            var localTime = TimeZoneInfo.ConvertTime(localServerTime, info);
            if (_lastHour == localTime.Hour) return;
            var start = new TimeSpan(22, 0, 0);
            var end = new TimeSpan(22, 59, 59);
            var now = new TimeSpan(localTime.Hour, localTime.Minute, localTime.Second);
            if (now <= start || now >= end) return;
            _lastHour = localTime.Hour;
            var from = "2018-04-01";
            var to = "2018-05-03";
            if (_prod)
            {
                @from = localTime.Date.ToShortDateString();
                to = localTime.Date.ToShortDateString();
            }
            Program.SendInclutions(Convert.ToDateTime(@from), Convert.ToDateTime(to));
        }
    }
}

