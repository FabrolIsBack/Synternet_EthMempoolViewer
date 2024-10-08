using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Media;
using System.Net;
using System.Numerics;
using System.Text;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using Google.Protobuf.WellKnownTypes;
using NatsProvidersub;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Series;


namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
   

        public ObservableCollection<BlockchainTransaction> Transactions { get; set; }
        NatsProvider natsProvider;
        private DispatcherTimer _timer; // Timer to call LoadData periodically
        public decimal max = 0;
        String accessToken;
        String natsUrl;
        String streamName;
      
        private DispatcherTimer _soundTimer = new DispatcherTimer(); // Timer to limit the sound duration
        private bool _isSoundPlaying = false;  // Flag to indicate if a sound is currently playing

        private decimal lastAlertValue = -1; // Tracks the last value that triggered an alert
        private DateTime lastAlertTime; // Tracks the last time an alert was triggered
        private TimeSpan alertCooldown = TimeSpan.FromSeconds(5); // Sets a cooldown to prevent repeated sounds
      
        public void setAttributesForNatsProvider()
        {
            accessToken = "{accessToken}";
            natsUrl = "nats://broker-eu-03.synternet.com"; //"{nats://url}";
            streamName = "synternet.ethereum.mempool";// {streamName}";
        }
        public MainWindow()
        {
            InitializeComponent();
            Transactions = new ObservableCollection<BlockchainTransaction>();
            DataContext = this; // Set DataContext for data binding
            TransactionListView.ItemsSource = Transactions; // Set ItemsSource here

            setAttributesForNatsProvider();

            //instance of nastProvider, C# SDK
            natsProvider = new NatsProvider(accessToken, natsUrl, streamName);
            natsProvider.Connect();

            // configure timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); 
            _timer.Tick += Timer_Tick; // Method that will be called at every interval
            _timer.Start(); // Start the timer
        }

        //load new data and update chart
        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadData();
            UpdatePlot();
        }

        private void PlayAlertIfNeeded(decimal value, int importance)
        {

            // Trigger the alarm only if the current value is significantly different from the last alert value 
          //  if (Math.Abs(value - lastAlertValue) > 1) 
            {
                PlayAlertSound(importance);
                lastAlertValue = value; // Update the last alert value
            }
        }

        //beep for transactions over 1 eth
        private void PlayAlertSound(int importance)
        {
            // Check if the last alert was sounded recently to avoid frequent repetitions
             if ((DateTime.Now - lastAlertTime) < alertCooldown)
             {
                 return; // Exit if the last alert was sounded too recently
             }

            // If a sound is already playing, stop it to avoid overlaps
            if (_isSoundPlaying)
            {
               // _mediaPlayer.Stop();
                _soundTimer?.Stop(); // Also stop the timer if it was running
            }

            _isSoundPlaying = true; // Set that the sound is currently playing
            
            // Choose the sound based on importance
            Debug.WriteLine($"Dentro a playalertsound: {importance}");
            string alertName = (importance == 0) ? "alert.mp3" : "normalAlert.mp3";
            string mp3FilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, alertName);

            // Check that the file exists
            if (System.IO.File.Exists(mp3FilePath))
            {
                MediaPlayer _mediaPlayer=new MediaPlayer();
                _mediaPlayer.Open(new Uri(mp3FilePath, UriKind.Absolute));
                _mediaPlayer.Play();
                Debug.WriteLine($"suona");
                // Configure the timer to stop playback after 2 seconds
                 _soundTimer = new DispatcherTimer();
                  _soundTimer.Interval = TimeSpan.FromSeconds(3);
                  _soundTimer.Tick += (s, e) =>
                  {
                      _mediaPlayer.Stop(); // Stop the sound after 2 seconds
                      _soundTimer.Stop();   // Stop the timer
                      _isSoundPlaying = false; // Sound completed
                  };
                  _soundTimer.Start(); // Start the timer*/
            }

            lastAlertTime = DateTime.Now; // Update the timestamp of the last alert
        }

        //when you click on an address that has invited or received more than one eth, view the address on Ethercan
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is string address && address != "N/A")
            {
                string url = $"https://etherscan.io/address/{address}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Required to open the browser
                });
            }
        }

        //create chart
        private void UpdatePlot()
        {
            var plotModel = new PlotModel { Title = "Transaction Values" };

            var lineSeries = new LineSeries
            {
                Title = "Values",
                MarkerType = MarkerType.Circle,
                DataFieldX = "Index",
                DataFieldY = "Value",
                StrokeThickness = 2
            };

            // Add data to the chart
            int index = 0;
            foreach (var transaction in Transactions)
            {
                if (decimal.TryParse(transaction.Value, out var value))
                {
                    lineSeries.Points.Add(new DataPoint(index++, (double)value));
                }
            }

            plotModel.Series.Add(lineSeries);
            TransactionPlotView.Model = plotModel;
        }

        public void LoadData()
        {
          
           
            string name = natsProvider.SubscribeSync();
            // Read JSON from file
            string json = name;
            // Deserialize JSON to a list of BlockchainTransaction objects
            var transaction = JsonConvert.DeserializeObject<BlockchainTransaction>(json);


            // Convert the value to BigInteger for accurate calculations
            BigInteger valueInWei = BigInteger.Parse(transaction.Value);
            BigInteger etherConversionFactor = new BigInteger(1000000000000000000); // 10^18

            // Divide using BigInteger and convert to decimal for formatting
            decimal valueInEther = (decimal)valueInWei / (decimal)etherConversionFactor;

            // Format the transaction value with 5 decimal places
            transaction.Value = valueInEther.ToString("F5");

            // Notify the ListView of the updated data
            // Add the transaction to the existing collection
            Transactions.Add(transaction);
            if (max < valueInEther)
            {
                max = valueInEther;
                ValueStatusLabel.Content = $"Attention: a max value detected is {valueInEther:F5}";
                ValueStatusLabel.Foreground = new SolidColorBrush(Colors.Red); 
            }

            //if a transaction above 1 eth is detected, the counters are incremented and a sound is played
            if (valueInEther >= 1){
                UpdateCounters();
            }
            

            // natsProvider.close();
            Debug.WriteLine($"JSON: {name}");
          
        }
       
        //update counters and play the sound
        private void UpdateCounters()
        {
            // Initialize variables for counters and addresses
            int count1To10 = 0;
            string from1To10 = "";
            string to1To10 = "";

            int count11To50 = 0;
            string from11To50 = "";
            string to11To50 = "";

            int count51To100 = 0;
            string from51To100 = "";
            string to51To100 = "";

            int countAbove100 = 0;
            string fromAbove100 = "";
            string toAbove100 = "";

            foreach (var transaction in Transactions)
            {
                if (decimal.TryParse(transaction.Value, out decimal value))
                {
                    if (value >= 1 && value <= 10)
                    {
                        count1To10++;
                        from1To10 = transaction.From; // Update the originating address
                        to1To10 = transaction.To; // Update the destination address

                        Debug.WriteLine($"value tra 1 e 10 call play {value} {count1To10}");
                        PlayAlertIfNeeded(value, 1);

                        // Update counters and address labels
                        Count1To10.Text = count1To10.ToString();
                        From1To10.Text = count1To10 > 0 ? from1To10 : "N/A"; // Display the address if it exists
                        From1To10.Tag = count1To10 > 0 ? from1To10 : "N/A"; // Set the Tag

                        To1To10.Text = count1To10 > 0 ? to1To10 : "N/A";
                        To1To10.Tag = count1To10 > 0 ? to1To10 : "N/A";


                    }
                    else if (value > 10 && value <= 50)
                    {
                        count11To50++;
                        from11To50 = transaction.From;
                        to11To50 = transaction.To;

                        Debug.WriteLine($"value tra 10 e 50 call play {value} {count11To50}");
                        PlayAlertIfNeeded(value, 1);


                        Count11To50.Text = count11To50.ToString();
                        From11To50.Text = count11To50 > 0 ? from11To50 : "N/A";
                        From11To50.Tag = count11To50 > 0 ? from11To50 : "N/A";

                        To11To50.Text = count11To50 > 0 ? to11To50 : "N/A";
                        To11To50.Tag = count11To50 > 0 ? to11To50 : "N/A";
                    }
                    else if (value > 50 && value <= 100)
                    {
                        count51To100++;
                        from51To100 = transaction.From;
                        to51To100 = transaction.To;

                        Debug.WriteLine($"value tra 50 e 100 call play {value} {count51To100}");
                        PlayAlertIfNeeded(value, 0);

                        Count51To100.Text = count51To100.ToString();
                        From51To100.Text = count51To100 > 0 ? from51To100 : "N/A";
                        From51To100.Tag = count51To100 > 0 ? from51To100 : "N/A";

                        To51To100.Text = count51To100 > 0 ? to51To100 : "N/A";
                        To51To100.Tag = count51To100 > 0 ? to51To100 : "N/A";

                    }
                    else if (value > 100)
                    {
                        countAbove100++;
                        fromAbove100 = transaction.From;
                        toAbove100 = transaction.To;
                        Debug.WriteLine($"value sopra 100 call play {value} {countAbove100}");
                        PlayAlertIfNeeded(value, 0);



                        CountAbove100.Text = countAbove100.ToString();
                        FromAbove100.Text = countAbove100 > 0 ? fromAbove100 : "N/A";
                        FromAbove100.Tag = countAbove100 > 0 ? fromAbove100 : "N/A";

                        ToAbove100.Text = countAbove100 > 0 ? toAbove100 : "N/A";
                        ToAbove100.Tag = countAbove100 > 0 ? toAbove100 : "N/A";
                    }
                }
            }

          


        }
      
    }
}