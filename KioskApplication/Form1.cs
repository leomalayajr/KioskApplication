using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using KioskApplication.Properties;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using iTextSharp.text.pdf.parser;


namespace KioskApplication
{
    public partial class Form1 : Form
    {
        #region APP INIT VARIABLES
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private Dictionary<int, string> transaction_type = new Dictionary<int, string>();
        private int location = 0;
        public static double time = 0;
        private bool qHided, gHided;
        private DateTime thisday = DateTime.Today;
        public static int Servicing_Office = 1;
        private String connection_string = System.Configuration.ConfigurationManager.ConnectionStrings["dbString"].ConnectionString;
        internal static int newID;
        internal static int shownID;
        private int tickTime;
        private Label[] sb = new Label[7];
        private Label[] qb = new Label[7];

        DataTable table_Transactions;
        DataTable table_Transaction_Table;
        DataTable table_Servicing_Office;
        System.Timers.Timer timer10 = new System.Timers.Timer();

        public string PROGRAM_Name = string.Empty;
        public bool PROGRAM_Guest = true;
        public string PROGRAM_Student_No = string.Empty;

        int counter = 0;

        //Transaction Type counter
        int transCounter = 0;
        int tempTransCounter = 0;
        int Height = 0;
        int Width = 0;

        //Getting idForPrint
        int transactionID;
        float addHeight = 0;

        //George Here
        Array[] offices;
        #endregion

        public Form1()
        {
            InitializeComponent();
            flowLayoutPanel1.AutoScrollPosition = new Point(0, 0);
            autoGenerateButton();
            timer2.Enabled = true;
            timer2.Start();
            timer1.Enabled = true;
            timer1.Start();

            // from queue app on import 
            checkIfOnline();
            newID = 0;
            shownID = 0;
            timer10.Start();
            table_Transactions = getTransactionList();
            table_Transaction_Table = getTransactionType();
            table_Servicing_Office = getServicingOffice();

        }
        #region MAIN METHODS
        private void checkIfOnline()
        {
            SqlConnection con = new SqlConnection(connection_string);
            string query = "select id from Queue_Info";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            var b = cmd.ExecuteScalar();
            if (b == null)
            {
                MessageBox.Show("Please initialize the queuing system first.", "Critical Error!");
                Environment.Exit(0);
            }
            con.Close();
        }
        private void createNewRating(string CQN, bool isStudent, int Transaction_ID, int Servicing_Office, SqlConnection con)
        {
            String QUERY_create_RatingOffice_onNext = "insert into Rating_Office (Customer_Queue_Number,isStudent,Score,isGiven,Transaction_ID,Servicing_Office) " +
                    " values " +
                    " (@param_CQN,@param_isStudent,0,0,@param_tt_ID,@param_so)";
            SqlCommand cmd = new SqlCommand(QUERY_create_RatingOffice_onNext, con);
            cmd.Parameters.AddWithValue("@param_CQN",CQN);
            cmd.Parameters.AddWithValue("@param_IsStudent", isStudent);
            cmd.Parameters.AddWithValue("@param_tt_ID", Transaction_ID);
            cmd.Parameters.AddWithValue("@param_so", Servicing_Office);
            cmd.ExecuteNonQuery();
        }
        private void studentSubmit(int myKey)
        {
            SqlConnection con = new SqlConnection(connection_string);
            using (con)
            {
                con.Open();
                SqlCommand cmd2 = con.CreateCommand();
                cmd2.CommandType = CommandType.Text;


                // NOTE : THIS IS BARELY UPDATED.
                // Recent update : March 27, 2018
                // UPDATE immediately after receiving student database from sir
                String query2 = "insert into Main_Queue (Queue_Number,Full_Name,Servicing_Office,Student_No,Transaction_Type,Type,Time,Pattern_Current,Pattern_Max,Customer_Queue_Number,Queue_Status,Customer_From) OUTPUT Inserted.id";
                query2 += " values (@q_qn,@q_fn,@q_so,@q_sn,@q_tt,1,GETDATE(),@q_pc,@q_pm,@q_cqn,@q_qs,@q_cf)";

                
                    int _tt_id = myKey;
                    int _f_so = getFirstServicingOffice(_tt_id);
                    int c = getQueueNumber(con, _f_so);
                    string gqsn = generateQueueShortName(_tt_id, c);
                

                cmd2 = new SqlCommand(query2, con);
                cmd2.Parameters.AddWithValue("@q_qn", c);
                cmd2.Parameters.AddWithValue("@q_fn", PROGRAM_Name);
                cmd2.Parameters.AddWithValue("@q_so", _f_so); // 02 - 01 - 18 -- insert the first servicing office!
                cmd2.Parameters.AddWithValue("@q_sn", PROGRAM_Student_No);
                cmd2.Parameters.AddWithValue("@q_tt", _tt_id); // Note -> this was changed on March 2, 2018
                cmd2.Parameters.AddWithValue("@q_pc", 1);
                cmd2.Parameters.AddWithValue("@q_pm", retrievePatternMax(_tt_id));
                cmd2.Parameters.AddWithValue("@q_cqn", gqsn);
                cmd2.Parameters.AddWithValue("@q_qs", "Waiting");
                cmd2.Parameters.AddWithValue("@q_cf", 0);
                Console.Write("--INSERTING TO Main_Queue--");
                newID = (int)cmd2.ExecuteScalar();
                //setQueueTicket function here -- uncomment after student db received
                setQueueTicket(con, _tt_id, _f_so, PROGRAM_Student_No, gqsn, c);

                createNewRating(gqsn, true, _tt_id, _f_so, con);

                shownID = c;
                //new_transaction_queue(con, _tt_id);


                
                // George Here | Inserting Data to Logs...
                String logQuery = "insert into Controller_Queue_Log (Log_Title, Log_Text) values (@param_type, @param_SO_NAME)";

                SqlCommand cmd3 = new SqlCommand(logQuery, con);
                cmd3.Parameters.AddWithValue("@param_type", "Kiosk");
                cmd3.Parameters.AddWithValue("@param_SO_NAME", PROGRAM_Name + " joined the queue with transaction "+getTransactionName(_tt_id)+".");
                cmd3.ExecuteNonQuery();
                //upto here

                time = 0;
                con.Close();

                textBox1.Clear();

                // timer2.Start();
                Console.Write("--INSERTING TO Main_Queue--");
            }
        }
        private string getTransactionName(int _tt_id)
        {
            string transaction_name = "";
            foreach (DataRow row in table_Transaction_Table.Rows)
            {
                int temp_id = (int)row["id"];
                if (_tt_id == temp_id)
                {
                    transaction_name = (string)row["Transaction_Name"];
                    break;
                }
            }
            return transaction_name;
        }

        private string getServicingOfficeNameLog(int _so)
        {
            string servicing_office_Name = "";
            foreach (DataRow row in table_Servicing_Office.Rows)
            {
                int temp_id = (int)row["id"];
                if (_so == temp_id)
                {
                    servicing_office_Name = (string)row["Name"];
                    break;
                }
            }
            return servicing_office_Name;
        }

        private void guestSubmit(int myKey)
        {
            counter++;
            SqlConnection con = new SqlConnection(connection_string);
            using (con)
            {

                con.Open();

                SqlCommand cmd = con.CreateCommand();
                SqlCommand cmd2 = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd2.CommandType = CommandType.Text;
                // Name -> textBox1.Text
                String query2 = "insert into Main_Queue (Queue_Number,Full_Name,Servicing_Office,Student_No,Transaction_Type,Type,Time,Pattern_Current,Pattern_Max,Customer_Queue_Number,Queue_Status,Customer_From) OUTPUT Inserted.id";
                query2 += " values (@q_qn,@q_fn,@q_so,@q_sn,@q_tt,1,GETDATE(),@q_pc,@q_pm,@q_cqn,@q_qs,@q_cf)";

                int _tt_id = myKey;
                int _f_so = getFirstServicingOffice(_tt_id);
                int c = getQueueNumber(con, _f_so);
                string gqsn = generateQueueShortName(_tt_id, c);

                // Values chart
                /*
                 * CUSTOMER_INSERTED_VALUES
                 * Transaction_Type (the ID)
                 * Name (for guest), Student_ID (for students)
                 * 
                 * PROGRAM_GENERATED_VALUES
                 * Queue Number

                 */
                cmd2 = new SqlCommand(query2, con);
                cmd2.Parameters.AddWithValue("@q_qn", c);
                cmd2.Parameters.AddWithValue("@q_fn", textBox1.Text);
                cmd2.Parameters.AddWithValue("@q_so", _f_so);
                cmd2.Parameters.AddWithValue("@q_sn", "N/A");
                cmd2.Parameters.AddWithValue("@q_tt", _tt_id);
                cmd2.Parameters.AddWithValue("@q_pc", 1);
                cmd2.Parameters.AddWithValue("@q_pm", retrievePatternMax(_tt_id));
                cmd2.Parameters.AddWithValue("@q_cqn", gqsn);
                cmd2.Parameters.AddWithValue("@q_qs", "Waiting");
                cmd2.Parameters.AddWithValue("@q_cf", 0);
                Console.Write("--INSERTING TO Main_Queue--");
                newID = (int)cmd2.ExecuteScalar();

                setQueueTicket(con, _tt_id, _f_so, "Guest", gqsn, c);
                
                createNewRating(gqsn, false, _tt_id, _f_so, con);

                shownID = c;
                //new_transaction_queue(con, _tt_id);
                String logQuery = "insert into Controller_Queue_Log (Log_Title, Log_Text) values (@param_type, @param_SO_NAME)";

                SqlCommand cmd3 = new SqlCommand(logQuery, con);
                cmd3.Parameters.AddWithValue("@param_type", "Kiosk");
                cmd3.Parameters.AddWithValue("@param_SO_NAME", PROGRAM_Name + " joined the queue with transaction " + getTransactionName(_tt_id)+ ".");
                cmd3.ExecuteNonQuery();
                time = 0;
                con.Close();

                //Clear Value
                textBox1.Clear();


            }
            Console.WriteLine(counter);
        }
        #endregion
        #region HELPING METHODS

        private void timeUpdate()
        {
            int x = 0;

            label4.Text = DateTime.Now.ToString("h:m:s tt");
            //label27.Text = tickTime.ToString();
            tickTime++;
            if (tickTime == 50)
            {
                SqlConnection con = new SqlConnection(connection_string);
                tickTime = 0;
                x = 0;
                using (con)
                {
                    con.Open();
                    SqlCommand cmd = con.CreateCommand();
                    SqlCommand cmd2 = con.CreateCommand();
                    cmd.CommandType = CommandType.Text;
                    cmd2.CommandType = CommandType.Text;
                    String query = "select id,Queue_Number,Type,Student_No from (select TOP 7 id,Queue_Number, Type, Student_No from Main_Queue where Servicing_Office = @Servicing_Office order by id desc) temp_n order by id asc";

                    cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Servicing_Office", Servicing_Office);
                    SqlDataReader rdr;
                    rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        // get the results of each column
                        // string id = (string)rdr["id"];
                        string qn = rdr["Queue_Number"].ToString();
                        string type = ((Boolean)rdr["Type"] == false) ? "Student" : "Guest";
                        string s_id = (string)rdr["Student_No"];

                        qb[x].Text = qn;
                        if (type == "Student") { sb[x].Text = s_id; }
                        else { sb[x].Text = type; }
                        x++;

                    }

                    //cmd3.CommandText = "return_total_queue";
                    //cmd3.CommandType = CommandType.StoredProcedure;
                    //cmd3.Parameters.AddWithValue("ServicingOffice", Servicing_Office);
                    //cmd3.Connection = con;
                    //rdr2 = cmd3.ExecuteReader();
                    //while (rdr2.Read()) { label29.Text =  rdr2["a"].ToString();}
                    //label29.Text = return_on_queue(con).ToString(); important

                }
                con.Close();



            }
        }

        private void setQueueTicket(SqlConnection con, int _transaction_type, int _ServicingOffice, string _student_no, string _cqn, int _queue_number)
        {
            string _so_name = getServicingOfficeName(_ServicingOffice);
            foreach (DataRow row in table_Transactions.Rows)
            {
                if (_transaction_type == (int)row["Transaction_ID"])
                {
                    string QUERY_Count_ServingTime = "select count(id) from Serving_Time where Servicing_Office = @param1";
                    SqlCommand CMD_Count_ServingTime = new SqlCommand(QUERY_Count_ServingTime, con);
                    CMD_Count_ServingTime.Parameters.AddWithValue("@param1", _ServicingOffice);
                    double temp_time = 0;
                    int time_count = (int)CMD_Count_ServingTime.ExecuteScalar();
                    Console.WriteLine("time counts->"+time_count);
                    Console.WriteLine("param1 -> " + _ServicingOffice);
                    if ( time_count > 4)
                    {
                        // Give customer estimated time
                        // Retrieve avg time first 
                        int turns_before_customer = 0;

                        string QUERY_Average_ServingTime = "select AVG(Duration_Seconds) from Serving_Time where " +
                                    "Servicing_Office = @param1";
                        SqlCommand CMD_Average_ServingTime = new SqlCommand(QUERY_Average_ServingTime, con);
                        CMD_Average_ServingTime.Parameters.AddWithValue("@param1", _ServicingOffice);

                        temp_time = (int)CMD_Average_ServingTime.ExecuteScalar();

                        // Retrieve how many turns before he will be served
                        string QUERY_Retrieve_BeforeCustomerTurn = "select TOP 1 Current_Number from Queue_Info where Servicing_Office = @param1";
                        SqlCommand CMD_Retrieve_BeforeCustomerTurn = new SqlCommand(QUERY_Retrieve_BeforeCustomerTurn, con);
                        CMD_Retrieve_BeforeCustomerTurn.Parameters.AddWithValue("@param1", _ServicingOffice);

                        turns_before_customer = (int)CMD_Retrieve_BeforeCustomerTurn.ExecuteScalar();

                        turns_before_customer = _queue_number - turns_before_customer;
                        temp_time = temp_time * turns_before_customer;


                        label11.Text = "About to be served at " + _format_estimated_time(temp_time);
                        time += temp_time;
                    }
                    else
                    {
                        // Estimated time = N/A
                        label11.Text = "Not enough customers to estimate serving time.";
                    }

                }
            }
            label10.Text = PROGRAM_Name;
            label8.Text = _cqn;
            //num2Counter.Text = _so_name;
            // important
        }
        private string _format_estimated_time(double time)
        {
            string a = "";
            var minutes = Math.Floor((time / 60));
            var seconds = time - minutes * 60;
            if (minutes > 0)
            {
                if (minutes == 1)
                {
                    a = minutes + " minute and ";
                }
                else
                {
                    a = minutes + " minutes and ";
                }
            }
            if (seconds >= 2)
                a += seconds + " seconds";
            else
                a += seconds + " second";

            return a;
        }
        private int return_transaction_type_offices_count(SqlConnection con, int q_tt)
        {
            String query6 = "select Pattern_Max from Transaction_Type where id = @q_tt";
            SqlCommand cmd6 = new SqlCommand(query6, con);
            cmd6.Parameters.AddWithValue("@q_tt", q_tt);
            int c = 0;
            SqlDataReader rdr3;
            cmd6.Parameters.AddWithValue("@sn", Servicing_Office);
            rdr3 = cmd6.ExecuteReader();
            while (rdr3.Read()) { c = (int)rdr3["Pattern_Max"]; }
            return c;
        }
        private string getEstimatedTime(int servicing_office, SqlConnection con)
        {
            string _time = "";

            string _query = "select TOP 1 Avg_Serving_Time from Queue_Info where Servicing_Office = @param1";
            SqlCommand _cmd = new SqlCommand(_query, con);
            _cmd.Parameters.AddWithValue("@param1", servicing_office);
            int _seconds = (int)_cmd.ExecuteScalar();

            return _time;

        }
        private void new_transaction_queue(SqlConnection con, int q_tt)
        {
            String query5 = "insert into Queue_Transaction (Main_Queue_ID,Pattern_No,Servicing_Office) values (@q_mid,@q_pn,@sn)";
            SqlCommand cmd5 = new SqlCommand(query5, con);

            //loop -- how many servicing offices
            int c_pattern_no = 0;
            int c_servicing_office = 0;
            int temp_pattern_no = 0;
            int temp_transaction_id = 0;
            //find servicing office based on pattern number

            for (int x = 0; x < (return_transaction_type_offices_count(con, q_tt)); x++)
            {
                c_pattern_no++;
                foreach (DataRow row in table_Transactions.Rows)
                {
                    temp_pattern_no = (int)row["Pattern_No"];
                    temp_transaction_id = (int)row["Transaction_ID"];
                    Console.Write(" searching for the servicing office based on pattern number");
                    if (q_tt == temp_transaction_id && temp_pattern_no == c_pattern_no)
                    {
                        c_servicing_office = (int)row["Servicing_Office"];
                        break;
                    }
                }
                cmd5.Parameters.AddWithValue("@q_mid", newID);
                cmd5.Parameters.AddWithValue("@sn", c_servicing_office);
                cmd5.Parameters.AddWithValue("@q_pn", c_pattern_no);
                cmd5.ExecuteNonQuery();
                cmd5.Parameters.Clear();

                // Inserting data to firebase -> Queue_Transaction // to let the user know where he is
                // FirebaseFunction: Kiosk_Insert_QueueTransaction(newID,c_servicing_office,c_pattern_no);

            }


        }
        private int return_on_queue(SqlConnection con)
        {
            int a = 0;
            String query4 = "select count(*) as a from Main_Queue where Servicing_Office = @sn and Queue_Status = @qs";
            SqlCommand cmd3 = new SqlCommand(query4, con);
            SqlDataReader rdr2;
            cmd3.Parameters.AddWithValue("@sn", Servicing_Office);
            cmd3.Parameters.AddWithValue("@qs", "Waiting");
            rdr2 = cmd3.ExecuteReader();
            while (rdr2.Read()) { a = (int)rdr2["a"]; }
            //execute return_total_queue
            return a;
        }
        private void incrementQueueNumber(SqlConnection con, int q_so)
        {
            int b = 0;
            // increment queue number 
            SqlCommand cmd4;
            String query2 = "update Queue_Info set Current_Queue = Current_Queue+1 OUTPUT Inserted.Current_Queue where Servicing_Office = @Servicing_Office";
            cmd4 = new SqlCommand(query2, con);
            cmd4.Parameters.AddWithValue("@Servicing_Office", q_so);
            try
            {
                b = (int)cmd4.ExecuteScalar();
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Information about Queue is not initialized yet. Please contact an admin!", "Critical Error");
                Environment.Exit(0);
            }

            // Update firebase -> Queue_Info > use the data processed and update the value
            // FirebaseFunction: Kiosk_Update_QueueInfo(b,q_so);
        }

        private void Queue_Info_Update()
        {
            //Checks whether Queue_Info is available.
            //Writes default data.
            //Always executed when a Kiosk have been opened.

            SqlConnection con = new SqlConnection(connection_string);
            using (con)
            {
                con.Open();
                SqlCommand cmd = con.CreateCommand();
                SqlCommand cmd2 = con.CreateCommand();
                SqlCommand cmd3 = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd2.CommandType = CommandType.Text;

                String query = "select * from Queue_Info where Servicing_Office = @Servicing_Office";
                String query2 = "";
                cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Servicing_Office", Servicing_Office);

                SqlDataReader rdr;
                rdr = cmd.ExecuteReader();
                int rowCount = 0;
                while (rdr.Read())
                { rowCount++; { if (rowCount > 0) { break; } } }
                if (rowCount > 0)
                {
                    string Current_Number = rdr["Current_Number"].ToString();
                    string Status = rdr["Status"].ToString();
                    //MessageBox.Show(Status+" already");
                }
                else
                {
                    foreach (DataRow a in table_Servicing_Office.Rows)
                    {
                        int _so = (int)a["id"];
                        query2 = "insert into Queue_Info (Current_Number,Current_Queue,Servicing_Office,Mode,Status,Counter,Office_Name) values (@cn,@cq,@so,@m,@sn,@c,@o_n)";
                        cmd2 = new SqlCommand(query2, con);
                        cmd2.Parameters.AddWithValue("@cn", 0);
                        cmd2.Parameters.AddWithValue("@cq", 1);
                        cmd2.Parameters.AddWithValue("@so", _so);
                        cmd2.Parameters.AddWithValue("@m", 1);
                        cmd2.Parameters.AddWithValue("@sn", "Online");
                        cmd2.Parameters.AddWithValue("@c", "0");
                        cmd2.Parameters.AddWithValue("@o_n", getServicingOfficeName(_so));
                        int result = cmd2.ExecuteNonQuery();
                        Console.WriteLine("Queue info inserting something...");
                        // Inserting data to firebase
                        // FirebaseFunction: Kiosk_Insert_QueueInfo(_so);
                    }
                }

            }
            con.Close();


        }
        #endregion

        #region GET METHODS
        private int getQueueNumber(SqlConnection con, int q_so)
        {
            // retrieves queue number
            int res = 0;

            SqlCommand cmd3;
            String query = "select Current_Queue from Queue_Info where Servicing_Office = @Servicing_Office";
            cmd3 = new SqlCommand(query, con);
            cmd3.Parameters.AddWithValue("@Servicing_Office", q_so);
            SqlDataReader rdr2;
            rdr2 = cmd3.ExecuteReader();
            while (rdr2.Read()) { res = (int)rdr2["Current_Queue"]; }
            Console.Write("--RETURNING-> getQueueNumber[" + res + "]");
            incrementQueueNumber(con, q_so);
            return res;
        }
        private string getServicingOfficeName(int _so)
        {
            string servicing_office_Name = "";
            foreach (DataRow row in table_Servicing_Office.Rows)
            {
                int temp_id = (int)row["id"];
                if (_so == temp_id)
                {
                    servicing_office_Name = (string)row["Name"];
                    break;
                }
            }
            return servicing_office_Name;
        }
        private DataTable getServicingOffice()
        {
            DataTable b = new DataTable();
            b.Columns.Add("id", typeof(int));
            b.Columns.Add("Name", typeof(string));
            b.Columns.Add("Address", typeof(string));
            SqlConnection con = new SqlConnection(connection_string);
            using (con)
            {
                con.Open();
                SqlCommand b_cmd = con.CreateCommand();
                SqlDataReader b_rdr;

                String b_q = "select * from Servicing_Office";
                b_cmd = new SqlCommand(b_q, con);

                b_rdr = b_cmd.ExecuteReader();
                while (b_rdr.Read())
                {
                    b.Rows.Add(
                       (int)b_rdr["id"],
                       (string)b_rdr["Name"],
                       (string)b_rdr["Address"]);
                    Console.Write(" RUNNING READS... ");
                }
                con.Close();
            }
            Console.Write(" \n RETURNING READS \n ");
            return b;
        }
        private int getFirstServicingOffice(int q_tt)
        {
            int a = 0;
            int temp_pattern_no = 0;
            int temp_transaction_id = 0;
            foreach (DataRow row in table_Transactions.Rows)
            {
                temp_pattern_no = (int)row["Pattern_No"];
                temp_transaction_id = (int)row["Transaction_ID"];
                Console.WriteLine(" retrieving the first servicing office ");
                if (q_tt == temp_transaction_id && temp_pattern_no == 1)
                {
                    a = (int)row["Servicing_Office"];
                    break;
                }
            }
            return a;
        }
        private int retrievePatternMax(int Transaction_Type)
        {
            int a = 0, id = 0;
            foreach (DataRow row in table_Transaction_Table.Rows)
            {
                id = (int)row["id"];
                Console.Write(" RetrievePatternMax -> searching for the respective pattern number ");
                if (id == Transaction_Type)
                {
                    a = (int)row["Pattern_Max"];
                    break;
                }
            }
            return a;
        }
        private string generateQueueShortName(int Transaction_Type, int queueNumber)
        {
            string short_name = "";
            int id = 0;
            foreach (DataRow row in table_Transaction_Table.Rows)
            {
                id = (int)row["id"];
                Console.Write(" generateQueueShortName - > searching for short name");
                if (id == Transaction_Type)
                {
                    short_name = (string)row["Short_Name"];
                    break;
                }

            }
            short_name += queueNumber;
            return short_name;
        }
        private DataTable getTransactionType()
        {
            DataTable a = new DataTable();
            a.Columns.Add("id", typeof(int));
            a.Columns.Add("Pattern_Max", typeof(int));
            a.Columns.Add("Transaction_Name", typeof(string));
            a.Columns.Add("Description", typeof(string));
            a.Columns.Add("Short_Name", typeof(string));
            SqlConnection con = new SqlConnection(connection_string);
            using (con)
            {
                con.Open();
                SqlCommand a_cmd = con.CreateCommand();
                SqlDataReader a_rdr;

                String a_q = "select * from Transaction_Type";
                a_cmd = new SqlCommand(a_q, con);

                a_rdr = a_cmd.ExecuteReader();
                while (a_rdr.Read())
                {
                    a.Rows.Add(
                       (int)a_rdr["id"],
                       (int)a_rdr["Pattern_Max"],
                       (string)a_rdr["Transaction_Name"],
                       (string)a_rdr["Description"],
                       (string)a_rdr["Short_Name"]);
                    Console.Write(" write getTransactionType -> Added a row! ");
                }
                con.Close();
            }
            Console.Write(" \n returning getTransasctionType... \n ");
            return a;
        }
        private DataTable getTransactionList()
        {
            DataTable transactionList = new DataTable();
            transactionList.Columns.Add("Transaction_ID", typeof(int));
            transactionList.Columns.Add("Servicing_Office", typeof(int));
            transactionList.Columns.Add("Pattern_No", typeof(int));

            SqlConnection con = new SqlConnection(connection_string);
            using (con)
            {
                con.Open();
                SqlCommand t_cmd = con.CreateCommand();
                SqlDataReader t_rdr;

                String t_q = "select * from Transaction_List";
                t_cmd = new SqlCommand(t_q, con);

                t_rdr = t_cmd.ExecuteReader();
                while (t_rdr.Read())
                {
                    transactionList.Rows.Add(
                       (int)t_rdr["Transaction_ID"],
                       (int)t_rdr["Servicing_Office"],
                       (int)t_rdr["Pattern_No"]);
                    Console.Write(" getTransactions -> Added a row! ");
                }
                con.Close();
            }
            Console.Write(" \n returning transactionList... \n ");
            return transactionList;
        }
        #endregion
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
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
            PROGRAM_Guest = false;
        }

        public void guest_button(object sender, EventArgs e)
        {
            // Guest Button
            label5.Text = "Please provide your name.";
            panel4.Visible = true;
            PROGRAM_Guest = true;
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
                if (PROGRAM_Guest)
                {
                    // Show next Panel / all transaction type.
                    panel5.Visible = true;
                    PROGRAM_Name = textBox1.Text;
                }
                else
                {
                    try
                    {
                        SqlConnection con = new SqlConnection(connection_string);
                        using (con)
                        {
                            con.Open();
                            int count = 0;
                            SqlCommand cmd = con.CreateCommand();
                            cmd.CommandType = CommandType.Text;
                            String query = "select Full_name, Student_No from vw_es_students where Student_No = '" + textBox1.Text + "'";
                            String fullname = "";
                            cmd = new SqlCommand(query, con);
                            SqlDataReader dr;
                            dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                count += 1;
                                fullname = dr.GetString(0);
                            }
                            if (count == 1)
                            {
                                panel5.Visible = true;
                                PROGRAM_Name = textBox1.Text;
                            }
                            else
                            {
                                MessageBox.Show("Student ID not found.");
                            }
                            con.Close();
                        }
                    }
                    catch (SqlException exd)
                    {
                        MessageBox.Show("Unable to process request to retrieve Student Number. Contact an administrator. Error->"+exd.Message,"Database error");
                    }
                    
                }
            }
        }

        public void up_button(object sender, EventArgs e)
        {
            // Up Button
            
            if (location - 70 > 0)
            {
                location -= 70;
                flowLayoutPanel1.VerticalScroll.Value = location;
            }
            else
            {
                // If scroll position is below 0 set the position to 0 (MIN)
                location = 0;
                flowLayoutPanel1.AutoScrollPosition = new Point(0, location);
            }
        }

        public void down_button(object sender, EventArgs e)
        {
            // Down Button
            if (location + 70 < flowLayoutPanel1.VerticalScroll.Maximum)
            {
                location += 70;
                flowLayoutPanel1.VerticalScroll.Value = location;
            }
            else
            {
                // If scroll position is above 280 set the position to 280 (MAX)
                location = flowLayoutPanel1.VerticalScroll.Maximum;
                flowLayoutPanel1.AutoScrollPosition = new Point(0, location);
            }
        }

        public void autoGenerateButton()
        {
            if (Height == 0 && Width == 0)
            {
                Height = flowLayoutPanel1.Height / 2;
                Width = (flowLayoutPanel1.Width / 3) - 7;
            }

            SqlConnection con = new SqlConnection(connection_string);
            con.Open();
            // Counting Data rows;
            string queryCount = "select count(*) from Transaction_Type";
            SqlCommand cmdCount = new SqlCommand(queryCount, con);
            tempTransCounter = (int)cmdCount.ExecuteScalar();

            if (transCounter == 0 || transCounter < tempTransCounter || transCounter > tempTransCounter)
            {
                transCounter = tempTransCounter;
                transaction_type.Clear();
                flowLayoutPanel1.Controls.Clear();
                // Getting Data
                string query = "select * from Transaction_Type";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader rdr;
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    int id = (int)rdr["id"];
                    string transaction_name = (string)rdr["Transaction_Name"];
                    string description = (string)rdr["Description"];
                    transaction_type.Add(id, transaction_name);

                    Button newButton = new Button();
                    newButton.Name = transaction_name;
                    newButton.Text = description;
                    newButton.Width = Width;
                    newButton.Height = Height;
                    newButton.FlatStyle = FlatStyle.Flat;
                    newButton.TextAlign = ContentAlignment.MiddleCenter;
                    newButton.Font = new System.Drawing.Font(newButton.Font.FontFamily, 22, FontStyle.Bold);
                    newButton.Click += new EventHandler(pick_button);
                    this.flowLayoutPanel1.Controls.Add(newButton);
                }
                con.Close();
            }
        }

        public void pick_button(object sender, EventArgs e)
        {
            //buttonStatus("disable");
            flowLayoutPanel1.Enabled = false;
            // Pick Button
            var myKey = transaction_type.First(x => x.Value == ((Button)sender).Name).Key;
            transactionID = myKey;
            // myKey -> transaction_type_id

            // find out if the customer is a student or guest
            // false for customer, true for guest
            try
            {
                if (PROGRAM_Guest)
                {
                    guestSubmit(myKey);
                }
                else
                {
                    studentSubmit(myKey);
                }
            }
            catch (SqlException b)
            {
                MessageBox.Show("Error ->"+b.Message,"Database error!");
            }

            // add more info on giving queue number
            panel6.Visible = true;
            PROGRAM_Guest = true;
            PROGRAM_Name = string.Empty;
            PROGRAM_Student_No = string.Empty;


            //Print Result;
            this.TopMost = false;
            getPrintResult();
        }

        public void getPrintResult()
        {
            Document doc = new Document();
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream("Receipt.pdf", FileMode.Create));
            doc.Open();
            PdfContentByte content = writer.DirectContent;

            //Getting Image

            iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance("../../Resources/ReceiptHeader.png");
            image.ScaleAbsolute(140f, 30f);
            image.Alignment = Element.ALIGN_CENTER;

            doc.Add(image);


            // Setting Text paragraph
            iTextSharp.text.Font fdefault = FontFactory.GetFont("Arial", 8, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);

            iTextSharp.text.Paragraph paragraph = new iTextSharp.text.Paragraph("Your Queue Number is:", fdefault);
            paragraph.Alignment = Element.ALIGN_CENTER;
            doc.Add(paragraph);

            fdefault = FontFactory.GetFont("Arial", 24, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
            paragraph = new iTextSharp.text.Paragraph(label8.Text, fdefault);
            paragraph.Alignment = Element.ALIGN_CENTER;
            doc.Add(paragraph);

            fdefault = FontFactory.GetFont("Arial", 6, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
            paragraph = new iTextSharp.text.Paragraph("\nPlease wait for your number.", fdefault);
            paragraph.Alignment = Element.ALIGN_CENTER;
            doc.Add(paragraph);

            paragraph = new iTextSharp.text.Paragraph(getTransactionOffices(), fdefault);
            paragraph.Alignment = Element.ALIGN_CENTER;
            doc.Add(paragraph);

            content.Rectangle(225f, 690f-addHeight, 150f, 120f+addHeight);
            content.Stroke();

            //iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(new Uri(path));

            //doc.Add(image);

            doc.Close();

            System.Diagnostics.Process.Start("Receipt.pdf");
                
        }

        public String getTransactionOffices()
        {
            String offices = "";
            int counter = 0;

            SqlConnection con = new SqlConnection(connection_string);
            con.Open();

            //string query = "select Name from Servicing_Office WHERE id IN ()";

            string query = "select SO.Name FROM Transaction_List as TL LEFT JOIN Servicing_Office as SO ON TL.Servicing_Office = SO.id WHERE Transaction_ID = @param1";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@param1", transactionID);
            SqlDataReader rdr;
            rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                counter++;
                if(counter == 4)
                {
                    offices += "\n";
                    addHeight += 5;
                }

                offices += rdr["Name"].ToString() + " -> ";

            }

            // return the string
            return offices;
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

        public static uint GetIdleTime()
        {
            LASTINPUTINFO LastUserAction = new LASTINPUTINFO();
            LastUserAction.cbSize = (uint)Marshal.SizeOf(LastUserAction);
            GetLastInputInfo(ref LastUserAction);
            return ((uint)Environment.TickCount - LastUserAction.dwTime);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            autoGenerateButton();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if ((Application.OpenForms["Form2"] as Form2) != null)
            {
                //Form is already open
            }
            else
            {
                this.TopMost = false;
                Form2 f2 = new Form2();
                f2.Show();
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (GetIdleTime() >= 2000)
            {
                autoGenerateButton();

                if ((Application.OpenForms["Form2"] as Form2) != null)
                {
                    //Form is already open
                }else
                {
                    this.TopMost = true;
                }

                flowLayoutPanel1.Enabled = true;
                resetFields();
                if(panel6.Visible == true)
                {
                    panel6.Visible = false;
                    panel5.Visible = false;
                    panel4.Visible = false;
                    panel3.Visible = false;

                }else if(panel5.Visible == true)
                {
                    panel5.Visible = false;
                    panel4.Visible = false;
                    panel3.Visible = false;

                }else if(panel4.Visible == true)
                {
                    panel4.Visible = false;
                    panel3.Visible = false;
                }else if(panel3.Visible == true)
                {
                    panel3.Visible = false;
                }
            }
        }

        //public void buttonStatus(String task)
        //{
        //    foreach(Control c in this.Controls)
        //    {
        //        if(c is Button && task == "enable")
        //        {
        //            c.Enabled = true;
        //        }else if(c is Button && task == "disable")
        //        {
        //            c.Enabled = false;
        //        }
        //    }
        //}
    }
}
