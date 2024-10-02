using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Collections;
using System.Diagnostics;
using System.Net;

namespace TransparentTest
{

    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        DateTime accessHistory;

        Bitmap memoryImage;

        private static readonly HttpClient client = new HttpClient();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void CopyPixels1(PaintEventArgs e)
        {
       //     e.Graphics.CopyFromScreen(this.Location,
         //       new Point(40, 40), new Size(100, 100));
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
   //         CopyPixels1(e);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            timer1.Interval = trackBar1.Value;
        }

        bool IsFileLocked(string filePath)
        {
            try
            {
                // Attempt to open the file with exclusive access
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // If we get here, the file is not locked
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // If we catch an IOException, the file is locked
                return true;
            }

            return false; // File is not locked
        }

        FileStream LockFile(string filePath)
        {
            try
            {
                // Open the file in exclusive access mode (read/write)
                FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                // Optionally, you can perform operations here
                // For example, reading or writing to the file

                return stream; // Return the stream, which holds the lock
            }
            catch (IOException ex)
            {
                Console.WriteLine("File is locked by another process: " + ex.Message);
                return null; // Could not lock the file
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            pictureBox1.Visible = false;

            Graphics myGraphics = this.CreateGraphics();
            Size s = new Size(512,512);
            memoryImage = new Bitmap(s.Width, s.Height, myGraphics);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);
            memoryGraphics.CopyFromScreen(this.Location.X, this.Location.Y, 0, 0, s);

            memoryImage.Save(txtCapImage.Text);            

            pictureBox1.Visible = true;

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Capbox");

            //storing the values  
            key.SetValue("CapImage", txtCapImage.Text);
            key.SetValue("RetImage", txtRetImage.Text);
            key.SetValue("Speed", trackBar1.Value);
            key.Close();

            SendPrompt(txtPosPrompt.Text, txtNegPrompt.Text);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox1.Checked;
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Capbox");
            if (key != null)
                    {
                //storing the values  
                txtCapImage.Text = (string)key.GetValue("CapImage");
                txtRetImage.Text = (string)key.GetValue("RetImage");
                trackBar1.Value = (int)key.GetValue("Speed");
                timer1.Interval = trackBar1.Value;

                key.Close();
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            this.TopMost = chkAlwaysOnTop.Checked;

            if (System.IO.File.Exists(txtRetImage.Text))
            {
                if (IsFileLocked(txtRetImage.Text))
                {
                    Console.WriteLine("File is locked.");
                }
                else
                {
                    DateTime lastAccess = System.IO.File.GetLastWriteTime(txtRetImage.Text);

                    if (lastAccess == accessHistory)
                    {
                        return;
                    }

                    accessHistory = lastAccess;

                    try
                    {
                        // Load image
                        Image img;
                        using (var bmpTemp = new Bitmap(txtRetImage.Text))
                        {
                            img = new Bitmap(bmpTemp);
                        }
                        pictureBox1.Image = img;
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Invalid image: " + ex.Message);
                    }

                }
            }
        }

        void SendPrompt(string positive, string negative)
        {
            string jsonstrings = File.ReadAllText("d:\\workflow_api.json");

            jsonstrings = "{ \"prompt\": " + jsonstrings + ", \"client_id\": \"1\"}";
            //jsonstrings = "{ \"prompt\": " + jsonstrings + ", \"client_id\": \"" + (Random.Range(10, 3000)).ToString() + "\"}";

            jsonstrings = jsonstrings.Replace("\n", "").Replace("\r", "");

            jsonstrings = jsonstrings.Replace("positiveprompt", positive);
            jsonstrings = jsonstrings.Replace("negativeprompt", negative);
            //jsonstrings = jsonstrings.Replace("564884377592820", Random.Range(0, 3000000).ToString());
 
            byte[] jsonBytes = Encoding.ASCII.GetBytes(jsonstrings); //json contains the {prompt}

            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8188/prompt");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(Encoding.UTF8.GetString(jsonBytes));
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }


                /*
                var www = new UnityWebRequest("http://192.168.0.139:8188/prompt", "POST");
                www.uploadHandler = new UploadHandlerRaw(jsonBytes);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("content-type", "application/json");
                www.SetRequestHeader("accept", "application/json");
                Debug.Log("Sending request");

                Debug.Log("Received: " + www.downloadHandler.text);
                PromptReturn pr = JsonUtility.FromJson<PromptReturn>(www.downloadHandler.text);
                promptReturns.Add(pr);
                www.uploadHandler.Dispose();*/

            }

        private void button3_Click(object sender, EventArgs e)
        {
            SendPrompt("test", "test");
        }
    }
}
