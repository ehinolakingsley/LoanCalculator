using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LoanCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<DbModel> LoanRecords { get; set; }
        double interestResult;
        double totalAmount;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDb();

            var x = await RetrieveRecordsFromDb();

            LoanRecords = new ObservableCollection<DbModel>(x);

            dbRecords.ItemsSource = LoanRecords;
        }


        #region Database Setup

        string BaseDir => $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/LoanCalculator";
        string DbFileLocation => $"{BaseDir}/LoanCalculator.db";

        SQLiteConnection Sqlite_Conn;
        SQLiteCommand Sqlite_Cmd;
        const string DbTableName = "LoanRecords";

        async void InitializeDb()
        {
            if (!Directory.Exists(BaseDir))
                Directory.CreateDirectory(BaseDir);

            Sqlite_Conn = new SQLiteConnection($"Data Source={DbFileLocation}; Version = 3; Compress = True; ");

            await Sqlite_Conn.OpenAsync();

            try
            {
                string Createsql = $"CREATE TABLE IF NOT EXISTS {DbTableName} (INVOICE_ID INTEGER, NAME VARCHAR(100), NUMBER VARCHAR(50), INTEREST_AMOUNT VARCHAR(50), TOTAL_AMOUNT VARCHAR(50), DATE_CREATED VARCHAR(50))";

                Sqlite_Cmd = Sqlite_Conn.CreateCommand();
                Sqlite_Cmd.CommandText = Createsql;
                await Sqlite_Cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\nPlease restart the application or contact developer!", "Database Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);

                Application.Current.Shutdown();
            }

            Sqlite_Conn.Close();
        }

        async Task<IEnumerable<DbModel>> RetrieveRecordsFromDb()
        {
            Sqlite_Conn.Open();

            SQLiteDataReader reader;
            Sqlite_Cmd = Sqlite_Conn.CreateCommand();
            Sqlite_Cmd.CommandText = $"SELECT * FROM {DbTableName}";

            reader = Sqlite_Cmd.ExecuteReader();

            List<DbModel> tempRecords = new List<DbModel>();

            while (await reader.ReadAsync())
            {
                DbModel x = new DbModel
                {
                    InvoiceId = reader.GetInt32(0),
                    CustomerName = reader.GetString(1),
                    CustomerContact = double.Parse(reader.GetString(2)),
                    InterestAmount = double.Parse(reader.GetString(3)),
                    TotalAmount = double.Parse(reader.GetString(4)),
                    DateTime = DateTime.Parse(reader.GetString(5))
                };

                tempRecords.Add(x);
            }

            Sqlite_Conn.Close();

            return tempRecords;
        }

        async Task<bool> SaveToDatabase(DbModel data)
        {
            try
            {
                await Sqlite_Conn.OpenAsync();

                Sqlite_Cmd = Sqlite_Conn.CreateCommand();
                Sqlite_Cmd.CommandText = $"INSERT INTO {DbTableName} (INVOICE_ID, NAME, NUMBER, INTEREST_AMOUNT, TOTAL_AMOUNT, DATE_CREATED) VALUES({data.InvoiceId}, '{data.CustomerName}', '{data.CustomerContact}','{data.InterestAmount}','{data.TotalAmount}', '{data.DateTime}');";

                await Sqlite_Cmd.ExecuteNonQueryAsync();

                LoanRecords.Add(data);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\nSaving failed! Please try again or contact the administrator.", "Database Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);

                return false;
            }
            finally
            {
                Sqlite_Conn.Close();
            }

        }


        #endregion

        #region Model

        double GetInterest_Model(double interest, double amount) => (interest / 100) * amount;

        double GetTotalAmount_Model(double amount, double interest_model) => amount + interest_model;

        void ClearScreen()
        {
            txtAmount.Text = null;
            txtInterest.Text = null;
            txtCustomerContact.Text = null;
            txtCustomerName.Text = null;

            viewInterestAmt.Text = null;
            viewTotalAmt.Text = null;
            interestResult = 0f;
            totalAmount = 0f;
        }

        #endregion

        #region Helpers

        bool IsEmpty(string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            if (string.IsNullOrWhiteSpace(value))
                return true;

            return false;
        }


        #endregion


        void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            string amount = txtAmount.Text;
            string interest = txtInterest.Text;

            if (IsEmpty(amount))
            {
                MessageBox.Show("Amount is required!", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
                return;
            }

            if (IsEmpty(interest))
            {
                MessageBox.Show("Interest is required!", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
                return;
            }

            double amount_, interest_;

            if(!double.TryParse(amount, out amount_))
            {
                MessageBox.Show("Amount is invalid!", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
                return;
            }

            if (!double.TryParse(interest, out interest_))
            {
                MessageBox.Show("Interest is invalid!", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
                return;
            }


             interestResult = GetInterest_Model(interest: interest_, amount_);
             totalAmount = GetTotalAmount_Model(amount: amount_, interestResult);

            viewInterestAmt.Text = interestResult.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-NG"));
            viewTotalAmt.Text =  totalAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-NG"));
        }

        async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = txtCustomerName.Text;
            string contact = txtCustomerContact.Text;

            if (IsEmpty(name))
            {
                MessageBox.Show("Customer's name is required!", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
                return;
            }

            if (IsEmpty(contact))
            {
                MessageBox.Show("Customer's mobile number is required!", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
                return;
            }

            if(!double.TryParse(contact, out double contact_))
            {
                MessageBox.Show("Customer's mobile number is in valid!", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
                return;
            }

            bool saveSuccessful = await SaveToDatabase(new DbModel
            {
                CustomerContact = contact_,
                DateTime = DateTime.Now,
                CustomerName = name,
                InterestAmount = interestResult,
                TotalAmount = totalAmount,
                InvoiceId = LoanRecords.Count + 1
            });

            if (saveSuccessful)
            {
                ClearScreen();
                MessageBox.Show("Save successful", "Saved", button: MessageBoxButton.OK, icon: MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Unable to save customer's record! Please try again", "Exception", button: MessageBoxButton.OK, icon: MessageBoxImage.Exclamation);
            }
        }

        async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string query = txtSearchQuery.Text;

            LoanRecords = new ObservableCollection<DbModel>(await RetrieveRecordsFromDb());

            if (IsEmpty(query))
            {
                dbRecords.ItemsSource = LoanRecords;
                return;
            }

           dbRecords.ItemsSource = LoanRecords.Where(i => i.CustomerContact.ToString().Contains(query));
        }
    }
}
