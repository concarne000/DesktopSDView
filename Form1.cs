using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;

namespace TransparentTest
{
    public partial class Form1 : Form
    {
        // Constants for moving the window
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        // Importing user32.dll functions for window dragging
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // Variables to hold access history and screen capture
        private DateTime lastAccessTime;
        private Bitmap screenCapture;

        System.Random randomGen;

        // HttpClient for sending web requests
        private static readonly HttpClient httpClient = new HttpClient();

        public Form1()
        {
            InitializeComponent();
            randomGen = new System.Random();
        }

        // Button click to exit the application
        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Placeholder for copying pixels (commented out code)
        private void CopyPixels1(PaintEventArgs e)
        {
            // e.Graphics.CopyFromScreen(this.Location, new Point(40, 40), new Size(100, 100));
        }

        // Placeholder for handling form paint events (commented out code)
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // CopyPixels1(e);
        }

        // Adjust timer interval based on trackBar value
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            timer1.Interval = trackBar1.Value;
        }

        // Check if a file is locked by another process
        private bool IsFileLocked(string filePath)
        {
            try
            {
                // Attempt to open the file with exclusive access
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true; // File is locked
            }

            return false; // File is not locked
        }

        // Attempt to lock a file for read/write access
        private FileStream LockFile(string filePath)
        {
            try
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File is locked by another process: {ex.Message}");
                return null; // Could not lock the file
            }
        }

        // Timer tick event for capturing the screen and saving the image
        private void timer1_Tick(object sender, EventArgs e)
        {
            // Hide the PictureBox while capturing the screen
            pictureBox1.Visible = false;

            // Capture the screen
            using (Graphics formGraphics = this.CreateGraphics())
            {
                Size captureSize = new Size(512, 512);
                screenCapture = new Bitmap(captureSize.Width, captureSize.Height, formGraphics);
                using (Graphics captureGraphics = Graphics.FromImage(screenCapture))
                {
                    captureGraphics.CopyFromScreen(this.Location.X, this.Location.Y, 0, 0, captureSize);
                }
            }

            // Save the captured image
            screenCapture.Save(txtCapImage.Text);

            // Show the PictureBox again
            pictureBox1.Visible = true;

            // Save settings in the registry
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Capbox"))
            {
                key.SetValue("CapImage", txtCapImage.Text);
                key.SetValue("RetImage", txtRetImage.Text);
                key.SetValue("Speed", trackBar1.Value);
            }

            // Send the prompts to the server
            SendPrompt(txtPosPrompt.Text, txtNegPrompt.Text);
        }

        // Handle mouse down event to move the form
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // Enable or disable the timer based on checkbox state
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox1.Checked;
        }

        // Handle mouse down event to move the form from another PictureBox
        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // Load form and restore settings from the registry
        private void Form1_Load(object sender, EventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Capbox"))
            {
                if (key != null)
                {
                    txtCapImage.Text = (string)key.GetValue("CapImage");
                    txtRetImage.Text = (string)key.GetValue("RetImage");
                    trackBar1.Value = (int)key.GetValue("Speed");
                    timer1.Interval = trackBar1.Value;
                }
            }
        }

        // Timer tick event for updating the PictureBox with a new image
        private void timer2_Tick(object sender, EventArgs e)
        {
            this.TopMost = chkAlwaysOnTop.Checked;

            // Check if the return image exists and is not locked
            if (File.Exists(txtRetImage.Text) && !IsFileLocked(txtRetImage.Text))
            {
                DateTime lastWriteTime = File.GetLastWriteTime(txtRetImage.Text);

                // If the file hasn't changed, return
                if (lastWriteTime == lastAccessTime)
                {
                    return;
                }

                lastAccessTime = lastWriteTime;

                // Load the new image and display it
                try
                {
                    using (var bmpTemp = new Bitmap(txtRetImage.Text))
                    {
                        pictureBox1.Image = new Bitmap(bmpTemp);
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Invalid image: {ex.Message}");
                }
            }
        }

        // Send a prompt to the server with positive and negative prompts
        private void SendPrompt(string positive, string negative)
        {
            string jsonTemplate = File.ReadAllText("workflow_api.json");

            // Create JSON request with positive and negative prompts
            string jsonRequest = "{ \"prompt\": " + jsonTemplate + ", \"client_id\": \"1\"}";
            jsonRequest = jsonRequest.Replace("\n", "").Replace("\r", "");
            jsonRequest = jsonRequest.Replace("positiveprompt", positive);
            jsonRequest = jsonRequest.Replace("negativeprompt", negative);
            jsonRequest = jsonRequest.Replace("666999", randomGen.Next().ToString());

            byte[] jsonBytes = Encoding.ASCII.GetBytes(jsonRequest);

            // Send HTTP request
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8188/prompt");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(Encoding.UTF8.GetString(jsonBytes));
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                string result = streamReader.ReadToEnd();
            }
        }

        // Button click to send a test prompt
        private void button3_Click(object sender, EventArgs e)
        {
            SendPrompt("test", "test");
        }
    }
}