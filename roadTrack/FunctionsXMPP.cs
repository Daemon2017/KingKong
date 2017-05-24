using Sharp.Xmpp.Extensions;
using System.Windows.Forms;

namespace roadTrack
{
    public partial class Form1
    {
        public void FileTransferCallback(bool accepted, FileTransfer transfer)
        {
            textBox1.Invoke((MethodInvoker)delegate
            {
                textBox1.Text = transfer.To + " has " + (accepted == true ? "accepted " : "rejected ") + "the transfer of " + transfer.Name + ".";
            });
        }

        public void OnFileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            textBox1.Invoke((MethodInvoker)delegate
            {
                textBox1.Text = "Transferring " + e.Transfer.Name + "..." + e.Transfer.Transferred + "/" + e.Transfer.Size + " Bytes";
            });

            locked = true;
            lockedFrames = 0;
        }

        public void OnFileTransferAborted(object sender, FileTransferAbortedEventArgs e)
        {
            textBox1.Invoke((MethodInvoker)delegate
            {
                textBox1.Text = "The transfer of " + e.Transfer.Name + " has been aborted.";
            });
        }
    }
}
