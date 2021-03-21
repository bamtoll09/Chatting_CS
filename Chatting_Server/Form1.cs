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
using System.Text;
using Chatting_Server;

namespace Chatting_Server
{
    public partial class Form1 : Form
    {
        private TcpListener server = null;
        private TcpClient clientSocket = null;

        static int userCount = 0;
        string date;

        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        private StringBuilder sb = new StringBuilder();

        public Form1()
        {
            InitializeComponent();

            this.ActiveControl = this.textBox1;

            Thread thread = new Thread(InitSocket);
            thread.IsBackground = true;
            thread.Start();
        }

        private void InitSocket()
        {
            server = new TcpListener(IPAddress.Any, 9999);
            clientSocket = default(TcpClient);
            server.Start();
            DisplayText(">> Server Started");


            // Thread Loop
            while (true)
            {
                try
                {
                    userCount++;
                    clientSocket = server.AcceptTcpClient();
                    DisplayText(">> Accept connection from client");

                    NetworkStream stream = clientSocket.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string userName = Encoding.Unicode.GetString(buffer, 0, bytes);
                    userName = userName.Substring(0, userName.IndexOf("$"));

                    clientList.Add(clientSocket, userName);

                    SendMessageAll(userName + " 님이 입장하셨습니다.", "", false);

                    SendMessageAll("getClients", "", true);
                    DisplayText(">> Connected: " + userName);
                    listBox1.Items.Add(userName);

                    handleClient hClient = new handleClient();
                    hClient.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);
                    hClient.OnDisconnected += new handleClient.DisconnectedHandler(OnDisconnected);
                    hClient.startClient(clientSocket, clientList);
                }
                catch (SocketException se) { break; }
                catch (Exception ex) { break; }
            }

            clientSocket.Close();
            server.Stop();
        }

        void OnDisconnected(TcpClient clientSocket)
        {
            if (clientList.ContainsKey(clientSocket))
            {
                clientList.TryGetValue(clientSocket, out string userName);
                listBox1.Items.Remove(userName);
                clientList.Remove(clientSocket);
                SendMessageAll("getClients", "", true);
            }
        }

        private void OnReceived(string message, string userName)
        {
            if (message.Equals("leaveChat"))
            {
                string displayMessage = "leave user: " + userName;
                DisplayText(displayMessage);
                SendMessageAll("leaveChat", userName, true);
            } else {
                string displayMessage = "From client [" + userName + "]: " + message;
                DisplayText(displayMessage);
                SendMessageAll(message, userName, true);
            }
        }

        public void SendMessageAll(string message, string userName, bool flag)
        {
            foreach (var pair in clientList)
            {
                date = DateTime.Now.ToString("MM.dd HH:mm:ss");

                TcpClient client = pair.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    if (message.Equals("leaveChat"))
                    {
                        buffer = Encoding.Unicode.GetBytes(userName + " 님이 대화방을 나갔습니다.");
                    }
                    else if (message.Equals("getClients"))
                    {
                        sb.Clear();
                        sb.Append("clients:");
                        foreach (var name in clientList.Values)
                        {
                            sb.Append(name);
                            sb.Append("|");
                        }
                        sb.Remove(sb.Length-1, 1);

                        buffer = Encoding.Unicode.GetBytes(sb.ToString());
                    }
                    else
                        buffer = Encoding.Unicode.GetBytes("[" + date + "] " + userName + ": " + message);
                }
                else { buffer = Encoding.Unicode.GetBytes(message); }

                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
        }

        private void DisplayText(string text)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.BeginInvoke(new MethodInvoker(delegate
                {
                    richTextBox1.AppendText(text + Environment.NewLine);
                }));
            } else { richTextBox1.AppendText(text + Environment.NewLine); }
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                MessageBox.Show(textBox1.Text);
                textBox1.Text = String.Empty;
            }
        }
    }
}
