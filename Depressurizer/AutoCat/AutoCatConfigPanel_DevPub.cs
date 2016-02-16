﻿/*
This file is part of Depressurizer.
Copyright 2011, 2012, 2013 Steve Labbe.

Depressurizer is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Depressurizer is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Depressurizer.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Depressurizer {
    public partial class AutoCatConfigPanel_DevPub : AutoCatConfigPanel {

        // used to remove unchecked items from the Add and Remove checkedlistbox.
        private Thread workerThread;
        private bool loaded = false;
        private GameList ownedGames;

        public AutoCatConfigPanel_DevPub(GameList g) {
            
            InitializeComponent();

            ownedGames = g;

            ttHelp.Ext_SetToolTip( helpPrefix, GlobalStrings.DlgAutoCat_Help_Prefix );
            ttHelp.Ext_SetToolTip(list_helpScore, GlobalStrings.DlgAutoCat_Help_MinScore);
            ttHelp.Ext_SetToolTip(list_helpOwnedOnly, GlobalStrings.DlgAutoCat_Help_ListOwnedOnly);
            ttHelp.Ext_SetToolTip(btnDevSelected, GlobalStrings.DlgAutoCat_Help_DevSelected);
            ttHelp.Ext_SetToolTip(btnPubSelected, GlobalStrings.DlgAutoCat_Help_PubSelected);

            FillDevList();
            FillPubList();

            clbDevelopersSelected.DisplayMember = "text";
            clbPublishersSelected.DisplayMember = "text";

            //Hide count columns
            lstDevelopers.Columns[1].Width = 0;
            lstPublishers.Columns[1].Width = 0;

        }

        public void FillDevList(ICollection<string> preChecked = null)
        {
            if (Program.GameDB != null)
            {
                IEnumerable<Tuple<string, int>> tagList = Program.GameDB.CalculateSortedDevList( chkOwnedOnly.Checked ? ownedGames : null, (int)list_numScore.Value);
                clbDevelopersSelected.Items.Clear();
                lstDevelopers.BeginUpdate();
                lstDevelopers.Items.Clear();
                foreach (Tuple<string, int> tag in tagList)
                {
                    ListViewItem newItem = new ListViewItem(string.Format("{0} [{1}]", tag.Item1, tag.Item2));
                    newItem.Tag = tag.Item1;
                    if (preChecked != null && preChecked.Contains(tag.Item1)) newItem.Checked = true;
                    newItem.SubItems.Add(tag.Item2.ToString());
                    lstDevelopers.Items.Add(newItem);
                }
                lstDevelopers.Columns[0].Width = -1;
                SortDevelopers(1, SortOrder.Descending);
                lstDevelopers.EndUpdate();
                chkAllDevelopers.Text = "All (" + lstDevelopers.Items.Count.ToString() + ")";
            }
        }

        public void FillPubList(ICollection<string> preChecked = null)
        {
            if (Program.GameDB != null)
            {
                IEnumerable<Tuple<string, int>> tagList = Program.GameDB.CalculateSortedDevList(chkOwnedOnly.Checked ? ownedGames : null, (int)list_numScore.Value);
                clbPublishersSelected.Items.Clear();
                lstPublishers.BeginUpdate();
                lstPublishers.Items.Clear();
                foreach (Tuple<string, int> tag in tagList)
                {
                    ListViewItem newItem = new ListViewItem(string.Format("{0} [{1}]", tag.Item1, tag.Item2));
                    newItem.Tag = tag.Item1;
                    if (preChecked != null && preChecked.Contains(tag.Item1)) newItem.Checked = true;
                    newItem.SubItems.Add(tag.Item2.ToString());
                    lstPublishers.Items.Add(newItem);
                }
                lstPublishers.Columns[0].Width = -1;
                SortPublishers(1, SortOrder.Descending);
                lstPublishers.EndUpdate();
                chkAllPublishers.Text = "All (" + lstPublishers.Items.Count.ToString() + ")";
            }
        }

        public override void LoadFromAutoCat( AutoCat autocat ) {
            AutoCatDevPub ac = autocat as AutoCatDevPub;
            if( ac == null ) return;
            chkAllDevelopers.Checked = ac.AllDevelopers;
            chkAllPublishers.Checked = ac.AllPublishers;
            txtPrefix.Text = ac.Prefix;

            lstDevelopers.BeginUpdate();
            foreach ( ListViewItem item in lstDevelopers.Items ) {
                item.Checked = ac.Developers.Contains( item.Text );
            }
            lstDevelopers.EndUpdate();

            lstPublishers.BeginUpdate();
            foreach (ListViewItem item in lstPublishers.Items)
            {
                item.Checked = ac.Publishers.Contains(item.Text);
            }
            lstPublishers.EndUpdate();
            loaded = true;
        }

        public override void SaveToAutoCat( AutoCat autocat ) {
            AutoCatDevPub ac = autocat as AutoCatDevPub;
            if( ac == null ) return;
            ac.Prefix = txtPrefix.Text;
            ac.AllDevelopers = chkAllDevelopers.Checked;
            ac.AllPublishers = chkAllPublishers.Checked;

            ac.Developers.Clear();
            if (!chkAllDevelopers.Checked)
            {
                foreach (ListViewItem item in clbDevelopersSelected.CheckedItems)
                {
                    ac.Developers.Add(item.Text);
                }
                //foreach (ListViewItem i in lstDevelopers.Items)
                //{
                //    if (i.Checked) ac.Developers.Add(i.Text);
                //}
            }

            ac.Publishers.Clear();
            if (!chkAllPublishers.Checked)
            {
                foreach (ListViewItem item in clbPublishersSelected.CheckedItems)
                {
                    ac.Publishers.Add(item.Text);
                }
                //foreach (ListViewItem i in lstDevelopers.Items)
                //{
                //    if (i.Checked) ac.Developers.Add(i.Text);
                //}
            }
        }

        private void SetAllListCheckStates( ListView list, bool to ) {
            foreach( ListViewItem item in list.Items ) {
                item.Checked = to;
            }
        }

        private void btnDevCheckAll_Click( object sender, EventArgs e ) {
            SetAllListCheckStates( lstDevelopers, true );
        }

        private void btnDevUncheckAll_Click( object sender, EventArgs e ) {
            loaded = false;
            FillDevList();
            loaded = true;
        }

        private void btnPubCheckAll_Click(object sender, EventArgs e)
        {
            SetAllListCheckStates(lstPublishers, true);
        }

        private void btnPubUncheckAll_Click(object sender, EventArgs e)
        {
            loaded = false;
            FillPubList();
            loaded = true;
        }

        private void cmdListRebuild_Click(object sender, EventArgs e)
        {
            HashSet<string> checkedTags = new HashSet<string>();
            foreach (ListViewItem item in lstDevelopers.CheckedItems)
            {
                checkedTags.Add(item.Tag as string);
            }
            FillDevList(checkedTags);

            checkedTags = new HashSet<string>();
            foreach (ListViewItem item in lstPublishers.CheckedItems)
            {
                checkedTags.Add(item.Tag as string);
            }
            FillPubList(checkedTags);
        }

        private void SortDevelopers(int c, SortOrder so)
        {
            // Create a comparer.
            lstDevelopers.ListViewItemSorter =
                new ListViewComparer(c, so);

            // Sort.
            lstDevelopers.Sort();
        }

        private void SortPublishers(int c, SortOrder so)
        {
            // Create a comparer.
            lstPublishers.ListViewItemSorter =
                new ListViewComparer(c, so);

            // Sort.
            lstDevelopers.Sort();
        }

        #region Developers

        private void chkAllDevelopers_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAllDevelopers.Checked)
            {
                lstDevelopers.Enabled = false;
                clbDevelopersSelected.Enabled = false;
                btnDevCheckAll.Enabled = false;
                btnDevUncheckAll.Enabled = false;
            }
            else
            {
                lstDevelopers.Enabled = true;
                clbDevelopersSelected.Enabled = true;
                btnDevCheckAll.Enabled = true;
                btnDevUncheckAll.Enabled = true;
            }
        }

        private void lstDevelopers_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Checked) clbDevelopersSelected.Items.Add(e.Item, true);
            else if ((!e.Item.Checked) && loaded && clbDevelopersSelected.Items.Contains(e.Item))
            {
                workerThread = new Thread(new ParameterizedThreadStart(DevelopersItemWorker));
                workerThread.Start(e.Item);
            }
        }

        private void clbDevelopersSelected_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Unchecked)
            {
                ((ListViewItem)clbDevelopersSelected.Items[e.Index]).Checked = false;
            }
        }

        delegate void DevItemCallback(ListViewItem obj);

        private void DevelopersRemoveItem(ListViewItem obj)
        {
            if (this.clbDevelopersSelected.InvokeRequired)
            {
                DevItemCallback callback = new DevItemCallback(DevelopersRemoveItem);
                this.Invoke(callback, new object[] { obj });
            }
            else
            {
                clbDevelopersSelected.Items.Remove(obj);
            }
        }

        private void DevelopersItemWorker(object obj)
        {
            DevelopersRemoveItem((ListViewItem)obj);
        }

        private void btnDevSelected_Click(object sender, EventArgs e)
        {
            if (splitDevTop.Panel1Collapsed)
            {
                splitDevTop.Panel1Collapsed = false;
                btnDevSelected.Text = "<";
            }
            else
            {
                splitDevTop.Panel1Collapsed = true;
                btnDevSelected.Text = ">";
            }
        }

        #endregion

        #region Publishers

        private void chkAllPublishers_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAllPublishers.Checked)
            {
                lstPublishers.Enabled = false;
                clbPublishersSelected.Enabled = false;
                btnPubCheckAll.Enabled = false;
                btnPubUncheckAll.Enabled = false;
            }
            else
            {
                lstPublishers.Enabled = true;
                clbPublishersSelected.Enabled = true;
                btnPubCheckAll.Enabled = true;
                btnPubUncheckAll.Enabled = true;
            }
        }

        private void lstPublishers_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Checked) clbPublishersSelected.Items.Add(e.Item, true);
            else if ((!e.Item.Checked) && loaded && clbPublishersSelected.Items.Contains(e.Item))
            {
                workerThread = new Thread(new ParameterizedThreadStart(PublishersItemWorker));
                workerThread.Start(e.Item);
            }
        }

        private void clbPublishersSelected_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Unchecked)
            {
                ((ListViewItem)clbPublishersSelected.Items[e.Index]).Checked = false;
            }
        }

        delegate void PubItemCallback(ListViewItem obj);

        private void PublishersRemoveItem(ListViewItem obj)
        {
            if (this.clbPublishersSelected.InvokeRequired)
            {
                PubItemCallback callback = new PubItemCallback(PublishersRemoveItem);
                this.Invoke(callback, new object[] { obj });
            }
            else
            {
                clbPublishersSelected.Items.Remove(obj);
            }
        }

        private void PublishersItemWorker(object obj)
        {
            PublishersRemoveItem((ListViewItem)obj);
        }

        private void btnPubSelected_Click(object sender, EventArgs e)
        {
            if (splitPubTop.Panel1Collapsed)
            {
                splitPubTop.Panel1Collapsed = false;
                btnPubSelected.Text = "<";
            }
            else
            {
                splitPubTop.Panel1Collapsed = true;
                btnPubSelected.Text = ">";
            }
        }

        #endregion

        private void nameascendingDev_Click(object sender, EventArgs e)
        {
            SortDevelopers(0, SortOrder.Ascending);
        }

        private void namedescendingDev_Click(object sender, EventArgs e)
        {
            SortDevelopers(0, SortOrder.Descending);
        }

        private void countascendingDev_Click(object sender, EventArgs e)
        {
            SortDevelopers(1, SortOrder.Ascending);
        }

        private void countdescendingDev_Click(object sender, EventArgs e)
        {
            SortDevelopers(1, SortOrder.Descending);
        }

        private void nameascendingPub_Click(object sender, EventArgs e)
        {
            SortPublishers(0, SortOrder.Ascending);
        }

        private void namedescendingPub_Click(object sender, EventArgs e)
        {
            SortPublishers(0, SortOrder.Descending);
        }

        private void countascendingPub_Click(object sender, EventArgs e)
        {
            SortPublishers(1, SortOrder.Ascending);
        }

        private void countdescendingPub_Click(object sender, EventArgs e)
        {
            SortPublishers(1, SortOrder.Descending);
        }
    }
}