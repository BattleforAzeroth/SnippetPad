namespace SnippetPad.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel flowGroups;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.CheckBox chkTopMost;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            flowGroups = new System.Windows.Forms.FlowLayoutPanel();
            btnReload = new System.Windows.Forms.Button();
            chkTopMost = new System.Windows.Forms.CheckBox();
            lblStatus = new System.Windows.Forms.Label();

            SuspendLayout();

            // flowGroups
            flowGroups.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom |
                                System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            flowGroups.AutoScroll = true;
            flowGroups.Location = new System.Drawing.Point(10, 50);
            flowGroups.Name = "flowGroups";
            flowGroups.Size = new System.Drawing.Size(270, 700);
            // 纵向栈：每行一个组容器
            flowGroups.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            flowGroups.WrapContents = false;
            flowGroups.Padding = new System.Windows.Forms.Padding(0);
            flowGroups.Margin = new System.Windows.Forms.Padding(0);

            // btnReload
            btnReload.Location = new System.Drawing.Point(10, 12);
            btnReload.Name = "btnReload";
            btnReload.Size = new System.Drawing.Size(50, 28);
            btnReload.Text = "刷新";
            btnReload.UseVisualStyleBackColor = true;
            btnReload.Click += (s, e) => ReloadConfig();

            // chkTopMost
            chkTopMost.AutoSize = true;
            chkTopMost.Location = new System.Drawing.Point(70, 16);
            chkTopMost.Name = "chkTopMost";
            chkTopMost.Size = new System.Drawing.Size(51, 21);
            chkTopMost.Text = "置顶";
            chkTopMost.Checked = true;
            chkTopMost.CheckedChanged += (s, e) => TopMost = chkTopMost.Checked;

            // lblStatus（内部状态提示，不用消息气泡）
            lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblStatus.AutoSize = false;
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblStatus.ForeColor = System.Drawing.Color.DimGray;
            lblStatus.Location = new System.Drawing.Point(120, 14);
            lblStatus.Size = new System.Drawing.Size(200, 28);
            lblStatus.Text = ""; // 运行时设置

            // MainForm
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(280, 750); // 背景更窄
            Controls.Add(flowGroups);
            Controls.Add(btnReload);
            Controls.Add(chkTopMost);
            Controls.Add(lblStatus);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "SnippetPad";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}