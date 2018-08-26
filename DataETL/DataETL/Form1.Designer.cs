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
            this.button12 = new System.Windows.Forms.Button();
            this.button13 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button15 = new System.Windows.Forms.Button();
            this.button16 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.button14 = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.button18 = new System.Windows.Forms.Button();
            this.button17 = new System.Windows.Forms.Button();
            this.button19 = new System.Windows.Forms.Button();
            this.button20 = new System.Windows.Forms.Button();
            this.button21 = new System.Windows.Forms.Button();
            this.button22 = new System.Windows.Forms.Button();
            this.button23 = new System.Windows.Forms.Button();
            this.button24 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(4, 18);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(73, 51);
            this.button1.TabIndex = 0;
            this.button1.Text = "Load Data";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(416, 330);
            this.button2.Margin = new System.Windows.Forms.Padding(2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(73, 51);
            this.button2.TabIndex = 1;
            this.button2.Text = "SQL connect";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(4, 74);
            this.button3.Margin = new System.Windows.Forms.Padding(2);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(73, 58);
            this.button3.TabIndex = 2;
            this.button3.Text = "Split Data to Station Variable";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.splitData);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(4, 17);
            this.button4.Margin = new System.Windows.Forms.Padding(2);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(141, 51);
            this.button4.TabIndex = 3;
            this.button4.Text = "annual summary";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.summary);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(4, 136);
            this.button5.Margin = new System.Windows.Forms.Padding(2);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(73, 51);
            this.button5.TabIndex = 4;
            this.button5.Text = "load stations";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.loadStations);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(493, 330);
            this.button6.Margin = new System.Windows.Forms.Padding(2);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(73, 51);
            this.button6.TabIndex = 5;
            this.button6.Text = "clean up";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.removePACollections);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(4, 73);
            this.button7.Margin = new System.Windows.Forms.Padding(2);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(141, 51);
            this.button7.TabIndex = 6;
            this.button7.Text = "output annual summary";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.outputAnnualSummary);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(4, 17);
            this.button8.Margin = new System.Windows.Forms.Padding(2);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(141, 51);
            this.button8.TabIndex = 7;
            this.button8.Text = "monthly summary";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.monthlysummary);
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(4, 76);
            this.button9.Margin = new System.Windows.Forms.Padding(2);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(141, 51);
            this.button9.TabIndex = 8;
            this.button9.Text = "monthly graphs";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.monthlygraphs);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(4, 305);
            this.button10.Margin = new System.Windows.Forms.Padding(2);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(73, 51);
            this.button10.TabIndex = 9;
            this.button10.Text = "set groups";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.setGroups);
            // 
            // button11
            // 
            this.button11.Location = new System.Drawing.Point(4, 73);
            this.button11.Margin = new System.Windows.Forms.Padding(2);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(73, 51);
            this.button11.TabIndex = 10;
            this.button11.Text = "grouped monthly graphs";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.groupedmonthlygraphs);
            // 
            // button12
            // 
            this.button12.Location = new System.Drawing.Point(4, 17);
            this.button12.Margin = new System.Windows.Forms.Padding(2);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(73, 51);
            this.button12.TabIndex = 11;
            this.button12.Text = "convert 10 minute readings";
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.convertTenMinuteCollections);
            // 
            // button13
            // 
            this.button13.Location = new System.Drawing.Point(416, 274);
            this.button13.Margin = new System.Windows.Forms.Padding(2);
            this.button13.Name = "button13";
            this.button13.Size = new System.Drawing.Size(73, 51);
            this.button13.TabIndex = 12;
            this.button13.Text = "synthetic year";
            this.button13.UseVisualStyleBackColor = true;
            this.button13.Click += new System.EventHandler(this.syntheticYear);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button10);
            this.groupBox1.Controls.Add(this.button15);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.button3);
            this.groupBox1.Controls.Add(this.button16);
            this.groupBox1.Controls.Add(this.button5);
            this.groupBox1.Location = new System.Drawing.Point(9, 10);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(85, 364);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Load data";
            // 
            // button15
            // 
            this.button15.Location = new System.Drawing.Point(4, 193);
            this.button15.Margin = new System.Windows.Forms.Padding(2);
            this.button15.Name = "button15";
            this.button15.Size = new System.Drawing.Size(73, 51);
            this.button15.TabIndex = 5;
            this.button15.Text = "add indexes to stations";
            this.button15.UseVisualStyleBackColor = true;
            this.button15.Click += new System.EventHandler(this.addIndexes);
            // 
            // button16
            // 
            this.button16.Location = new System.Drawing.Point(4, 249);
            this.button16.Margin = new System.Windows.Forms.Padding(2);
            this.button16.Name = "button16";
            this.button16.Size = new System.Drawing.Size(73, 51);
            this.button16.TabIndex = 19;
            this.button16.Text = "add variable meta";
            this.button16.UseVisualStyleBackColor = true;
            this.button16.Click += new System.EventHandler(this.addVariables);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.button4);
            this.groupBox2.Controls.Add(this.button7);
            this.groupBox2.Location = new System.Drawing.Point(101, 10);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox2.Size = new System.Drawing.Size(150, 132);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Numeric annual summary";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.button9);
            this.groupBox3.Controls.Add(this.button8);
            this.groupBox3.Location = new System.Drawing.Point(256, 10);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox3.Size = new System.Drawing.Size(150, 132);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Monthly graphic summary";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.button11);
            this.groupBox4.Location = new System.Drawing.Point(411, 10);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox4.Size = new System.Drawing.Size(82, 132);
            this.groupBox4.TabIndex = 16;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "grouping";
            // 
            // button14
            // 
            this.button14.Location = new System.Drawing.Point(4, 73);
            this.button14.Margin = new System.Windows.Forms.Padding(2);
            this.button14.Name = "button14";
            this.button14.Size = new System.Drawing.Size(73, 51);
            this.button14.TabIndex = 17;
            this.button14.Text = "remove values outside range";
            this.button14.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.button24);
            this.groupBox5.Controls.Add(this.button18);
            this.groupBox5.Controls.Add(this.button17);
            this.groupBox5.Controls.Add(this.button12);
            this.groupBox5.Controls.Add(this.button14);
            this.groupBox5.Location = new System.Drawing.Point(101, 178);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox5.Size = new System.Drawing.Size(240, 132);
            this.groupBox5.TabIndex = 18;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "cleaning";
            // 
            // button18
            // 
            this.button18.Location = new System.Drawing.Point(82, 73);
            this.button18.Margin = new System.Windows.Forms.Padding(2);
            this.button18.Name = "button18";
            this.button18.Size = new System.Drawing.Size(73, 51);
            this.button18.TabIndex = 19;
            this.button18.Text = "check indexes on clean collections";
            this.button18.UseVisualStyleBackColor = true;
            this.button18.Click += new System.EventHandler(this.checkIndexes);
            // 
            // button17
            // 
            this.button17.Location = new System.Drawing.Point(82, 17);
            this.button17.Margin = new System.Windows.Forms.Padding(2);
            this.button17.Name = "button17";
            this.button17.Size = new System.Drawing.Size(73, 51);
            this.button17.TabIndex = 18;
            this.button17.Text = "check 10 minute readings";
            this.button17.UseVisualStyleBackColor = true;
            this.button17.Click += new System.EventHandler(this.checkTenMinAverages);
            // 
            // button19
            // 
            this.button19.Location = new System.Drawing.Point(493, 274);
            this.button19.Margin = new System.Windows.Forms.Padding(2);
            this.button19.Name = "button19";
            this.button19.Size = new System.Drawing.Size(73, 51);
            this.button19.TabIndex = 20;
            this.button19.Text = "read synthetic year";
            this.button19.UseVisualStyleBackColor = true;
            this.button19.Click += new System.EventHandler(this.readSyntheticYear);
            // 
            // button20
            // 
            this.button20.Location = new System.Drawing.Point(106, 314);
            this.button20.Margin = new System.Windows.Forms.Padding(2);
            this.button20.Name = "button20";
            this.button20.Size = new System.Drawing.Size(73, 51);
            this.button20.TabIndex = 21;
            this.button20.Text = "daily scatter plots";
            this.button20.UseVisualStyleBackColor = true;
            this.button20.Click += new System.EventHandler(this.dailyScatter);
            // 
            // button21
            // 
            this.button21.Location = new System.Drawing.Point(183, 314);
            this.button21.Margin = new System.Windows.Forms.Padding(2);
            this.button21.Name = "button21";
            this.button21.Size = new System.Drawing.Size(73, 51);
            this.button21.TabIndex = 22;
            this.button21.Text = "daily scatter plots TS RS";
            this.button21.UseVisualStyleBackColor = true;
            this.button21.Click += new System.EventHandler(this.dailyScatterTSRS);
            // 
            // button22
            // 
            this.button22.Location = new System.Drawing.Point(268, 314);
            this.button22.Margin = new System.Windows.Forms.Padding(2);
            this.button22.Name = "button22";
            this.button22.Size = new System.Drawing.Size(73, 51);
            this.button22.TabIndex = 23;
            this.button22.Text = "daily scatter plots Monthly";
            this.button22.UseVisualStyleBackColor = true;
            this.button22.Click += new System.EventHandler(this.dailyScatterMonthly);
            // 
            // button23
            // 
            this.button23.Location = new System.Drawing.Point(497, 84);
            this.button23.Margin = new System.Windows.Forms.Padding(2);
            this.button23.Name = "button23";
            this.button23.Size = new System.Drawing.Size(73, 51);
            this.button23.TabIndex = 11;
            this.button23.Text = "city meta data";
            this.button23.UseVisualStyleBackColor = true;
            this.button23.Click += new System.EventHandler(this.printcitymeta);
            // 
            // button24
            // 
            this.button24.Location = new System.Drawing.Point(159, 17);
            this.button24.Margin = new System.Windows.Forms.Padding(2);
            this.button24.Name = "button24";
            this.button24.Size = new System.Drawing.Size(73, 51);
            this.button24.TabIndex = 20;
            this.button24.Text = "cloud cleaner";
            this.button24.UseVisualStyleBackColor = true;
            this.button24.Click += new System.EventHandler(this.cloudClean);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(605, 391);
            this.Controls.Add(this.button23);
            this.Controls.Add(this.button22);
            this.Controls.Add(this.button21);
            this.Controls.Add(this.button20);
            this.Controls.Add(this.button19);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button13);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button2);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
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
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button button14;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button button15;
        private System.Windows.Forms.Button button16;
        private System.Windows.Forms.Button button17;
        private System.Windows.Forms.Button button18;
        private System.Windows.Forms.Button button19;
        private System.Windows.Forms.Button button20;
        private System.Windows.Forms.Button button21;
        private System.Windows.Forms.Button button22;
        private System.Windows.Forms.Button button23;
        private System.Windows.Forms.Button button24;
    }
}

