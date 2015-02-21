using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using NAudio.Wave;

namespace Retranslyator
{
    public partial class Form1 : Form
    {
        static string ip = "";
        static Thread recieve_thread;
        static UdpClient udpc;
        static IPEndPoint ipep;
        static Byte[] incoming;
        static MemoryStream sound;
        static WaveIn  waveIn;
        static WaveOut waveOut;
        static WaveFileWriter waveWriter;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Closing);
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            #region NAudio-staff
            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 100;
            waveIn.NumberOfBuffers = 10;
            waveOut = new WaveOut();

            //
            //Дефолтное устройство для записи (если оно имеется)
            //
            waveIn.DeviceNumber = 0;

            //
            //Прикрепляем к событию DataAvailable обработчик, возникающий при наличии записываемых данных
            //
            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(waveIn_DataAvailable);

            //
            //Формат wav-файла - принимает параметры - частоту дискретизации и количество каналов(здесь mono)
            //
            waveIn.WaveFormat = new WaveFormat(44200, 2);
            waveIn.RecordingStopped += new EventHandler<StoppedEventArgs>(waveIn_RecordingStopped);

            sound = new MemoryStream();
            waveWriter = new WaveFileWriter(sound, waveIn.WaveFormat);
            #endregion

            ip = textBox1.Text;
            udpc = new UdpClient(40015);
            ipep = new IPEndPoint(IPAddress.Parse(ip), 40015);

            udpc.Send(new Byte[1], 1, ipep);
            recieve_thread = new Thread(recv);
            recieve_thread.Start();

            waveIn.StartRecording();
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            udpc.Send(e.Buffer, e.Buffer.Length, ipep);
        }

        void waveIn_RecordingStopped(object sender, EventArgs e)
        {
            waveIn.Dispose();
            waveIn = null;
        }

        private void Form1_Closing(object sender, EventArgs e)
        {
            if (waveIn == null)
                return;
            recieve_thread.Abort();
            waveIn.StopRecording();
            waveOut.Dispose();
            waveOut = null;
            udpc = null;
        }

        static void recv()
        {
            BufferedWaveProvider PlayBuffer = new BufferedWaveProvider(waveIn.WaveFormat);
            waveOut.Init(PlayBuffer);
            waveOut.Play();

            while (true)
            {
                incoming = udpc.Receive(ref ipep);
                PlayBuffer.AddSamples(incoming, 0, incoming.Length);
            }
        }
    }
}
