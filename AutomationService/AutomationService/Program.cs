using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AutomationService
{
    static class Program
    {
        private static readonly BOPEntities Db = new BOPEntities();
        static void Main()
        {
#if DEBUG
            var svc = new Service1();
            svc.OnNewExecution();
            Thread.Sleep(Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }

        public static void SendInclutions(DateTime from, DateTime to)
        {
            foreach (var app in GetApps())
            {
                var query = "exec sp_info_usuarios_intervalo '" + app.AppCode + "', '" + from.GetFormatedDate() + "', '" + to.GetFormatedDate() + "'";
                var results = Db.Database.SqlQuery<ResultsUsers>(query).ToList();
                results = results.FormatResults();
                if (results.Count <= 0) continue;
                ProcessTem(results);
                ProcessFuncional(results);
                ProcessEpharma(results);
            }
        }

        private static IEnumerable<Apps> GetApps()
        {
            var query = "select id as Id, AppCode, Name as 'Name' from AppContract";
            return Db.Database.SqlQuery<Apps>(query).ToList();
        }

        private static string GetFormatedDate(this DateTime date)
        {
            return string.Concat(date.Year, date.Month.ToString("00"), date.Day.ToString("00"));
        }

        private static string GetFormatedDate(this string date)
        {
            DateTime dt;
            var parse = DateTime.TryParse(date, out dt);
            if (!parse)
            {
                var arr = date.ToCharArray();
                var newDate = string.Concat(arr[0], arr[1], arr[2], arr[3], '-', arr[4], arr[5], '-', arr[6], arr[7]);
                dt = Convert.ToDateTime(newDate);
                return string.Concat(dt.Day.ToString("00"), dt.Month.ToString("00"), dt.Year);
            }
            dt = Convert.ToDateTime(date);
            return string.Concat(dt.Day.ToString("00"), dt.Month.ToString("00"), dt.Year);
        }

        private static void ProcessTem(List<ResultsUsers> users)
        {
            var file = "";
            foreach (var user in users)
            {
                var line = "1;";
                line += user.Tarjeta + ";";
                line += "".GenerateEmptyFields(1, 1);
                line += "5001;";
                line += user.CPF + ";";
                line += "".GenerateEmptyFields(1, 1);
                line += user.Nombre_Usuario + ";";
                line += "".GenerateEmptyFields(7, 1);
                line += user.Celular + ";";
                line += user.Correo + ";";
                line += "1;";
                line += "".GenerateEmptyFields(1, 1);
                line += user.Fecha_nacimiento.GetFormatedDate() + ";";
                line += "M;";
                line += "".GenerateEmptyFields(6, 1);
                line += user.fecha_ini.GetFormatedDate() + ";";
                line += "".GenerateEmptyFields(20, 1);
                file += line + "\r\n";
            }
            SendToFtp(file);
        }

        private static void ProcessFuncional(List<ResultsUsers> users)
        {
            var file = "";
            //Create Header
            var line = "AA";
            line += "".GenerateEmptyFields(7);
            line += DateTime.Now.ToShortDateString().GetFormatedDate();
            line += "".GenerateEmptyFields(266);
            line += "29178941000100";
            line += "00001";
            line += "".GenerateEmptyFields(1);
            line += 'H';
            file += line + "\r\n";
            var counter = 0;
            //create body
            foreach (var user in users)
            {
                line = "";
                line += "B1";
                line += "000";
                line += "00000";
                line += "0000000000";
                line += user.CPF.GetFullCpf();
                line += user.Tarjeta.GenerateEmptyFields(16);
                line += "".GenerateEmptyFields(3);
                line += user.Nombre_Usuario.GenerateEmptyFields(58);
                line += "Av. João Cabral de Melo Neto, 850 Bloco 2 Sala 1706".GenerateEmptyFields(40);
                line += "Barra da Tijuca".GenerateEmptyFields(30);
                line += "Rio de Janeiro".GenerateEmptyFields(30);
                line += "RJ";
                line += "22775057";
                line += user.Celular.GenerateEmptyFields(11);
                line += "00";
                line += "00";
                line += "".GenerateEmptyFields(9, 2);
                line += "000000001";
                line += string.IsNullOrEmpty(user.Sexo) ? "M" : user.Sexo.GenerateEmptyFields(1);
                line += user.Fecha_nacimiento.GetFormatedDate();
                line += user.Id_Usuario.GenerateEmptyFields(30);
                line += "00";
                line += user.fecha_ini.GetFormatedDate();
                line += "".GenerateEmptyFields(12);
                line += 'N';
                line += (counter + 1).ToString().GenerateEmptyFields(5, 3);
                line += "".GenerateEmptyFields(1);
                line += 'I';
                file += line + "\r\n";
                counter++;
            }
            //create footer
            line = "";
            line += "ZZ";
            line += "".GenerateEmptyFields(7);
            line += DateTime.Now.ToShortDateString().GetFormatedDate();
            line += counter.ToString().GenerateEmptyFields(5, 3);
            line += "".GenerateEmptyFields(273);
            line += "00001";
            line += "".GenerateEmptyFields(1);
            line += 'T';
            file += line;
            CreateFile(file);
        }

        private static void ProcessEpharma(List<ResultsUsers> users)
        {
            var file = "";
        }

        private static string GenerateEmptyFields(this string s, int q, int t = 0)
        {
            s = string.IsNullOrEmpty(s) ? "" : s;
            var ln = s.Length;
            if (ln > q) return string.Join("", s.ToCharArray().Take(q));
            switch (t)
            {
                case 0:
                    for (var i = 0; i < q - ln; i++)
                    {
                        s += ' ';
                    }
                    break;
                case 1:
                    for (var i = 0; i < q - ln; i++)
                    {
                        s += ';';
                    }
                    break;
                case 2:
                    for (var i = 0; i < q - ln; i++)
                    {
                        s += '0';
                    }
                    break;
                case 3:
                    for (var i = 0; i < q - ln; i++)
                    {
                        s += '0';
                    }
                    var arr = s.ToCharArray();
                    s = "";
                    for (var i = arr.Length - 1; i >= 0; i--)
                    {
                        s += arr[i];
                    }
                    break;
            }

            return s;
        }

        private static void SendToFtp(string file)
        {

        }

        private static void CreateFile(string file)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory + "FuncionalArquivo" + DateTime.Now.ToString("ddMMyyyy") + ".txt";
            File.WriteAllText(path, file);
        }

        private static string GetFullCpf(this string cpf)
        {
            var ln = cpf.Length;
            if (ln == 11) return cpf;
            if (ln > 11) return string.Join("", cpf.ToCharArray().Take(11));
            var rtn = "";
            for (var i = 0; i < 11 - ln; i++)
            {
                rtn += '0';
            }
            return string.Concat(rtn, cpf);
        }

        private static List<ResultsUsers> FormatResults(this List<ResultsUsers> lst)
        {
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            foreach (var user in lst)
            {
                user.CPF = rgx.Replace(user.CPF, "");
                user.Celular = rgx.Replace(user.Celular, "");
                user.Fecha_nacimiento = rgx.Replace(user.Fecha_nacimiento, "");
                user.Nombre_Usuario = rgx.Replace(user.Nombre_Usuario, "");
                user.Tarjeta = rgx.Replace(user.Tarjeta, "");
            }
            return lst;
        }
    }
}

public class ResultsUsers
{
    public string Nombre_Usuario { get; set; }
    public string CPF { get; set; }
    public string Correo { get; set; }
    public string Fecha_nacimiento { get; set; }
    public string Sexo { get; set; }
    public string Celular { get; set; }
    public string Tarjeta { get; set; }
    public string Id_Usuario { get; set; }
    public string Pais { get; set; }
    public string Producto { get; set; }
    public string Codigo_App { get; set; }
    public string Codigo_Nombre { get; set; }
    public string fecha_ini { get; set; }
    public string fecha_fin { get; set; }
}

public class Apps
{
    public int Id { get; set; }
    public string AppCode { get; set; }
    public string Name { get; set; }
}

