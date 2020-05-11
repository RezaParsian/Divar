using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace divarapi
{
    public partial class Form1 : Form
    {
        List<DivarPost> database = new List<DivarPost>();
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (citycombo.SelectedIndex != -1)
            {
                progres.Visible = true;
                var posts = await Task.Run(() => GetData());
                dataGridView1.DataSource = null;
                database.InsertRange(0, posts);
                dataGridView1.DataSource = database;
               
                if (cookielist.Items.Count < cookielist.SelectedIndex)
                {
                    cookielist.SelectedIndex = -1;
                }

                cookielist.SelectedIndex++;
            }
            else
            {
                MessageBox.Show("لطفا ابتدا یک شهر انتخاب فرمایید");
                citycombo.Focus();
            }

        }

        private List<DivarPost> GetData()
        {
            curenttxt.Text = curenttxt.Text == "-1" ? "0" : curenttxt.Text;
            string url = "https://api.divar.ir/v8/search/" + (citycombo.SelectedItem as dynamic).Value + "/house-villa-sell";
            string data = "{\"json_schema\": {\"category\": {\"value\": \"house-villa-sell\"}},\"last-post-date\": " + curenttxt.Text + "}";
            string result = "";
            List<DivarPost> posts = new List<DivarPost>();

            try
            {
                using (WebClient wb = new WebClient())
                {
                    result = wb.UploadString(url, data);
                    JObject j = JObject.Parse(result);
                    curenttxt.Text = j["last_post_date"].ToString();
                    if (curenttxt.Text == "-1")
                    {
                        MessageBox.Show("به اخر لیست رسیدید");
                    }
                    foreach (var item in j["widget_list"])
                    {

                        posts.Add(
                            new DivarPost
                            {
                                Title = item["data"]["title"].ToString() != "" ? item["data"]["title"].ToString() : "-",
                                Des = item["data"]["description"].ToString() != "" ? item["data"]["description"].ToString() : "-",
                                Date = item["data"]["normal_text"].ToString() != "" ? item["data"]["normal_text"].ToString() : "-",
                                Token = item["data"]["token"].ToString() != "" ? item["data"]["token"].ToString() : "-",
                                Number = GetNumber(item["data"]["token"].ToString())
                            }
                            );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            progres.Visible = false;
            return posts;
        }

        private void SaveCities()
        {
            string pattern = @"""places.*}}}";
            string input = GetCities();
            RegexOptions options = RegexOptions.Multiline;
            Match m = Regex.Match(input, pattern, options);

            string newmatch = m.Value.Replace("\\", "");
            newmatch = "{" + newmatch;
            JObject jb = JObject.Parse(newmatch);

            StreamWriter sw = new StreamWriter("places.json");
            sw.Write(jb.ToString());
            sw.Close();


            FetchList();
        }

        private string GetCities()
        {
            string url = "https://divar.ir/s/tehran";
            string result = "";

            try
            {
                using (WebClient wb = new WebClient())
                {
                    wb.Headers[HttpRequestHeader.ContentType] = "application/json; charset=utf-8";
                    result = wb.DownloadString(url);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return result;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("places.json"))
            {
                FetchList();
            }
            else
            {
                SaveCities();
            }
            if (Directory.Exists("Cookie"))
            {
                string[] cookiefiles = Directory.GetFiles("Cookie");

                foreach (var item in cookiefiles)
                {
                    StreamReader sr = new StreamReader(item);

                    string pattern = @"token"",""Value"":""(.*?)""";
                    string input = sr.ReadToEnd();
                    RegexOptions options = RegexOptions.Multiline;

                    Match m = Regex.Match(input, pattern, options);

                    cookielist.Items.Add(m.Groups[1].Value);

                    sr.Close();
                }
                cookielist.SelectedIndex = 0;
            }
        }

        private void FetchList()
        {
            string readfile = "";
            StreamReader sr = new StreamReader("places.json");
            readfile = sr.ReadToEnd();
            sr.Close();

            JObject jb = JObject.Parse(readfile);

            foreach (var item in jb["places"])
            {
                //MessageBox.Show(item.First["name"].ToString());

                cities city = new cities()
                {
                    Text = item.First["name"].ToString(),
                    Value = item.First["id"].ToString()
                };

                citycombo.Items.Add(city);
            }
            citycombo.Items.RemoveAt(0);

        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            string token = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString();
            System.Diagnostics.Process.Start($"https://divar.ir/v/{token}");
        }

        private void citycombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
        }

        private string GetNumber(string txttocken)
        {
            string url = "https://api.divar.ir/v5/posts/" + txttocken + "/contact/";
            JObject jb = null;
            try
            {
                using (WebClient wb = new WebClient())
                {
                    //WebProxy wp = new WebProxy("137.59.155.253", 8088);
                    //wp.UseDefaultCredentials = false;
                    //wp.BypassProxyOnLocal = false;  
                    //wb.Proxy = wp;

                    wb.Headers[HttpRequestHeader.Host] = "api.divar.ir";
                    wb.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:77.0) Gecko/20100101 Firefox/77.0";
                    wb.Headers[HttpRequestHeader.Accept] = "application/json, text/plain, */*";
                    wb.Headers[HttpRequestHeader.AcceptLanguage] = "en";
                    wb.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate, br";
                    wb.Headers[HttpRequestHeader.Referer] = "https://divar.ir/v/" + txttocken;
                    wb.Headers[HttpRequestHeader.Cookie] = "did=bdd79193-eb77-456a-ad56-077766a52e67; _gcl_au=1.1.444842256.1580543925; city=esfahan; MEDIAAD_USER_ID=4e4aeae7-4e49-4e24-a954-9fd5000e570c; device_id=1961478167; _hjid=73c493a0-5571-4fd8-9b77-ef2d08ce71fb; token="+cookielist.SelectedItem.ToString()+"; _ga=GA1.2.144012545.1588844123; _gid=GA1.2.959140253.1588844123; _pk_id.1.fbba=69e3b8d61114fb79.1588844123.8.1588955692.1588954221.; _pk_ses.1.fbba=1";

                    string result = wb.DownloadString(url);

                    jb = JObject.Parse(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return jb.First.First.First.First["phone"].ToString();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string token = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(cookielist.SelectedItem.ToString());
            MessageBox.Show(cookielist.Items.Count.ToString());
        }
    }
}
