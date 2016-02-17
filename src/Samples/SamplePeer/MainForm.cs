﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetMQ;
using NetMQ.Zyre;
using NetMQ.Zyre.ZyreEvents;

namespace SamplePeer
{
    public partial class MainForm : Form
    {
        private readonly string _name;
        private readonly Zyre _zyre;
        private readonly List<Peer> _connectedPeers;
        private readonly List<Group> _ownGroups;
        private readonly List<Group> _peerGroups;
        private readonly Guid _uuid;
        private string _endpoint;

        public MainForm(string name)
        {
            InitializeComponent();
            btnStop.Enabled = false;
            _connectedPeers = new List<Peer>();
            peerBindingSource.DataSource = _connectedPeers;
            _ownGroups = new List<Group>();
            ownGroupBindingSource.DataSource = _ownGroups;
            _peerGroups = new List<Group>();
            peerGroupBindingSource.DataSource = _peerGroups;
            _zyre = new Zyre(_name, NodeLogger);
            if (!string.IsNullOrEmpty(name))
            {
                _zyre.SetName(name);
            }
            _name = _zyre.Name();
            _uuid = _zyre.Uuid();
            _zyre.EnterEvent += ZyreEnterEvent;
            _zyre.StopEvent += ZyreStopEvent;
            _zyre.ExitEvent += ZyreExitEvent;
            _zyre.EvasiveEvent += ZyreEvasiveEvent;
            _zyre.JoinEvent += ZyreJoinEvent;
            _zyre.LeaveEvent += ZyreLeaveEvent;
            _zyre.WhisperEvent += ZyreWhisperEvent;
            _zyre.ShoutEvent += ZyreShoutEvent;
        }

        private void ZyreShoutEvent(object sender, ZyreEventShout e)
        {
            var msg = e.Content.Pop().ConvertToString();
            var uuidShort = e.SenderUuid.ToShortString6();
            var str = string.Format("Shout from {0} to group={1}: {2}", uuidShort, e.GroupName, msg);
            UpdateMessageReceived(str);
            EventsLogger($"Shout: {e.SenderName} {e.SenderUuid.ToShortString6()} shouted message {msg} to group:{e.GroupName}");
        }

        private void ZyreWhisperEvent(object sender, ZyreEventWhisper e)
        {
            var msg = e.Content.Pop().ConvertToString();
            var uuidShort = e.SenderUuid.ToShortString6();
            var str = string.Format("Whisper from {0}: {1}", uuidShort, msg);
            UpdateMessageReceived(str);
            EventsLogger($"Whisper: {e.SenderName} {e.SenderUuid.ToShortString6()} whispered message {msg}");
        }

        private void ZyreJoinEvent(object sender, ZyreEventJoin e)
        {
            EventsLogger($"Join: {e.SenderName} {e.SenderUuid.ToShortString6()} Group:{e.GroupName}");
            UpdateAndShowGroups();
        }

        private void ZyreLeaveEvent(object sender, ZyreEventLeave e)
        {
            EventsLogger($"Leave: {e.SenderName} {e.SenderUuid.ToShortString6()} Group:{e.GroupName}");
            UpdateAndShowGroups();
        }

        private void ZyreEvasiveEvent(object sender, ZyreEventEvasive e)
        {
            EventsLogger($"Evasive: {e.SenderName} {e.SenderUuid.ToShortString6()}");
        }

        private void ZyreExitEvent(object sender, ZyreEventExit e)
        {
            _connectedPeers.RemoveAll(x => x.SenderUuid == e.SenderUuid);
            EventsLogger($"Exited: {e.SenderName} {e.SenderUuid.ToShortString6()}");
            UpdateAndShowGroups();
        }

        private void ZyreStopEvent(object sender, ZyreEventStop e)
        {
            _connectedPeers.RemoveAll(x => x.SenderUuid == e.SenderUuid);
            EventsLogger($"Stopped: {e.SenderName} {e.SenderUuid.ToShortString6()}");
            UpdateAndShowGroups();
        }

        private void ZyreEnterEvent(object sender, ZyreEventEnter e)
        {
            var peer = new Peer(e.SenderName, e.SenderUuid, e.Address);
            _connectedPeers.Add(peer);
            EventsLogger($"Entered: {e.SenderName} {e.SenderUuid.ToShortString6()} at {e.Address} with {e.Headers.Count} headers");
            if (e.Headers.Count > 0)
            {
                var sb = new StringBuilder($"Headers: " + Environment.NewLine);
                foreach (var header in e.Headers)
                {
                    sb.AppendLine(header.Key + "|" + header.Value);
                }
                EventsLogger(sb.ToString());
            }
            UpdateAndShowGroups();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            DisplayTitle();
        }

        private void DisplayTitle()
        {
            Text = $"Zyre Node: {_name} {_uuid}";
            if (!string.IsNullOrEmpty(_endpoint))
            {
                Text += $" -- listening at {_endpoint}";
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _zyre.Dispose();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            _zyre.Start();
            _endpoint = _zyre.EndPoint(); // every time we start, we bind our RouterSocket to a new port
            DisplayTitle();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnStart.Enabled = true;
            _zyre.Stop();
            _endpoint = null;  // every time we start, we bind our RouterSocket to a new port
            DisplayTitle();
            _connectedPeers.Clear();
        }
        public void MessageLogger(string str)
        {
            var msg = $"{DateTime.Now.ToString("h:mm:ss.fff")} {str}";
            Logger(rtbMessages, msg);
        }

        public void NodeLogger(string str)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var msg = $"Thd:{threadId} {DateTime.Now.ToString("h:mm:ss.fff")} ({_name}) {str}";
            Logger(rtbNodeLog, msg);
        }

        public void EventsLogger(string str)
        {
            var msg = $"{DateTime.Now.ToString("h:mm:ss.fff")} ({_name}) {str}";
            Logger(rtbEventsLog, msg);
        }

        /// <summary>
        /// Put a message at the TOP of the richTextBox
        /// </summary>
        /// <param name="rtb">the richTextBox</param>
        /// <param name="msg">the message</param>
        private void Logger(RichTextBox rtb, string msg)
        {
            if (rtb.InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Logger(rtb, msg)));
            }
            else
            {
                const int longestText = 1000000;
                rtb.Text = msg + Environment.NewLine + rtb.Text;
                if (rtb.TextLength > longestText)
                {
                    rtb.Text = rtb.Text.Substring(0, longestText / 2);
                }
            }
        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            var groupName = txtGroupName.Text;
            if (string.IsNullOrEmpty(groupName))
            {
                MessageBox.Show("You must enter a group name");
                return;
            }
            _zyre.Join(groupName);
            Thread.Sleep(10);
            UpdateAndShowGroups();
        }

        private void btnLeave_Click(object sender, EventArgs e)
        {
            var groupName = txtGroupName.Text;
            if (string.IsNullOrEmpty(groupName))
            {
                MessageBox.Show("You must enter a group name");
                return;
            }
            _zyre.Leave(groupName);
            Thread.Sleep(10);
            UpdateAndShowGroups();
        }

        private void btnWhisper_Click(object sender, EventArgs e)
        {
            var chatMessage = txtWhisperMessage.Text;
            if (string.IsNullOrEmpty(chatMessage))
            {
                MessageBox.Show("You must enter a chat message.");
                return;
            }
            var selectedRowCount = peerDataGridView.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (selectedRowCount == 0)
            {
                MessageBox.Show("You must select a row in the Peer Groups list");
                return;
            }
            var uuid = peerDataGridView.SelectedRows[0].Cells[2].Value.ToString();
            Guid guid;
            if (!Guid.TryParse(uuid, out guid))
            {
                MessageBox.Show("Unable to convert SenderUuid to Guid");
                return;
            }
            var msg = new NetMQMessage();
            msg.Append(chatMessage);
            _zyre.Whisper(guid, msg);
        }

        private void btnShout_Click(object sender, EventArgs e)
        {
            var chatMessage = txtShoutMessage.Text;
            if (string.IsNullOrEmpty(chatMessage))
            {
                MessageBox.Show("You must enter a chat message.");
                return;
            }
            var selectedRowCount = peerGroupDataGridView.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (selectedRowCount == 0)
            {
                MessageBox.Show("You must select a row in the Connected Peers list");
                return;
            }
            var groupName = peerGroupDataGridView.SelectedRows[0].Cells[0].Value.ToString();
            var msg = new NetMQMessage();
            msg.Append(chatMessage);
            _zyre.Shout(groupName, msg);
        }

        private void UpdateAndShowGroups()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(UpdateAndShowGroups));
            }
            else
            {
                var ownGroups =_zyre.OwnGroups();
                _ownGroups.Clear();
                foreach (var ownGroup in ownGroups)
                {
                    _ownGroups.Add(new Group(ownGroup));
                }
                var peerGroups = _zyre.PeerGroups();
                _peerGroups.Clear();
                foreach (var peerGroup in peerGroups)
                {
                    _peerGroups.Add(new Group(peerGroup));
                }

                peerBindingSource.ResetBindings(false);
                ownGroupBindingSource.ResetBindings(false);
                peerGroupBindingSource.ResetBindings(false);
            }
        }

        private void UpdateMessageReceived(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => UpdateMessageReceived(msg)));
            }
            else
            {
                MessageLogger(msg);
            }
        }

        /// <summary>
        /// Provide generic error handling for a DataGridView error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void peerDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            var dgv = (DataGridView)sender;
            var senderName = dgv.Name;
            var senderError = senderName + "_DataError()";
            MessageBox.Show("Error happened " + e.Context.ToString() + "\n" + e.Exception, senderError);

            if (e.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Commit error", senderError);
            }
            if (e.Context == DataGridViewDataErrorContexts.CurrentCellChange)
            {
                MessageBox.Show("Cell change", senderError);
            }
            if (e.Context == DataGridViewDataErrorContexts.Parsing)
            {
                MessageBox.Show("Parsing error", senderError);
            }
            if (e.Context == DataGridViewDataErrorContexts.LeaveControl)
            {
                MessageBox.Show("Leave control error", senderError);
            }

            if ((e.Exception) is System.Data.ConstraintException)
            {
                var view = (DataGridView)sender;
                view.Rows[e.RowIndex].ErrorText = "an error";
                view.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "an error";

                e.ThrowException = false;
            }
        }
    }
}
