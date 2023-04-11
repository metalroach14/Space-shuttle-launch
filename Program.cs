using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MailKit.Net.Smtp;
using MimeKit;


namespace Space_shuttle_launch
{
    class Day
    {
        private int id;
        private int temperature;
        private int wind;
        private int humidity;
        private int precipitation;
        private bool isLightning;
        private string clouds;
        private int launchPriority;

        private void setInitialLaunchPriority()
        {
            if (temperature < 2 || temperature > 31 ||
                wind > 10 || humidity > 60 ||
                precipitation != 0 || isLightning ||
                string.Equals(clouds, "Cumulus") || string.Equals(clouds, "Nimbus"))
            {
                launchPriority = -1;
            }
            else
            {
                launchPriority = 0;
            }

        }
        public Day(int id, string temperature, string wind, string humidity, string precipitation, string isLightning, string clouds)
        {
            this.id = id;
            this.temperature = Convert.ToInt32(temperature);
            this.wind = Convert.ToInt32(wind);
            this.humidity = Convert.ToInt32(humidity);
            this.precipitation = Convert.ToInt32(precipitation);
            this.isLightning = string.Equals(isLightning, "Yes") ? true : false;
            this.clouds = clouds;
            setInitialLaunchPriority();
        }


        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        public int Temperature
        {
            get { return temperature; }
            set { temperature = value; }
        }
        public int Wind
        {
            get { return wind; }
            set { wind = value; }
        }
        public int Humidity
        {
            get { return humidity; }
            set { humidity = value; }
        }
        public int Precipitation
        {
            get { return precipitation; }
            set { precipitation = value; }
        }
        public bool IsLightning
        {
            get { return isLightning; }
            set { isLightning = value; }
        }
        public string Clouds
        {
            get { return clouds; }
            set { clouds = value; }
        }
        public int LaunchPriority
        {
            get { return launchPriority; }
            set { launchPriority = value; }
        }
    }

    class Program
    {
        // Check if the provided file contains valid weather entries. 
        static void verifyFileData(in List<string[]> rows)
        {
            try
            {
                for (int i = 1; i < rows[0].Length; i++)
                {
                    int temp = Convert.ToInt32(rows[1][i]);
                    int wind = Convert.ToInt32(rows[2][i]);
                    int humidity = Convert.ToInt32(rows[3][i]);
                    int precipitation = Convert.ToInt32(rows[4][i]);
                    if (temp < -50 || temp > 60 || wind < 0 || humidity < 0 || humidity > 100 || precipitation < 0 || precipitation > 100)
                    {
                        Console.WriteLine("File contains corrupted data");
                        Environment.Exit(1);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Problem with the data: {e.Message}");
                Environment.Exit(1);
            }

        }

        // Modifies each row of the input file to match the expected output format.
        //  Also calculates the most appropriate lauch day (if any)
        static void prepareOutputFile(ref List<string> lines, in List<Day> Days)
        {
            lines[0] = lines[0] + ",Average,Min,Max,Median";
            lines[1] = lines[1] + "," + Convert.ToInt32(Days.Average(day => day.Temperature)) + "," + Days.Min(day => day.Temperature)
                + "," + Days.Max(day => day.Temperature) +
                "," + selectAlgorithm(Days.Select(day => day.Temperature).ToList(), Days.Count / 2);
            lines[2] = lines[2] + "," + Convert.ToInt32(Days.Average(day => day.Wind)) + "," + Days.Min(day => day.Wind)
                + "," + Days.Max(day => day.Wind) +
                "," + selectAlgorithm(Days.Select(day => day.Wind).ToList(), Days.Count / 2);
            lines[3] = lines[3] + "," + Convert.ToInt32(Days.Average(day => day.Humidity)) + "," + Days.Min(day => day.Humidity)
                + "," + Days.Max(day => day.Humidity) +
                "," + selectAlgorithm(Days.Select(day => day.Humidity).ToList(), Days.Count / 2);
            lines[4] = lines[4] + "," + Convert.ToInt32(Days.Average(day => day.Precipitation)) + "," + Days.Min(day => day.Precipitation)
                + "," + Days.Max(day => day.Precipitation) +
                "," + selectAlgorithm(Days.Select(day => day.Precipitation).ToList(), Days.Count / 2);
            lines[5] = lines[5] + ", , , , ";
            lines[6] = lines[6] + ", , , , ";
            lines.Add("Most appropriate launch day");

            Days.RemoveAll(day => (day.LaunchPriority == -1));
            Days.Sort((day1, day2) =>
            {
                if (day1.Wind == day2.Wind)
                {
                    return day1.Humidity.CompareTo(day2.Humidity);
                }
                else
                {
                    return day1.Wind.CompareTo(day2.Wind);
                }
            });
            foreach (var day in Days)
            {
                lines[7] = lines[7] + "," + day.Id;
            }

        }

        // Creates the file "WeatherReport.csv" which will be send via email
        static void createOutputFile(in List<string> lines)
        {
            try
            {
                var newFilePath = "WeatherReport.csv";
                using (var writer = new StreamWriter(newFilePath))
                {
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"There was an error creating the file: {e.Message}");
                Environment.Exit(1);
            }
        }

        // 
        static void processFile(string fileName)
        {

            try
            {
                var rows = new List<string[]>();
                var lines = new List<string>();
                using (var reader = new StreamReader(fileName))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        lines.Add(line);
                        rows.Add(values);
                    }
                }
                verifyFileData(rows);
                List<Day> Days = new List<Day>();
                for (int i = 1; i < rows[0].Length; i++)
                {

                    Days.Add(new Day(i, rows[1][i], rows[2][i], rows[3][i], rows[4][i], rows[5][i], rows[6][i]));
                }
                prepareOutputFile(ref lines, Days);
                createOutputFile(lines);
            }
            catch (Exception e)
            {
                Console.WriteLine($"There was an error with the file: {e.Message}");
                Environment.Exit(1);
            }

        }

        // Fancy way to find median of a given list
        public static int selectAlgorithm(List<int> list, int k)
        {
            if (list.Count == 1)
            {
                return list[0];
            }

            int pivotIndex = 0; // For simplicity, always use the first element as the pivot
            int pivotValue = list[pivotIndex];

            List<int> lessThan = new List<int>();
            List<int> greaterThan = new List<int>();

            for (int i = 1; i < list.Count; i++)
            {
                if (list[i] < pivotValue)
                {
                    lessThan.Add(list[i]);
                }
                else
                {
                    greaterThan.Add(list[i]);
                }
            }

            if (lessThan.Count == k)
            {
                return pivotValue;
            }
            else if (lessThan.Count > k)
            {
                return selectAlgorithm(lessThan, k);
            }
            else
            {
                return selectAlgorithm(greaterThan, k - lessThan.Count - 1);
            }
        }

        // method to send the email, containing the newly created file
        public static void sendEmail(string filePath, string senderEmail, string password, string receiverEmail)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderEmail, senderEmail));
                message.To.Add(new MailboxAddress(receiverEmail, receiverEmail));
                message.Subject = "Weather Report";

                var body = new TextPart("plain")
                {
                    Text = "Hello, \n\nThis email contains report regarding most appropriate day for shuttle launch based on weather conditions. " +
                    "\n\nPlease refer to the last parameter, \"Most appropriate launch day \". In case it is empty, there are no days that meet " +
                    "the given criteria." +
                    "\n\nRegards,\n" + senderEmail
                };

                var attachment = new MimePart("application", "csv")
                {
                    Content = new MimeContent(File.OpenRead(filePath)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    FileName = Path.GetFileName(filePath)
                };

                var multipart = new Multipart("mixed");
                multipart.Add(body);
                multipart.Add(attachment);
                message.Body = multipart;

                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate(senderEmail, password); //"jccdktfdeunmmxok"
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error occured: {e.Message}");
                Environment.Exit(1);
            }
        }
        static void Main(string[] args)
        {

            string filePath;
            Console.WriteLine("Please enter file path: ");
            filePath = Console.ReadLine();
            processFile(filePath);

            Console.WriteLine("File has been successfully processed");
            string senderEmail;
            string password;
            string receiverEmail;
            Console.WriteLine("Please enter sender email, password and receiver email");
            senderEmail = Console.ReadLine();
            password = Console.ReadLine();
            receiverEmail = Console.ReadLine();

            sendEmail("WeatherReport.csv", senderEmail, password, receiverEmail);
            Console.WriteLine("Email send successfully! ");
        }
    }
}
