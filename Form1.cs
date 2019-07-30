using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetOpenClipboardWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetWindowText(int hwnd, StringBuilder text, int count);

        private MyNetworkInterface myNetworkInterface;
        private ClipboardNotification clipboardNotification;
        private int skipClipboardNotifications = 0;
        private int clipboardRxNb = 0;
        public Form1()
        {
            InitializeComponent();
            myNetworkInterface = new MyNetworkInterface();
            myNetworkInterface.newClient += onNewClient;
            myNetworkInterface.newDatagram += onNewDatagram;
            clipboardNotification = new ClipboardNotification();
            ClipboardNotification.ClipboardUpdate += onClipboardUpdate;
        }

        private void onNewDatagram(object sender, EventArgs e)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new MethodInvoker(delegate
                {
                    byte[] data = myNetworkInterface.getData();
                    string str = System.Text.Encoding.Default.GetString(data);
                    clipboardRxNb++;
                    label1.Text = "Rx:" + clipboardRxNb.ToString();
                    try
                    {
                        Clipboard.Clear();
                        skipClipboardNotifications++;
                        Clipboard.SetText(str);
                        skipClipboardNotifications += 2;
                        Console.WriteLine("+skipClipboardNotifications " + skipClipboardNotifications.ToString());
                    }
                    catch (Exception ex)
                    {
                        showMessProblem(ex);
                    }
                }));
            }
        }
        private void onNewClient(object sender, EventArgs e)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new MethodInvoker(delegate
                {
                    textBox1.Clear();
                    foreach (Int32 id in myNetworkInterface.clients.Keys)
                    {
                        textBox1.AppendText(myNetworkInterface.clients[id].ToString() + "\n");
                    }
                }));
            }
        }

        private void onClipboardUpdate(object sender, EventArgs e)
        {
            Console.WriteLine("-skipClipboardNotifications " + skipClipboardNotifications.ToString());
            if (skipClipboardNotifications == 0)
            {
                if (myNetworkInterface.clients.Count() > 0)
                {
                    byte[] clip = myClipboardGet();
                    if (clip != null && clip.Length > 0)
                    {
                        myNetworkInterface.send(clip);
                    }
                }
            }
            else
                skipClipboardNotifications--;
        }

        private byte[] myClipboardGet()
        {
            IDataObject o;
            byte[] data = null;
            try
            {
                Console.WriteLine(".");
                o = Clipboard.GetDataObject();
                if (o != null)
                {
                    Console.WriteLine("!");
                    if (o.GetDataPresent(DataFormats.Text))
                    {
                        data = Encoding.ASCII.GetBytes(o.GetData(DataFormats.Text).ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                showMessProblem(ex);
            }
            return data;
        }

        private void showMessProblem(Exception ex)
        {
            string msg = ex.Message;
            msg += Environment.NewLine;
            msg += Environment.NewLine;
            msg += "The problem:";
            msg += Environment.NewLine;
            IntPtr hwnd = GetOpenClipboardWindow();
            StringBuilder sb = new StringBuilder(501);
            GetWindowText(hwnd.ToInt32(), sb, 500);
            msg += sb.ToString();
            MessageBox.Show(msg);
        }
    }
}
