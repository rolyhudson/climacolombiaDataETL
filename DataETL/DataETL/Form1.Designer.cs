namespace DataETL
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.button11 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(97, 63);
            this.button1.TabIndex = 0;
            this.button1.Text = "Load Data";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(12, 406);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(97, 63);
            this.button2.TabIndex = 1;
            this.button2.Text = "SQL connect";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(115, 12);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(155, 63);
            this.button3.TabIndex = 2;
            this.button3.Text = "Split Data to Station Variable";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.splitData);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(379, 12);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(97, 63);
            this.button4.TabIndex = 3;
            this.button4.Text = "annual summary";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.summary);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(276, 12);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(97, 63);
            this.button5.TabIndex = 4;
            this.button5.Text = "load stations";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.loadStations);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(115, 406);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(97, 63);
            this.button6.TabIndex = 5;
            this.button6.Text = "clean up";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.removePACollections);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(379, 81);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(97, 63);
            this.button7.TabIndex = 6;
            this.button7.Text = "output annual summary";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.outputAnnualSummary);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(482, 12);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(97, 63);
            this.button8.TabIndex = 7;
            this.button8.Text = "monthly summary";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.monthlysummary);
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(482, 81);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(97, 63);
            this.button9.TabIndex = 8;
            this.button9.Text = "monthly graphs";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.monthlygraphs);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(12, 192);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(97, 63);
            this.button10.TabIndex = 9;
            this.button10.Text = "set groups";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.setGroups);
            // 
            // button11
            // 
            this.button11.Location = new System.Drawing.Point(482, 150);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(97, 63);
            this.button11.TabIndex = 10;
            this.button11.Text = "grouped monthly graphs";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.groupedmonthlygraphs);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(705, 481);
            this.Controls.Add(this.button11);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button11;
    }
}

