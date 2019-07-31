using System;
using System.Collections.Generic;
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
        private List<Int32> clientsList;
        public Form1()
        {
            InitializeComponent();
            myNetworkInterface = new MyNetworkInterface();
            myNetworkInterface.newClient += onNewClient;
            myNetworkInterface.newDatagram += onNewDatagram;
            clipboardNotification = new ClipboardNotification();
            ClipboardNotification.ClipboardUpdate += onClipboardUpdate;
            checkedListBox1.ItemCheck += CheckedListBox1_ItemCheck;
            clientsList = new List<Int32>();
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
                        Clipboard.SetText(str);
                        skipClipboardNotifications += 2;
                        //                        Console.WriteLine("+skipClipboardNotifications " + skipClipboardNotifications.ToString());
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
                    checkedListBox1.Items.Clear();
                    clientsList.Clear();
                    foreach (Int32 id in myNetworkInterface.clients.Keys)
                    {
                        string s = myNetworkInterface.clients[id].addr.ToString() + ", " + ((myNetworkInterface.clients[id].alive > 0) ? id.ToString("x8") : "offline");
                        checkedListBox1.Items.Add(s, myNetworkInterface.clients[id].selected);
                        clientsList.Add(id);
                    }
                }));
            }
        }

        private void CheckedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            CheckedListBox cb = (CheckedListBox)sender;
            if (cb.SelectedIndex < 0)
                return;

            //когда галочка стоит возвращает FALSE, снята - TRUE !!! WTF??
            myNetworkInterface.setClientEnabled(clientsList[cb.SelectedIndex], !cb.GetItemChecked(cb.SelectedIndex));
        }

        private void onClipboardUpdate(object sender, EventArgs e)
        {
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
                o = Clipboard.GetDataObject();
                if (o != null && o.GetDataPresent(DataFormats.Text))
                    data = Encoding.Default.GetBytes(o.GetData(DataFormats.Text).ToString());
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
