using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;

namespace Chatting_Client
{
    public partial class Form1 : Form
    {
        TcpClient clientSocket = new TcpClient(); // 소켓
        NetworkStream stream = default(NetworkStream);

        string message = string.Empty;
        static string nickname = "HE";

        private bool isConnected = false;
        private bool isConnecting = false;

        private StringBuilder sb = new StringBuilder();

        public Form1()
        {
            InitializeComponent();

            this.ActiveControl = textBox1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void GetMessage() // 메세지 받기
        {
            while (true)
            {
                stream = clientSocket.GetStream();
                int BUFFERSIZE = clientSocket.ReceiveBufferSize;
                byte[] buffer = new byte[BUFFERSIZE];
                int bytes = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.Unicode.GetString(buffer, 0, bytes);

                if (message.Contains("clients:"))
                {
                    string[] newClients = message.Substring(message.IndexOf("clients:") + 8).Split("|");
                    listBox1.Invoke(new MethodInvoker(delegate() {
                        listBox1.Items.Clear();
                        listBox1.Items.AddRange(newClients);
                    })); 

                    DisplayText(message.Substring(0, message.IndexOf("clients:")));
                } else { DisplayText(message); }
            }
        }

        private void SendMessage(string m) // 메세지 보내기
        {
            byte[] buffer = Encoding.Unicode.GetBytes(m);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }

        private void DisplayText(string text) // 메세지 출력
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.BeginInvoke(new MethodInvoker(delegate
                {
                    richTextBox1.AppendText(text + Environment.NewLine);
                }));
            }
            else
                richTextBox1.AppendText(text + Environment.NewLine);
        }

        private void Connect(string url) // Server에 연결
        {
            string[] s = url.Split(":");
            string ip = s[0];
            int port = Convert.ToInt32(s[1]);

            try
            {
                clientSocket.Connect(ip, port); // 접속 IP 및 포트
                stream = clientSocket.GetStream();
            }
            catch (Exception e2)
            {
                MessageBox.Show("서버가 실행중이 아닙니다.", "연결 실패!");
                Application.Exit();
            }

            message = "채팅 서버에 연결 되었습니다.";
            DisplayText(message);

            button1.Text = "set name";
            button1.Enabled = true;

            message = "이름을 설정해주세요.";
            DisplayText(message);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isConnecting == false && isConnected == false)
            {
                string input = textBox1.Text.Trim();
                string ip = "localhost";
                int port = 9999;
                if (input.Contains(":"))
                {
                    string[] s = input.Split(":");
                    ip = s[0];
                    port = Convert.ToInt32(s[1]);
                }
                else if (input.Contains("."))
                {
                    ip = input;
                }
                else if (Regex.IsMatch(input, "^[0-9]{1,5}$"))
                {
                    port = Convert.ToInt32(input);
                }
                else if (!(input.Equals("") || input.Equals("localhost")))
                {
                    DisplayText("[Error] Please Check Input. (IP:PORT) | (IP) | (PORT) | ()");

                    return;
                }

                sb.Clear();
                sb.Append(ip);
                sb.Append(":");
                sb.Append(port);
                Connect(sb.ToString());

                isConnecting = true;
                button1.Text = "connecting...";
                button1.Enabled = false;
                textBox1.Text = String.Empty;
            }
            else if (isConnecting == true && isConnected == false) // Name 설정
            {
                Form1.nickname = textBox1.Text;

                sb.Clear();
                sb.Append(Form1.nickname);
                sb.Append("$");

                textBox1.Focus();
                SendMessage(sb.ToString());
                textBox1.Text = String.Empty;

                button1.Text = "send";

                Thread t_handler = new Thread(GetMessage);
                t_handler.IsBackground = true;
                t_handler.Start();
            }
            else if (isConnecting == false && isConnected == true)
            {
                sb.Clear();
                sb.Append(textBox1.Text);

                textBox1.Focus();
                SendMessage(sb.ToString());
                textBox1.Text = String.Empty;
            }
        } 

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) // 엔터키 눌렀을 때
                button1_Click(this, e);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isConnecting || isConnected)
            {
                byte[] buffer = Encoding.Unicode.GetBytes("leaveChat" + "$");
                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
                Application.ExitThread();
                Environment.Exit(0);
            }
        }
    }
}
