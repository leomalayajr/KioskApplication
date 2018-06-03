using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KioskApplication
{
    public partial class Form2 : Form
    {
        private String connection_string = System.Configuration.ConfigurationManager.ConnectionStrings["dbString"].ConnectionString;
        public Form2()
        {
            InitializeComponent();
            generateItems();
        }
        private List<_Servicing_Office> LIST_getServicingOffices()
        {

            List<_Servicing_Office> dataSource = new List<_Servicing_Office>();
            // List possible Servicing Offices
            SqlConnection con = new SqlConnection(connection_string);
            string retrieve_servicing_offices = "select * from Servicing_Office";
            SqlDataReader _rdr;
            SqlCommand __cmd = new SqlCommand(retrieve_servicing_offices, con);

            try
            {
                con.Open();
                _rdr = __cmd.ExecuteReader();
                while (_rdr.Read())
                {
                    dataSource.Add(new _Servicing_Office()
                    {
                        Name = (string)_rdr["Name"],
                        Address = (string)_rdr["Address"],
                        id = (int)_rdr["ID"]
                    });
                    Console.WriteLine((string)_rdr["Name"]);
                }
                con.Close();
            }
            catch (SqlException)
            {
                MessageBox.Show("Can't connect to local DB!");
                Environment.Exit(0);
            }
            return dataSource;
        }
        public void generateItems()
        {
            comboBox1.DataSource = LIST_getServicingOffices();
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "id";
        }
        public void pictureHover(object sender, EventArgs e)
        {
            if(((PictureBox)sender).Name == "pictureBox1")
            {
                pictureBox2.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox3.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox4.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox5.Image = KioskApplication.Properties.Resources.Star_nf;
                ((PictureBox)sender).Image = KioskApplication.Properties.Resources.Star_f;

            }else if (((PictureBox)sender).Name == "pictureBox2")
            {
                pictureBox3.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox4.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox5.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox1.Image = KioskApplication.Properties.Resources.Star_f;
                ((PictureBox)sender).Image = KioskApplication.Properties.Resources.Star_f;

            }
            else if (((PictureBox)sender).Name == "pictureBox3")
            {
                pictureBox4.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox5.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox1.Image = KioskApplication.Properties.Resources.Star_f;
                pictureBox2.Image = KioskApplication.Properties.Resources.Star_f;
                ((PictureBox)sender).Image = KioskApplication.Properties.Resources.Star_f;

            }
            else if (((PictureBox)sender).Name == "pictureBox4")
            {
                pictureBox5.Image = KioskApplication.Properties.Resources.Star_nf;
                pictureBox1.Image = KioskApplication.Properties.Resources.Star_f;
                pictureBox2.Image = KioskApplication.Properties.Resources.Star_f;
                pictureBox3.Image = KioskApplication.Properties.Resources.Star_f;
                ((PictureBox)sender).Image = KioskApplication.Properties.Resources.Star_f;

            }
            else
            {
                pictureBox1.Image = KioskApplication.Properties.Resources.Star_f;
                pictureBox2.Image = KioskApplication.Properties.Resources.Star_f;
                pictureBox3.Image = KioskApplication.Properties.Resources.Star_f;
                pictureBox4.Image = KioskApplication.Properties.Resources.Star_f;
                ((PictureBox)sender).Image = KioskApplication.Properties.Resources.Star_f;

            }
        }

        public void pictureOutHover(object sender, EventArgs e)
        {
            pictureBox1.Image = KioskApplication.Properties.Resources.Star_nf;
            pictureBox2.Image = KioskApplication.Properties.Resources.Star_nf;
            pictureBox3.Image = KioskApplication.Properties.Resources.Star_nf;
            pictureBox4.Image = KioskApplication.Properties.Resources.Star_nf;
            pictureBox5.Image = KioskApplication.Properties.Resources.Star_nf;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            giveRating(1);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            giveRating(2);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            giveRating(3);
        }
        private void giveRating(int rate)
        {
            try
            {
                SqlConnection con = new SqlConnection(connection_string);
                string query = "update Rating_Office set Score = @param_score, isGiven = 1 where Customer_Queue_Number = @param_cqn and isGiven = 0 and Servicing_Office = @param_selected_so";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@param_score", rate);
                cmd.Parameters.AddWithValue("@param_cqn", textBox1.Text);
                cmd.Parameters.AddWithValue("@param_selected_so", (int)comboBox1.SelectedValue);
                con.Open();
                int b = cmd.ExecuteNonQuery();
                textBox1.Clear();
                if (b == 0)
                    MessageBox.Show("You were not served or you already gave your rating on that office.", "Unable to give rating");
                else
                    MessageBox.Show("Thank you for the evaluation. This will help us know how to serve you better.", "Evaluation submitted");
                con.Close();
            }
            catch (SqlException a) { Console.WriteLine("Could not process your request. Problem with local connection."); }
            catch (NullReferenceException b) { Console.WriteLine("Something went wrong. Please ask an admin to restart this kiosk."); }
            generateItems();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            giveRating(4);
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            giveRating(5);
        }
    }
}
