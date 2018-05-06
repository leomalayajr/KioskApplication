using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KioskApplication
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        public void tapBeginTransaction(object sender, EventArgs e)
        {
            // 1st Page functionailty going to 2nd page
            panel3.Visible = true;
        }

        public void student_button(object sender, EventArgs e)
        {
            // Student Button
            label5.Text = "Please provide your ID number.";
            panel4.Visible = true;
        }

        public void guest_button(object sender, EventArgs e)
        {
            // Guest Button
            label5.Text = "Please provide your name.";
            panel4.Visible = true;
        }

        public Boolean checkInfoField()
        {
            bool check;
            if (textBox1.Text != "" && textBox1.Text.Trim() != "")
            {
                check = false;
            }else
            {
                check = true;
            }

            return check;
        }

        public void next_button(object sender, EventArgs e)
        {
            // Next Button
            if (checkInfoField())
            {
                MessageBox.Show("Please provide your information!");
            }else
            {
                // Show next Panel / all transaction type.
                panel5.Visible = true;
            }
        }

        public void up_button(object sender, EventArgs e)
        {
            // Up Button
        }

        public void down_button(object sender, EventArgs e)
        {
            // Down Button
        }

        public void pick_button(object sender, EventArgs e)
        {
            // Pick Button
            panel6.Visible = true;
        }

        public void rate_button(object sender, EventArgs e)
        {
            // Show rate page here
            new Rate().Show();
        }

        public void resetFields()
        {
            textBox1.Text = "";
        }

        private void button8_Click(object sender, EventArgs e)
        {
            resetFields();
            panel6.Visible = false;
            panel5.Visible = false;
            panel4.Visible = false;
            panel3.Visible = false;
        }
    }
}
