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
    public partial class Rate : Form
    {
        public Rate()
        {
            InitializeComponent();
        }
        
        public void rate_option_button(object sender, EventArgs e)
        {
            // get rate option from user.
            this.Hide();

        }
    }
}
